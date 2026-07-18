using OptimusPrime.Core.Network;
using OptimusPrime.Core.Security;
using OptimusPrime.Core.Security.Crypto;
using OptimusPrime.Core.Utils;
using OptimusPrime.Features.Contracts.Commands;
using OptimusPrime.Features.Contracts.Commands.Shared;
using OptimusPrime.Features.Contracts.Models;
using OptimusPrime.Features.Handlers.Shared;
using OptimusPrime.Features.Interfaces;
using OptimusPrime.Infrastructure.Notifications;
using System.Globalization;
using System.Net;
using System.Security.Cryptography;
using System.Text;

namespace OptimusPrime.Features.Handlers;

public class AuthHandler(
    IAuthRepository repositoryAuth,
    AuditHandler auditHandler,
    CheckHandler checkHandler,
    JwtProvider jwtProvider,
    HashProvider hashProvider,
    ProviderService providerService,
    EmailNotification emailNotification,
    IConfiguration configuration ) {

    private readonly IAuthRepository _repositoryAuth = repositoryAuth;
    private readonly AuditHandler _auditHandler = auditHandler;
    private readonly CheckHandler _checkHandler = checkHandler;
    private readonly JwtProvider _jwtProvider = jwtProvider;
    private readonly HashProvider _hashProvider = hashProvider;
    private readonly ProviderService _providerService = providerService;
    private readonly EmailNotification _emailNotification = emailNotification;
    private readonly IConfiguration _configuration = configuration;

    private const string entityName = "AUTH";

    // ==========================
    // Registration
    // ==========================
    public async Task<Result<string>> HandleRegistration(RegisterCommand request) {

        const string actionType = "REGISTRATION";

        // check: email already in use 
        Result<bool> emailExist = await _repositoryAuth.ExistUser(request.Email);
        if(emailExist.IsSuccess) {
            await _auditHandler.HandleSave(request, actionType, entityName, emailExist.Message, request.Email);
            return Result<string>.Failure("Email already in use", ErrorType.Conflict);
        }
        if(emailExist.Error != ErrorType.None && emailExist.Error != ErrorType.NotFound) {
            await _auditHandler.HandleSave(request, actionType, entityName, emailExist.Message, request.Email);
            return Result<string>.Failure(emailExist);
        }

        // check: diffie-hellman key exchange
        var cryptoResult = _providerService.ServerExecute(request);
        if(cryptoResult == null) {
            await _auditHandler.HandleSave(request, actionType, entityName, "Diffie-Hellman key exchange failed", request.Email);
            return Result<string>.Failure("Diffie-Hellman key exchange failed", ErrorType.InternalError);
        }

        var user = new UserModel {
            Email = request.Email,
            Password = _hashProvider.Hash(request.Password),
            PublicKey = cryptoResult.Value.PublicKey,
            DeviceId = request.DeviceId,
            EmailConfirmationToken = Convert.ToHexString(RandomNumberGenerator.GetBytes(32))
        };

        // link to confirm email
        string link = BuildLinkEmail(user.EmailConfirmationToken);
        Result<bool> emailResult = await _emailNotification.SendEmailConfirmation(request.Email, link);
        if(!emailResult.IsSuccess) {
            await _auditHandler.HandleSave(request, actionType, entityName, emailResult.Message, request.Email, user.Id);
            return Result<string>.Failure(emailResult);
        }

        // request: save user 
        Result<bool> salvato = await _repositoryAuth.SaveUser(user);
        if (!salvato.IsSuccess) {
            await _auditHandler.HandleSave(request, actionType, entityName, salvato.Message, request.Email, user.Id);
            return Result<string>.Failure(salvato);
        }

        await _auditHandler.HandleSave(request, actionType, entityName, "Registration successful. Email confirmation sent", request.Email, user.Id, true);
        return Result<string>.Success(cryptoResult.Value.Payload, "Registration successful. Check your inbox to confirm your email");
    }

    // ==========================
    // Login
    // ==========================
    public async Task<Result<JwtTokenModel>> HandleLogin(LoginCommand request) {

        const string actionType = "LOGIN";

        // checks
        Result<UserModel> checkResult = await _checkHandler.Check(
            request,
            new CheckOptions { RequireEmail = false, RequireAdmin = false },
            email: request.Email
        );

        if (!checkResult.IsSuccess || checkResult.Value == null) {
            await _auditHandler.HandleSave(request, actionType, entityName, checkResult.Message, checkResult.Value?.Email ?? string.Empty, checkResult.Value?.Id);
            return Result<JwtTokenModel>.Failure(checkResult);
        }

        UserModel user = checkResult.Value;

        // check: password
        bool passwordCorretta = _hashProvider.Verify(request.Password, user.Password);
        if(!passwordCorretta) {
            await _auditHandler.HandleSave(request, actionType, entityName, "Wrong password", user.Email, user.Id);
            return Result<JwtTokenModel>.Failure("Wrong credentials", ErrorType.Unauthorized);
        }

        // request: save refresh token and return jwt
        JwtTokenModel jwt = _jwtProvider.GenerateToken(user);

        var refreshToken = new RefreshTokenModel {
            RefreshToken = _hashProvider.Hash(jwt.RefreshToken),
            ExpiresOn = DateTime.UtcNow.AddSeconds(jwt.RefreshExpiresIn),
        };

        Result<bool> refreshResult = await _repositoryAuth.SaveRefreshToken(user.Email, refreshToken);
        if(!refreshResult.IsSuccess) {
            await _auditHandler.HandleSave(request, actionType, entityName, refreshResult.Message, user.Email, user.Id);
            return Result<JwtTokenModel>.Failure(refreshResult);
        }

        await _auditHandler.HandleSave(request, actionType, entityName, "Login successful", user.Email, user.Id, true);
        return Result<JwtTokenModel>.Success(jwt, "Login successful");
    }

    // ==========================
    // Confirm email
    // ==========================
    public async Task<Result<bool>> HandleConfirmEmail(ConfirmEmailCommand request) {

        const string actionType = "CONFIRM EMAIL";

        // request: set email confirmed
        Result<UserModel> confirmationResult = await _repositoryAuth.ConfirmEmail(request.Token);
        if (!confirmationResult.IsSuccess || confirmationResult.Value == null) {
            await _auditHandler.HandleSave(request, actionType, entityName, confirmationResult.Message, confirmationResult.Value?.Email ?? string.Empty, confirmationResult.Value?.Id);
            return Result<bool>.Failure(confirmationResult);
        }

        await _auditHandler.HandleSave(request, actionType, entityName, confirmationResult.Message, confirmationResult.Value.Email, confirmationResult.Value.Id, true);
        return Result<bool>.Success(confirmationResult.Message);
    }

    // ==========================
    // Resend confirm email
    // ==========================
    public async Task<Result<bool>> HandleResendConfirmEmail(EmailCommand request) {

        const string actionType = "RESEND CONFIRMATION EMAIL";

        // get token
        Result<UserModel> tokenResult = await _repositoryAuth.GetEmailConfirmToken(request.Email);
        if (!tokenResult.IsSuccess || tokenResult.Value == null) {
            await _auditHandler.HandleSave(request, actionType, entityName, "Failed to generate confirmation token", request.Email);
            return Result<bool>.Failure("Failed to generate confirmation token", ErrorType.InternalError);
        }

        // request: send link to confirm email
        string link = BuildLinkEmail(tokenResult.Value.EmailConfirmationToken);
        Result<bool> emailResult = await _emailNotification.SendEmailConfirmation(tokenResult.Value.Email, link);
        if (!emailResult.IsSuccess) {
            await _auditHandler.HandleSave(request, actionType, entityName, emailResult.Message, tokenResult.Value.Email, tokenResult.Value.Id);
            return Result<bool>.Failure(emailResult);
        }

        await _auditHandler.HandleSave(request, actionType, entityName, emailResult.Message, tokenResult.Value.Email, tokenResult.Value.Id, true);
        return Result<bool>.Success(emailResult.Message);
    }

    // ==========================
    // Forgot password
    // ==========================
    public async Task<Result<bool>> HandleForgotPassword(EmailCommand request) {

        const string actionType = "FORGOT PASSWORD";

        // check: user exists
        Result<bool> emailExist = await _repositoryAuth.ExistUser(request.Email);
        if (!emailExist.IsSuccess) {
            await _auditHandler.HandleSave(request, actionType, entityName, emailExist.Message, request.Email);
            return Result<bool>.Failure("No user found with the provided email", ErrorType.NotFound);
        }

        // save token
        string randomToken = Convert.ToHexString(RandomNumberGenerator.GetBytes(32));
        string date = DateTime.UtcNow.AddHours(1).ToString("yyyyMMddHHmmss");
        string token = $"{randomToken}{date}";
        
        Result<UserModel> saveTokenResult = await _repositoryAuth.SaveForgotPasswordToken(request.Email, token);
        if (!saveTokenResult.IsSuccess || saveTokenResult.Value == null) {
            await _auditHandler.HandleSave(request, actionType, entityName, saveTokenResult.Message, request.Email);
            return Result<bool>.Failure("Failed to save forgot password token", ErrorType.InternalError);
        }

        // request: send email with reset code
        Result<bool> emailResult = await _emailNotification.SendForgotPasswordEmail(saveTokenResult.Value.Email, token);
        if (!emailResult.IsSuccess) {
            await _auditHandler.HandleSave(request, actionType, entityName, emailResult.Message, saveTokenResult.Value.Email);
            return Result<bool>.Failure(emailResult);
        }

        await _auditHandler.HandleSave(request, actionType, entityName, emailResult.Message, saveTokenResult.Value.Email, saveTokenResult.Value.Id, true);
        return Result<bool>.Success(emailResult.Message);
    }

    // ==========================
    // Reset password
    // ==========================
    public async Task<Result<bool>> HandleResetPassword(ResetPasswordCommand request) {

        const string actionType = "RESET PASSWORD";

        // checks
        Result<UserModel> checkResult = await _checkHandler.Check(
            request,
            new CheckOptions { RequireAdmin = false },
            request.Email
        );

        if (!checkResult.IsSuccess || checkResult.Value == null) {
            await _auditHandler.HandleSave(request, actionType, entityName, checkResult.Message, checkResult.Value?.Email ?? string.Empty, checkResult.Value?.Id);
            return Result<bool>.Failure(checkResult);
        }

        UserModel user = checkResult.Value;

        // check: reset code validity
        Result<UserModel> resetCodeResult = await _repositoryAuth.GetForgotPasswordToken(user.Email);
        if (!resetCodeResult.IsSuccess || resetCodeResult.Value == null || string.IsNullOrEmpty(resetCodeResult.Value.PasswordResetToken)) {
            await _auditHandler.HandleSave(request, actionType, entityName, resetCodeResult.Message, user.Email);
            return Result<bool>.Failure(resetCodeResult);
        }

        string dbToken = resetCodeResult.Value.PasswordResetToken;
        if (string.IsNullOrEmpty(request.ResetCode) || dbToken.Length != 78 || request.ResetCode.Length != 78) {
            await _auditHandler.HandleSave(request, actionType, entityName, "Invalid or expired reset code", user.Email);
            return Result<bool>.Failure("Invalid or expired reset code", ErrorType.Unauthorized);
        }

        bool tokenMatch = CryptographicOperations.FixedTimeEquals(
            Encoding.UTF8.GetBytes(dbToken),
            Encoding.UTF8.GetBytes(request.ResetCode));
        if (!tokenMatch) {
            await _auditHandler.HandleSave(request, actionType, entityName, "Invalid or expired reset code", user.Email);
            return Result<bool>.Failure("Invalid or expired reset code", ErrorType.Unauthorized);
        }

        string datePart = dbToken[64..];
        bool isValidDate = DateTime.TryParseExact(
            datePart,
            "yyyyMMddHHmmss",
            CultureInfo.InvariantCulture,
            DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal,
            out DateTime expirationDate);

        if (!isValidDate || expirationDate < DateTime.UtcNow) {
            await _auditHandler.HandleSave(request, actionType, entityName, "Expired reset code", user.Email);
            return Result<bool>.Failure("Invalid or expired reset code", ErrorType.Unauthorized);
        }

        // request: change password
        Result<bool> changePasswordResult = await _repositoryAuth.ChangePassword(user.Email, _hashProvider.Hash(request.Password));  
        if (!changePasswordResult.IsSuccess) {
            await _auditHandler.HandleSave(request, actionType, entityName, changePasswordResult.Message, user.Email);
            return changePasswordResult;
        }

        await _auditHandler.HandleSave(request, actionType, entityName, "Password reset successful", user.Email, user.Id, true);
        return Result<bool>.Success("Password reset successful");
    }

    // ==========================
    // Refresh
    // ==========================
    public async Task<Result<JwtTokenModel>> HandleRefresh(RefreshCommand request) {

        const string actionType = "REFRESH";

        // checks
        Result<UserModel> checkResult = await _checkHandler.Check(
            request,
            new CheckOptions { RequireAdmin = false }
        );

        if (!checkResult.IsSuccess || checkResult.Value == null) {
            await _auditHandler.HandleSave(request, actionType, entityName, checkResult.Message, checkResult.Value?.Email ?? string.Empty, checkResult.Value?.Id);
            return Result<JwtTokenModel>.Failure(checkResult);
        }

        UserModel user = checkResult.Value;

        // check: refresh token 
        if (user.RefreshToken == null) {
            await _auditHandler.HandleSave(request, actionType, entityName, "No refresh token found", user.Email, user.Id);
            return Result<JwtTokenModel>.Failure("No refresh token found. Please login again", ErrorType.InternalError);
        }

        bool refreshValid = _hashProvider.Verify(request.RefreshToken, user.RefreshToken.RefreshToken);
        if (!refreshValid) {
            await _auditHandler.HandleSave(request, actionType, entityName, "Invalid refresh token", user.Email, user.Id);
            return Result<JwtTokenModel>.Failure("Invalid refresh token", ErrorType.Unauthorized);
        }

        if (user.RefreshToken.ExpiresOn < DateTime.UtcNow || user.RefreshToken.IsRevoked) {
            await _auditHandler.HandleSave(request, actionType, entityName, "The session is expired or revoked", user.Email, user.Id);
            return Result<JwtTokenModel>.Failure("The session is expired. Please login again", ErrorType.Unauthorized);
        }

        // request: save refresh token and return jwt
        JwtTokenModel? jwt = _jwtProvider.GenerateToken(user);

        var refreshToken = new RefreshTokenModel {
            RefreshToken = _hashProvider.Hash(jwt.RefreshToken),
            ExpiresOn = DateTime.UtcNow.AddSeconds(jwt.RefreshExpiresIn),
        };

        Result<bool> refreshResult = await _repositoryAuth.SaveRefreshToken(user.Email, refreshToken);
        if (!refreshResult.IsSuccess) {
            await _auditHandler.HandleSave(request, actionType, entityName, refreshResult.Message, user.Email, user.Id);
            return Result<JwtTokenModel>.Failure(refreshResult);
        }

        await _auditHandler.HandleSave(request, actionType, entityName, "Refresh successful", user.Email, user.Id, true);
        return Result<JwtTokenModel>.Success(jwt, "Refresh successful");
    }

    // ==========================
    // Delete all
    // ==========================
    public async Task<Result<bool>> HandleDeleteAll(BaseTrackedCommand request) {

        const string actionType = "DELETE ALL";

        // checks
        Result<UserModel> checkResult = await _checkHandler.Check(
            request,
            new CheckOptions { RequireDeviceId = false, RequireSignature = false }
        );

        if (!checkResult.IsSuccess || checkResult.Value == null) {
            await _auditHandler.HandleSave(request, actionType, entityName, checkResult.Message, checkResult.Value?.Email ?? string.Empty, checkResult.Value?.Id);
            return Result<bool>.Failure(checkResult);
        }

        UserModel user = checkResult.Value;

        // request: delete all users
        Result<bool> deleteAllResult = await _repositoryAuth.DeleteAllUsers();
        if(!deleteAllResult.IsSuccess) {
            await _auditHandler.HandleSave(request, actionType, entityName, deleteAllResult.Message, user.Email, user.Id);
            return deleteAllResult;
        }

        await _auditHandler.HandleSave(request, actionType, entityName, "Deleted all user data", user.Email, user.Id, true);
        return deleteAllResult;
    }

    // ==========================
    // Helpers
    // ==========================
    public string BuildLinkEmail(string token) {
        string baseUrl = _configuration.GetValue<string>("AppConfig:BaseUrl") ?? throw new Exception("No base URL configured");
        string encodedToken = WebUtility.UrlEncode(token);
        return $"{baseUrl.TrimEnd('/')}{ApiRoutes.ConfirmEmail}?token={encodedToken}";
    }
}
