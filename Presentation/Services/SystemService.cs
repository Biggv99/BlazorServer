using OptimusPrime.Core.Network;
using OptimusPrime.Core.Utils;
using OptimusPrime.Features.Contracts.Commands;
using OptimusPrime.Features.Contracts.Models;
using OptimusPrime.Presentation.Services.Shared;

namespace OptimusPrime.Presentation.Services;

public class SystemService(
    HttpClient http,
    Interceptor interceptor,
    ILogger<SystemService> logger) {

    private readonly HttpClient _http = http;
    private readonly Interceptor _interceptor = interceptor;
    private readonly ILogger<SystemService> _logger = logger;

    // ==========================
    // Application version
    // ==========================
    public async Task<Result<AppVersionModel>> AppVersion(AppVersionCommand command) {

        using var request = new HttpRequestMessage(HttpMethod.Post, ApiRoutes.AppVersion) {

            // body
            Content = JsonContent.Create(command)
        };

        // headers
        if (!await _interceptor.InterceptorHeaders(request, false, false)) {
            return Result<AppVersionModel>.Failure("Dati richiesti mancanti. Riprovare l'accesso.", ErrorType.Unauthorized);
        }

        // send request
        try {
            var response = await _http.SendAsync(request);
            return await response.Content.ReadFromJsonAsync<Result<AppVersionModel>>() ?? Result<AppVersionModel>.Failure("Il server non risponde", ErrorType.ExternalServiceError);

        } catch (Exception ex) {
            _logger.LogError($"Errore di comunicazione con il server: {ex.Message}");
            return Result<AppVersionModel>.Failure("Errore di comunicazione con il server", ErrorType.ExternalServiceError);
        }
    }
}