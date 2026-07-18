using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;
using OptimusPrime.Core.Settings;

namespace OptimusPrime.Infrastructure.Health;

public class FirebaseHealthCheck(
    HttpClient httpClient, 
    IOptions<FirebaseSettings> options) : IHealthCheck {

    private readonly HttpClient _httpClient = httpClient;
    private readonly FirebaseSettings _settings = options.Value;

    // ====================================================
    // Health Check: Firebase
    // ====================================================

    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default) {
        try {
            string encodedToken = Uri.EscapeDataString(_settings.AuthToken);
            var response = await _httpClient.GetAsync($"{_settings.BaseUrl}/.json?shallow=true&auth={encodedToken}", cancellationToken);

            if(!response.IsSuccessStatusCode) {    
                return HealthCheckResult.Unhealthy("Firebase not responding");
            }

            return HealthCheckResult.Healthy("Firebase online");

        } catch(Exception) {
            return HealthCheckResult.Unhealthy("Firebase connection error");
        }
    }
}