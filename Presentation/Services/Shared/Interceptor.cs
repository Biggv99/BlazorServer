using Microsoft.Extensions.Options;
using OptimusPrime.Core.Security.Crypto;
using OptimusPrime.Core.Settings;
using OptimusPrime.Presentation.Utils.Storage;
using System.Net.Http.Headers;

namespace OptimusPrime.Presentation.Services.Shared;

public class Interceptor(
    IHttpContextAccessor httpContextAccessor,
    LocalStorage localStorage,
    SessionStorage sessionStorage,
    ProviderService providerService,
    IOptions<SecuritySettings> securitySettings,
    ILogger<Interceptor> logger ) {

    private readonly IHttpContextAccessor _httpContextAccessor = httpContextAccessor;
    private readonly LocalStorage _localStorage = localStorage;
    private readonly SessionStorage _sessionStorage = sessionStorage;
    private readonly ProviderService _providerService = providerService;
    private readonly SecuritySettings _securitySettings = securitySettings.Value;
    private readonly ILogger<Interceptor> _logger = logger;

    public async Task<bool> InterceptorHeaders(HttpRequestMessage request, bool incluseAccessToken = true, bool includeDeviceSignature = true) {

        // security secret
        request.Headers.TryAddWithoutValidation("x-security-secret", _securitySettings.Secret);

        // user agent
        var userAgent = _httpContextAccessor.HttpContext?.Request.Headers.UserAgent.ToString();
        if(!string.IsNullOrWhiteSpace(userAgent)) {
            request.Headers.TryAddWithoutValidation("User-Agent", userAgent);
        }

        // device id
        try {
            string? deviceId = await _localStorage.GetDeviceId();
            if(string.IsNullOrWhiteSpace(deviceId)) {
                deviceId = Guid.NewGuid().ToString();
                await _localStorage.SetDeviceId(deviceId);
            }

            request.Headers.TryAddWithoutValidation("x-device-id", deviceId);

        } catch(Exception ex) {
            _logger.LogWarning("Unable to retrieve or generate device ID: {Message}", ex.Message);
        }

        // access token
        if(incluseAccessToken) {
            var tokens = await _sessionStorage.GetToken();
            if(tokens != null && !string.IsNullOrEmpty(tokens.AccessToken)) {
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", tokens.AccessToken);

            } else {
                _logger.LogError("Access token is required, but it's missing");
                return false;
            }
        }

        // device signature
        if(includeDeviceSignature) {
            try {
                byte[]? privateKeyBytes = await _localStorage.GetPrivateKey();
                if(privateKeyBytes != null && privateKeyBytes.Length > 0) {

                    long timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
                    string signature = _providerService.SignPayload(timestamp.ToString(), privateKeyBytes);

                    request.Headers.Add("x-device-timestamp", timestamp.ToString());
                    request.Headers.Add("x-device-signature", signature);

                } else {
                    _logger.LogError("Device signature is required, but it's missing");
                    return false;
                }

            } catch(Exception ex) {
                _logger.LogWarning("Unable to generate device signature: {Message}", ex.Message);
                return false;
            }
        }

        return true;
    }
}