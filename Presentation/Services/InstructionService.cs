using OptimusPrime.Core.Network;
using OptimusPrime.Core.Utils;
using OptimusPrime.Features.Contracts.Commands;
using OptimusPrime.Presentation.Services.Shared;

namespace OptimusPrime.Presentation.Services;

public class InstructionService(
    HttpClient http,
    Interceptor interceptor,
    ILogger<InstructionService> logger ) {

    private readonly HttpClient _http = http;
    private readonly Interceptor _interceptor = interceptor;
    private readonly ILogger<InstructionService> _logger = logger;

    // ==========================
    // Add instruction
    // ==========================
    public async Task<Result<bool>> Instruction(InstructionCommand instruction) {

        using var request = new HttpRequestMessage(HttpMethod.Post, ApiRoutes.Instruction) {

            // body
            Content = JsonContent.Create(instruction)
        };

        // headers
        if(!await _interceptor.InterceptorHeaders(request)) {
            return Result<bool>.Failure("Dati richiesti mancanti. Riprovare l'accesso.", ErrorType.Unauthorized);
        }

        // invio richiesta
        try {
            var response = await _http.SendAsync(request);
            return await response.Content.ReadFromJsonAsync<Result<bool>>() ?? Result<bool>.Failure("Il server non risponde", ErrorType.ExternalServiceError);

        } catch(Exception ex) {
            _logger.LogError($"Errore di comunicazione con il server: {ex.Message}");
            return Result<bool>.Failure("Errore di comunicazione con il server", ErrorType.ExternalServiceError);
        }
    }
}
