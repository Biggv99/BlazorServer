using Microsoft.AspNetCore.Components.Server.ProtectedBrowserStorage;
using OptimusPrime.Features.Contracts.Models;

namespace OptimusPrime.Presentation.Utils.Storage; 
public class SessionStorage(
    ProtectedSessionStorage protectedSessionStore,
    ILogger<SessionStorage> logger) {

    private readonly ProtectedSessionStorage _protectedSessionStore = protectedSessionStore;
    private readonly ILogger<SessionStorage> _logger = logger;

    // ====================================================
    // Collegamento al ProtectedSessionStorage 
    // ====================================================

    // ==========================
    // JWT
    // ==========================

    public async Task<bool> SaveTokens(JwtTokenModel token) {
        try {
            await _protectedSessionStore.SetAsync("TokenType", token.TokenType);
            await _protectedSessionStore.SetAsync("AccessToken", token.AccessToken);
            await _protectedSessionStore.SetAsync("RefreshToken", token.RefreshToken);
            await _protectedSessionStore.SetAsync("AccessExpiresIn", token.AccessExpiresIn);
            await _protectedSessionStore.SetAsync("RefreshExpiresIn", token.RefreshExpiresIn);

            return true;

        } catch(Exception ex) {
            _logger.LogError($"Errore durante il salvataggio dei token: {ex.Message}");
            return false;
        }
    }

    public async Task<JwtTokenModel?> GetToken() {

        ProtectedBrowserStorageResult<string> access;
        ProtectedBrowserStorageResult<string> refresh;

        try {
            access = await _protectedSessionStore.GetAsync<string>("AccessToken");
            if(!access.Success || string.IsNullOrEmpty(access.Value)) return null;
 
            refresh = await _protectedSessionStore.GetAsync<string>("RefreshToken");
            if(!refresh.Success || string.IsNullOrEmpty(refresh.Value)) return null;

        } catch(Exception ex) {
            _logger.LogError($"Errore durante la lettura del token: {ex.Message}"); 
            return null;
        }

        JwtTokenModel jwt = new() {
            AccessToken = access.Value,
            RefreshToken = refresh.Value
        };

        return jwt;
    }

    public async Task<bool> ClearTokens() {
        try {
            await _protectedSessionStore.DeleteAsync("TokenType");
            await _protectedSessionStore.DeleteAsync("AccessToken");
            await _protectedSessionStore.DeleteAsync("RefreshToken");
            await _protectedSessionStore.DeleteAsync("AccessExpiresIn");
            await _protectedSessionStore.DeleteAsync("RefreshExpiresIn");

            return true;

        } catch(Exception ex) {
            _logger.LogError($"Errore durante la cancellazione dei token: {ex.Message}");
            return false;
        }
    }
}
