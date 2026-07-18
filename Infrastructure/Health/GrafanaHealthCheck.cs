using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace OptimusPrime.Infrastructure.Health;

public class GrafanaHealthCheck(HttpClient httpClient) : IHealthCheck {

    private readonly HttpClient _httpClient = httpClient;
    private readonly string _grafanaUrl = "http://grafana:3000/api/health";

    // ====================================================
    // Health Check: Grafana
    // ====================================================

    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default) {
        try {
            var response = await _httpClient.GetAsync(_grafanaUrl, cancellationToken);

            if(!response.IsSuccessStatusCode) {
                return HealthCheckResult.Unhealthy("Grafana not responding");
            }

            return HealthCheckResult.Healthy("Grafana online!");

        } catch(Exception) {
            return HealthCheckResult.Unhealthy("Grafana connection error");
        }
    }
}

// rate(microsoft_aspnetcore_hosting_http_server_request_duration_sum{ http_route =~"/api/.*"}[5m]) / rate(microsoft_aspnetcore_hosting_http_server_request_duration_count{ http_route =~"/api/.*"}[5m])
// {{http_route}}