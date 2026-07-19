using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.EntityFrameworkCore;
using OptimusPrime.Core.Extensions;
using OptimusPrime.Features.Contracts.Models;
using OptimusPrime.Features.Interfaces;
using OptimusPrime.Infrastructure.Notifications;
using Prometheus;

namespace OptimusPrime;

static class Program {

    private static WebApplication? app = null;

    static void Main(string[] args) {
        try {
            var builder = WebApplication.CreateBuilder(args);

            // ==========================
            // Dependency injection
            // ==========================
            builder.Services.AddAppSecurity(builder.Configuration);
            builder.Services.AddAppRateLimiter();
            builder.Services.AddAppHealthCheck(builder.Configuration, builder.Environment);
            builder.Services.AddAppFeature(builder.Configuration);
            if(builder.Environment.IsDevelopment()) {
                builder.Services.AddAppPresentation(builder.Configuration);
            }
            builder.Services.AddAppDatabase(builder.Configuration);

            // ==========================
            // Pipeline building
            // ==========================
            app = builder.Build();

            // shutdown
            var lifetime = app.Services.GetRequiredService<IHostApplicationLifetime>();
            lifetime.ApplicationStopping.Register(() => {
                OnShutdown();
            });

            // ==========================
            // ForwardedHeaders 
            // ==========================
            var forwardedHeadersOptions = new ForwardedHeadersOptions {
                ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto
            };
            forwardedHeadersOptions.KnownIPNetworks.Clear(); 
            forwardedHeadersOptions.KnownProxies.Clear();

            app.UseForwardedHeaders(forwardedHeadersOptions);

            app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);

            if(app.Environment.IsDevelopment()) {
                app.MapStaticAssets();
            }

            app.UseAuthentication();
            app.UseAuthorization();

            app.UseRateLimiter();
            app.UseAntiforgery();

            app.MapAppEndpoint();
            if(app.Environment.IsDevelopment()) {
                app.MapRazorComponents<Presentation.App>().AddInteractiveServerRenderMode();
                app.MapMetrics().RequireHost("*:9091");
            }

            // ==========================
            // EF Core Migration 
            // ==========================
            using(var scope = app.Services.CreateScope()) {
                var dbContext = scope.ServiceProvider.GetRequiredService<Infrastructure.Database.AppDbcontext>();
                dbContext.Database.Migrate();
            }

            app.Logger.LogInformation("Server started 🚀");
            app.Run();

        } catch(Exception ex) {
            OnShutdown();
            throw new Exception($"Error during application startup: {ex}", ex);
        }
    }

    // try to catch shutdown event to update firebase status and send notification
    private static void OnShutdown() {
        if(app == null)
            return;

        try {
            using var scope = app.Services.CreateScope();
            var healthNotification = scope.ServiceProvider.GetRequiredService<HealthNotification>();
            var firebaseRepository = scope.ServiceProvider.GetRequiredService<IFirebaseRepository>();

            List<HealthModel> unhealthyServices = HealthUnhealthyModel.GetUnhealthyList();

            healthNotification.SendNotification("🚨 Server shutdown", "Server is offline, all systems are inactive", unhealthyServices).GetAwaiter().GetResult();
            firebaseRepository.UpdateHealthStatus(unhealthyServices).GetAwaiter().GetResult();

            app.Logger.LogWarning("Server shutdown. Updated status on Firebase and sent notification");

        } catch(Exception ex) {
            app.Logger.LogError("Server shutdown. Error updating status on Firebase: {Message}", ex.Message);
        }
    }
}