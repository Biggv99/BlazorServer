using Microsoft.Extensions.Diagnostics.HealthChecks;
using StackExchange.Redis; 

namespace OptimusPrime.Infrastructure.Health;

public class RedisHealthCheck(IConnectionMultiplexer redisConnection) : IHealthCheck {

    private readonly IConnectionMultiplexer _redisConnection = redisConnection;

    // ====================================================
    // Health Check: Redis
    // ====================================================
    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default) {
        try {
            var database = _redisConnection.GetDatabase();

            var reply = await database.PingAsync();
            if(reply.Ticks > 0) {
                return HealthCheckResult.Healthy("Redis online");
            }

            return HealthCheckResult.Unhealthy("Redis not responding");

        } catch(Exception) {
            return HealthCheckResult.Unhealthy("Redis connection error");
        }
    }
}