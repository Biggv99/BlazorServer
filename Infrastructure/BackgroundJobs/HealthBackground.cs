using Microsoft.Extensions.Diagnostics.HealthChecks;
using OptimusPrime.Core.Network;
using OptimusPrime.Core.Utils;
using OptimusPrime.Features.Contracts.Models;
using OptimusPrime.Features.Interfaces;
using OptimusPrime.Infrastructure.Notifications;
using OptimusPrime.Presentation.Utils.States;

namespace OptimusPrime.Infrastructure.BackgroundJobs;

public class HealthBackground(
    IServiceProvider serviceProvider,
    IFirebaseRepository firebaseRepository,
    HealthNotification healthNotification,
    HealthState healthState,
    ILogger<HealthBackground> logger) : BackgroundService {

    private readonly IServiceProvider _serviceProvider = serviceProvider;
    private readonly IFirebaseRepository _firebaseRepository = firebaseRepository;
    private readonly HealthNotification _healthNotification = healthNotification;
    private readonly HealthState _healthState = healthState;
    private readonly ILogger<HealthBackground> _logger = logger;
    private bool? notifiedHealthy = null;

    // ========================================================================================================
    // Periodic Health Check to monitor the health status of the system and send notifications
    // ========================================================================================================

    protected override async Task ExecuteAsync(CancellationToken stoppingToken) {
        await Task.Delay(5000, stoppingToken);
        _logger.LogInformation("Health Check started");

        // initial execution
        await RunHealthCheck(stoppingToken);

        // periodic execution
        using var timer = new PeriodicTimer(TimeSpan.FromSeconds(15));
        try {
            while(await timer.WaitForNextTickAsync(stoppingToken)) {
                await RunHealthCheck(stoppingToken);
            }

        } catch(OperationCanceledException) {
            _logger.LogInformation("Health Check stopped gracefully");

        } catch(Exception ex) {
            _logger.LogError("Health Check error. Stopping execution: {Message}", ex.Message);
        }
    }

    private async Task RunHealthCheck(CancellationToken stoppingToken) {
        try {

            // create a scope to resolve scoped services
            using var scope = _serviceProvider.CreateScope();

            var healthCheckService = scope.ServiceProvider.GetRequiredService<HealthCheckService>();
            HealthReport report = await healthCheckService.CheckHealthAsync(stoppingToken);

            // get the list of services: name and health status
            var services = report.Entries.Select(e =>
                new HealthModel {
                    Name = e.Key,
                    IsActive = e.Value.Status == HealthStatus.Healthy,
                }
            ).ToList();

            bool isGlobalSuccess = report.Status == HealthStatus.Healthy;

            var result = isGlobalSuccess
                ? Result<List<HealthModel>>.Success(services, "All services are operational")
                : Result<List<HealthModel>>.Failure(
                    services,
                    "Some services are not responding: " + string.Join(", ", services.Where(s => !s.IsActive).Select(s => s.Name)),
                    ErrorType.DatabaseError);

            if(result.Value != null) {
                _healthState.UpdateHealthState(result.Value);

                if(!result.IsSuccess) {
                    if(notifiedHealthy == true || notifiedHealthy == null) {
                        await _healthNotification.SendNotification("⚠️ System unhealthy", result.Message, result.Value);
                        await _firebaseRepository.UpdateHealthStatus(result.Value);
                        notifiedHealthy = false;
                    }
                    _logger.LogWarning(result.Message);

                } else {
                    if(notifiedHealthy == false || notifiedHealthy == null) {
                        await _healthNotification.SendNotification("✅ System healthy", result.Message, result.Value);
                        await _firebaseRepository.UpdateHealthStatus(result.Value);
                        notifiedHealthy = true;
                    }
                }

            } else {
                if(notifiedHealthy == true || notifiedHealthy == null) {
                    List<HealthModel> unhealthyServices = HealthUnhealthyModel.GetUnhealthyList();

                    await _healthNotification.SendNotification("⚠️ System error", result.Message, unhealthyServices);
                    await _firebaseRepository.UpdateHealthStatus(unhealthyServices);

                    notifiedHealthy = false;
                }
                _logger.LogWarning("Empty result for Health Check: {Message}", result.Message);
            }

        } catch(Exception ex) {
            _logger.LogError("Error during Health Check: {Message}", ex.Message);
        }
    }
}