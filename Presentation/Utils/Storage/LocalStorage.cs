using Microsoft.AspNetCore.Components.Server.ProtectedBrowserStorage;

namespace OptimusPrime.Presentation.Utils.Storage;

public class LocalStorage(ProtectedLocalStorage protectedLocalStore, ILogger<LocalStorage> logger) {

    private readonly ProtectedLocalStorage _protectedLocalStore = protectedLocalStore;
    private readonly ILogger<LocalStorage> _logger = logger;

    // ==========================
    // Private Key
    // ==========================

    public async Task<bool> SetPrivateKey(byte[] privateKey) {
        try {
            string privateKeyBase64 = Convert.ToBase64String(privateKey);
            await _protectedLocalStore.SetAsync("DevicePrivateKey", privateKeyBase64);
            return true;

        } catch(Exception ex) {
            _logger.LogError("Error saving private key: {Message}", ex.Message);
            return false;
        }
    }

    public async Task<byte[]?> GetPrivateKey() {
        try {
            var result = await _protectedLocalStore.GetAsync<string>("DevicePrivateKey");

            if(result.Success && !string.IsNullOrEmpty(result.Value)) return Convert.FromBase64String(result.Value);
            return null;

        } catch(Exception ex) {
            _logger.LogWarning("Error reading private key: {Message}", ex.Message);
            return null;
        }
    }

    public async Task<bool> ClearPrivateKey() {
        try {
            await _protectedLocalStore.DeleteAsync("DevicePrivateKey");
            return true;

        } catch(Exception ex) {
            _logger.LogError("Error clearing private key: {Message}", ex.Message);
            return false;
        }
    }

    // ==========================
    // Device ID
    // ==========================

    public async Task<bool> SetDeviceId(string deviceId) {
        try {
            await _protectedLocalStore.SetAsync("DeviceId", deviceId);
            return true;

        } catch(Exception ex) {
            _logger.LogError("Error saving device ID: {Message}", ex.Message);
            return false;
        }
    }

    public async Task<string?> GetDeviceId() {
        try {
            var result = await _protectedLocalStore.GetAsync<string>("DeviceId");

            if(result.Success && !string.IsNullOrEmpty(result.Value)) return result.Value;
            return null;

        } catch(Exception ex) {
            _logger.LogWarning("Error reading device ID: {Message}", ex.Message);
            return null;
        }
    }

    public async Task<bool> ClearDeviceId() {
        try {
            await _protectedLocalStore.DeleteAsync("DeviceId");
            return true;

        } catch(Exception ex) {
            _logger.LogError("Error clearing device ID: {Message}", ex.Message);
            return false;
        }
    }
}