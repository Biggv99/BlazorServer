using OptimusPrime.Core.Network;
using OptimusPrime.Core.Utils;
using OptimusPrime.Features.Contracts.Models;
using OptimusPrime.Presentation.Services.Shared;

namespace OptimusPrime.Presentation.Services;

public class ProfileService(
    HttpClient http,
    Interceptor interceptor,
    ILogger<ProfileService> logger) {

    private readonly HttpClient _http = http;
    private readonly Interceptor _interceptor = interceptor;
    private readonly ILogger<ProfileService> _logger = logger;

    // ==========================
    // Me
    // ==========================
    public async Task<Result<UserModel>> Me() {

        var request = new HttpRequestMessage(HttpMethod.Get, ApiRoutes.Me);

        // headers
        if (!await _interceptor.InterceptorHeaders(request, includeDeviceSignature: false)) {
            return Result<UserModel>.Failure("Missing required data. Try logging again", ErrorType.Unauthorized);
        }

        // send request
        try {
            var response = await _http.SendAsync(request);
            return await response.Content.ReadFromJsonAsync<Result<UserModel>>() ?? Result<UserModel>.Failure("Server not responding", ErrorType.ExternalServiceError);

        } catch (Exception ex) {
            _logger.LogError($"Error communicating with the server: {ex.Message}");
            return Result<UserModel>.Failure("Error communicating with the server", ErrorType.ExternalServiceError);
        }
    }
}
