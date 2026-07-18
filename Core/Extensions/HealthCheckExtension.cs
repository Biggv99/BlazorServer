using OptimusPrime.Features.Contracts.Models;
using OptimusPrime.Infrastructure.BackgroundJobs;
using OptimusPrime.Infrastructure.Health;
using OptimusPrime.Infrastructure.Notifications;
using OptimusPrime.Presentation.Utils.States;

namespace OptimusPrime.Core.Extensions;

public static class HealthCheckExtension {

    public static IServiceCollection AddAppHealthCheck(this IServiceCollection services, IConfiguration configuration, IWebHostEnvironment env)
    {
        // ====================================================
        // Health Check configuration
        // ====================================================

        var baseUrl = configuration.GetValue<string>("AppConfig:BaseUrl");
        if (string.IsNullOrWhiteSpace(baseUrl)) throw new Exception("No base URL configured");

        var healthChecks = services.AddHealthChecks()
            .AddCheck<FirebaseHealthCheck>(HealthServiceName.firebase)
            .AddCheck<PostgresHealthCheck>(HealthServiceName.postgres)
            .AddCheck<RedisHealthCheck>(HealthServiceName.redis);

        if (env.IsDevelopment())
        {
            healthChecks.AddCheck<PrometheusHealthCheck>(HealthServiceName.prometheus)
                        .AddCheck<GrafanaHealthCheck>(HealthServiceName.grafana);
        }

        services.AddSingleton<HealthState>();
        services.AddSingleton<HealthNotification>();
        services.AddHostedService<HealthBackground>();

        return services;
    }
}