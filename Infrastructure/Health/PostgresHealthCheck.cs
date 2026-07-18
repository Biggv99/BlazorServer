using Microsoft.Extensions.Diagnostics.HealthChecks;
using OptimusPrime.Infrastructure.Database;

namespace OptimusPrime.Infrastructure.Health;

public class PostgresHealthCheck(AppDbcontext appDBcontext) : IHealthCheck {

    private readonly AppDbcontext _appDBcontext = appDBcontext;

    // ====================================================
    // Health Check: PostgreSQL
    // ====================================================

    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default) {
        try {
            bool isConnected = await _appDBcontext.Database.CanConnectAsync(cancellationToken);

            if (!isConnected) {
                return HealthCheckResult.Unhealthy("Postgres not responding");
            } 

            return HealthCheckResult.Healthy("Postgres online");

        } catch (Exception) {
            return HealthCheckResult.Unhealthy("Postgres connection error");
        }
    }
}