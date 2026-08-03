using OptimusPrime.Core.Network;
using OptimusPrime.Core.Security;
using OptimusPrime.Core.Security.Crypto;
using OptimusPrime.Core.Utils;
using OptimusPrime.Features.Contracts.Commands.Shared;
using OptimusPrime.Features.Contracts.Models;
using OptimusPrime.Features.Interfaces;

namespace OptimusPrime.Features.Handlers.Shared;

public class CheckOptions {
    public bool RequireEmail { get; set; } = true;
    public bool RequireUser { get; set; } = true;
    public bool RequireEmailConfirmed { get; set; } = true;
    public bool RequireAdmin { get; set; } = true;
    public bool RequireDeviceId { get; set; } = true;
    public bool RequireSignature { get; set; } = true;

    // flags for special cases
    public bool LoginFlagged { get; set; } = false;
    public bool RefreshFlagged { get; set; } = false;
}

public class CheckHandler(
    IAuthRepository repositoryAuth,
    JwtProvider jwtProvider,
    ProviderService providerService) {

    private readonly IAuthRepository _repositoryAuth = repositoryAuth;
    private readonly JwtProvider _jwtProvider = jwtProvider;
    private readonly ProviderService _providerService = providerService;

    public async Task<Result<UserModel>> Check(
        BaseTrackedCommand request,
        CheckOptions check,
        string? email = null,
        UserModel? user = null ) {

        // email
        if (check.RequireEmail && string.IsNullOrEmpty(email)) {
           
            email = _jwtProvider.ExtractEmail(request.AccessToken, check.RefreshFlagged);
         
            if (string.IsNullOrEmpty(email)) {
                return Result<UserModel>.Failure("Invalid email", ErrorType.Unauthorized);
            }
        }

        // user
        if (check.RequireUser && !string.IsNullOrEmpty(email)) {
            Result<UserModel> userResult = await _repositoryAuth.GetUserSensitive(email, request.DeviceId);
            if (!userResult.IsSuccess || userResult.Value == null) {
                return Result<UserModel>.Failure(userResult.Message, userResult.Error);
            }

            user = userResult.Value;
        }

        if (user != null) {

            // email confirmation
            if (check.RequireEmailConfirmed && !user.IsEmailConfirmed) {
                return Result<UserModel>.Failure("Email not confirmed. Check your inbox", ErrorType.Forbidden);
            }

            // admin 
            if (check.RequireAdmin && user.Role.ToString() != "Admin") {
                return Result<UserModel>.Failure("User not authorized", ErrorType.Unauthorized);
            }

            // device id
            if (check.RequireDeviceId && (user.CurrentDevice == null || user.CurrentDevice.DeviceId != request.DeviceId)) {

                if(check.LoginFlagged) return Result<UserModel>.Success(user, "new device");

                return Result<UserModel>.Failure("Device not authorized", ErrorType.Unauthorized);
            }

            // signature 
            if (check.RequireSignature) {

                if(user.CurrentDevice == null || string.IsNullOrEmpty(user.CurrentDevice.PublicKey)) {

                    if(check.LoginFlagged) return Result<UserModel>.Success(user, "new device");

                    return Result<UserModel>.Failure("Device or public key missing for signature validation", ErrorType.Unauthorized);
                }

                var now = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
                if (!request.DeviceTimestamp.HasValue || Math.Abs(now - request.DeviceTimestamp.Value) > 300000) {

                    if(check.LoginFlagged) return Result<UserModel>.Success(user, "QR recovery");

                    return Result<UserModel>.Failure("Expired timestamp", ErrorType.Unauthorized);
                }

                bool isDeviceValid = _providerService.VerifySignature(
                    request.DeviceTimestamp.ToString()!,
                    request.DeviceSignature,
                    user.CurrentDevice.PublicKey
                );

                if (!isDeviceValid) {

                    if(check.LoginFlagged) return Result<UserModel>.Success(user, "QR recovery");

                    return Result<UserModel>.Failure("Device not authorized", ErrorType.Unauthorized);
                }
            }
        }

        // end of validations
        if (user == null) {
            return Result<UserModel>.Success("Checks passed without user context");
        }

        return Result<UserModel>.Success(user, "Checks passed");
    }
}