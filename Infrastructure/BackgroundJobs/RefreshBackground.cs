using OptimusPrime.Features.Interfaces;

namespace OptimusPrime.Infrastructure.BackgroundJobs;

public class RefreshBackground(
    IServiceProvider serviceProvider,
    ILogger<RefreshBackground> logger) : BackgroundService { 

    private readonly IServiceProvider _serviceProvider = serviceProvider;
    private readonly ILogger<RefreshBackground> _logger = logger;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken) {
        await Task.Delay(5000, stoppingToken);
        _logger.LogInformation("Refresh Check started");

        // initial execution
        await RunDeleteRefesh(stoppingToken);

        // periodic execution
        using var timer = new PeriodicTimer(TimeSpan.FromMinutes(5));
        try {
            while(await timer.WaitForNextTickAsync(stoppingToken)) {
                await RunDeleteRefesh(stoppingToken);
            }

        } catch(OperationCanceledException) {
            _logger.LogInformation("Refresh Check stopped gracefully.");

        } catch(Exception ex) {
            _logger.LogError("Refresh Check error. Stopping execution: {Message}", ex.Message);
        }
    }

    private async Task RunDeleteRefesh(CancellationToken stoppingToken) {
        try {

            // create a scope to resolve scoped services
            using var scope = _serviceProvider.CreateScope();
            var scopedRefreshRepository = scope.ServiceProvider.GetRequiredService<IAuthRepository>();

            await scopedRefreshRepository.DeleteInvalidRefreshTokens();

        } catch(Exception ex) {
            _logger.LogError("Error during refresh token deletion: {Message}", ex.Message);
        }
    }
}