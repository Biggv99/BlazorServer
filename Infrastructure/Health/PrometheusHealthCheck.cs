using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace OptimusPrime.Infrastructure.Health;

public class PrometheusHealthCheck(HttpClient httpClient) : IHealthCheck {

    private readonly HttpClient _httpClient = httpClient;
    private readonly string _prometheusUrl = "http://prometheus:9090/-/healthy";

    // ====================================================
    // Health Check: Prometheus
    // ====================================================

    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default) {
        try {
            var response = await _httpClient.GetAsync(_prometheusUrl, cancellationToken);

            if(!response.IsSuccessStatusCode) {
                return HealthCheckResult.Unhealthy("Prometheus not responding");
            }

            return HealthCheckResult.Healthy("Prometheus online!");

        } catch(Exception) {
            return HealthCheckResult.Unhealthy("Prometheus connection error");
        }
    }
}