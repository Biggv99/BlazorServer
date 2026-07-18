using OptimusPrime.Core.Network;
using OptimusPrime.Core.Security.Crypto;
using OptimusPrime.Core.Utils;
using OptimusPrime.Features.Contracts.Commands;
using OptimusPrime.Features.Contracts.Commands.Shared;
using OptimusPrime.Features.Contracts.Models;
using OptimusPrime.Features.Handlers;
using OptimusPrime.Presentation.Services.Shared;
using OptimusPrime.Presentation.Utils.Storage;
using System.Text.Json;

namespace OptimusPrime.Presentation.Services;

public class AuthService(
    HttpClient http,
    AuthHandler userHandler,
    ProviderService providerService,
    LocalStorage localStorage,
    SessionStorage sessionStorage,
    Interceptor interceptor,
    ILogger<AuthService> logger) {

    private readonly HttpClient _http = http;
    private readonly AuthHandler _userHandler = userHandler;
    private readonly Interceptor _interceptor = interceptor;
    private readonly ProviderService _providerService = providerService;
    private readonly LocalStorage _localStorage = localStorage;
    private readonly SessionStorage _sessionStorage = sessionStorage;
    private readonly ILogger<AuthService> _logger = logger;

    // ==========================
    // Registration
    // ==========================
    public async Task<Result<bool>> Register(RegisterCommand command) {

        // diffie-hellman
        var (user, tempPrivateKey) = _providerService.ClientRequest(command);

        var request = new HttpRequestMessage(HttpMethod.Post, ApiRoutes.Register) {

            // body
            Content = JsonContent.Create(user)
        };

        // headers
        if(!await _interceptor.InterceptorHeaders(request, false, false)) {
            return Result<bool>.Failure("Missing required data. Try logging again", ErrorType.Unauthorized);
        }

        // send request
        try {
            var response = await _http.SendAsync(request);
            var result = await response.Content.ReadFromJsonAsync<Result<string>>();

            if(result == null || !result.IsSuccess || string.IsNullOrEmpty(result.Value))
                return Result<bool>.Failure(result?.Message ?? "Server error", ErrorType.ExternalServiceError);

            byte[]? privateKey = _providerService.ClientResponse(result.Value, tempPrivateKey);
            if(privateKey == null)
                return Result<bool>.Failure("Error unpacking keys", ErrorType.InternalError);
            await _localStorage.SetPrivateKey(privateKey);

            return Result<bool>.Success("Registration successful");

        } catch(Exception ex) {
            _logger.LogError($"Error communicating with the server: {ex.Message}");
            return Result<bool>.Failure("Error communicating with the server", ErrorType.ExternalServiceError);
        }
    }

    // ==========================
    // Login
    // ==========================
    public async Task<Result<JwtTokenModel>> Login(LoginCommand command) {

        var request = new HttpRequestMessage(HttpMethod.Post, ApiRoutes.Login) {

            // body
            Content = JsonContent.Create(command)
        };

        // headers
        if(!await _interceptor.InterceptorHeaders(request, incluseAccessToken: false)) {
            return Result<JwtTokenModel>.Failure("Missing required data. Try logging again", ErrorType.Unauthorized);
        }

        // send request
        try {
            var response = await _http.SendAsync(request);
            return await response.Content.ReadFromJsonAsync<Result<JwtTokenModel>>() ?? Result<JwtTokenModel>.Failure("Server not responding", ErrorType.ExternalServiceError);

        } catch(Exception ex) {
            _logger.LogError($"Error communicating with the server: {ex.Message}");
            return Result<JwtTokenModel>.Failure("Error communicating with the server", ErrorType.ExternalServiceError);
        }
    }

    // ==========================
    // Resend confirmation email
    // ==========================
    public async Task<Result<bool>> ResendConfirmationEmail(EmailCommand command) {

        var request = new HttpRequestMessage(HttpMethod.Post, ApiRoutes.ResendConfirmEmail) {

            // body
            Content = JsonContent.Create(command)
        };

        // headers
        if (!await _interceptor.InterceptorHeaders(request, false, false)) {
            return Result<bool>.Failure("Missing required data. Try logging again", ErrorType.Unauthorized);
        }

        // send request
        try {
            var response = await _http.SendAsync(request);
            var rawResult = await response.Content.ReadFromJsonAsync<Result<JsonElement>>();
            return ParseBoolResult(rawResult);

        } catch (Exception ex) {
            _logger.LogError($"Error communicating with the server: {ex.Message}");
            return Result<bool>.Failure("Error communicating with the server", ErrorType.ExternalServiceError);
        }
    }

    // ==========================
    // Forgot password
    // ==========================
    public async Task<Result<bool>> ForgotPassword(EmailCommand command) {

        var request = new HttpRequestMessage(HttpMethod.Post, ApiRoutes.ForgotPassword) {

            // body
            Content = JsonContent.Create(command)
        };

        // headers
        if (!await _interceptor.InterceptorHeaders(request, false, false)) {
            return Result<bool>.Failure("Missing required data. Try logging again", ErrorType.Unauthorized);
        }

        // send request
        try {
            var response = await _http.SendAsync(request);
            var rawResult = await response.Content.ReadFromJsonAsync<Result<JsonElement>>();
            return ParseBoolResult(rawResult);

        } catch (Exception ex) {
            _logger.LogError($"Error communicating with the server: {ex.Message}");
            return Result<bool>.Failure("Error communicating with the server", ErrorType.ExternalServiceError);
        }
    }

    // ==========================
    // Reset password
    // ==========================
    public async Task<Result<bool>> ResetPassword(ResetPasswordCommand command) {

        var request = new HttpRequestMessage(HttpMethod.Post, ApiRoutes.ResetPassword) {

            // body
            Content = JsonContent.Create(command)
        };

        // headers
        if (!await _interceptor.InterceptorHeaders(request, incluseAccessToken: false)) {
            return Result<bool>.Failure("Missing required data. Try logging again", ErrorType.Unauthorized);
        }

        // send request
        try {
            var response = await _http.SendAsync(request);
            var rawResult = await response.Content.ReadFromJsonAsync<Result<JsonElement>>();
            return ParseBoolResult(rawResult);

        } catch (Exception ex) {
            _logger.LogError($"Error communicating with the server: {ex.Message}");
            return Result<bool>.Failure("Error communicating with the server", ErrorType.ExternalServiceError);
        }
    }

    // ==========================
    // Refresh
    // ==========================
    public async Task<Result<JwtTokenModel>> Refresh(RefreshCommand refresh) {

        using var request = new HttpRequestMessage(HttpMethod.Post, ApiRoutes.Refresh) {

            // body
            Content = JsonContent.Create(refresh)
        };

        // headers
        if (!await _interceptor.InterceptorHeaders(request)) {
            return Result<JwtTokenModel>.Failure("Dati richiesti mancanti. Riprovare l'accesso.", ErrorType.Unauthorized);
        }

        // invio richiesta
        try {
            var response = await _http.SendAsync(request);
            return await response.Content.ReadFromJsonAsync<Result<JwtTokenModel>>() ?? Result<JwtTokenModel>.Failure("Il server non risponde", ErrorType.ExternalServiceError);

        } catch (Exception ex) {
            _logger.LogError($"Errore di comunicazione con il server: {ex.Message}");
            return Result<JwtTokenModel>.Failure("Errore di comunicazione con il server", ErrorType.ExternalServiceError);
        }
    }

    // ==========================
    // Delete all
    // ==========================
    public async Task<Result<bool>> DeleteAll() {

        JwtTokenModel? token = await _sessionStorage.GetToken();
        if (token == null) { 
            return Result<bool>.Failure("Missing required data. Try logging again", ErrorType.Unauthorized);
        }

        BaseTrackedCommand request = new() {
            AccessToken = token.AccessToken,
        };

        return await _userHandler.HandleDeleteAll(request);
    }

    // ==========================
    // Helpers
    // ==========================
    private static Result<bool> ParseBoolResult(Result<JsonElement>? rawResult) {
        if (rawResult == null) {
            return Result<bool>.Failure("Server not responding", ErrorType.ExternalServiceError);
        }

        if (!rawResult.IsSuccess) {
            return Result<bool>.Failure(rawResult.Message, rawResult.Error);
        }

        if (rawResult.Value.ValueKind == JsonValueKind.True || rawResult.Value.ValueKind == JsonValueKind.False) {
            return Result<bool>.Success(rawResult.Value.GetBoolean(), rawResult.Message);
        }

        return Result<bool>.Success(rawResult.Message);
    }
}