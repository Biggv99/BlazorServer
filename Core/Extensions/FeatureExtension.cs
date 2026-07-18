using FluentValidation;
using OptimusPrime.Core.Settings;
using OptimusPrime.Features.Handlers;
using OptimusPrime.Features.Handlers.Shared;
using OptimusPrime.Features.Interfaces;
using OptimusPrime.Features.Validators;
using OptimusPrime.Infrastructure.BackgroundJobs;
using OptimusPrime.Infrastructure.Notifications;
using OptimusPrime.Infrastructure.Repository;

namespace OptimusPrime.Core.Extensions;

public static class FeatureExtension {
    public static IServiceCollection AddAppFeature(this IServiceCollection services, IConfiguration configuration) {

        // ====================================================
        // Features: Handlers, Validators, Repositories
        // ====================================================
        services.AddMemoryCache();
        services.Configure<EmailSettings>(configuration.GetSection("Email"));
        services.Configure<AppSettings>(configuration.GetSection("App"));

        services.AddScoped<CheckHandler>();
        services.AddScoped<EmailNotification>();

        services.AddScoped<AuthHandler>();
        services.AddScoped<ProfileHandler>();
        services.AddScoped<InstructionHandler>();
        services.AddScoped<AuditHandler>();
        services.AddScoped<SystemHandler>();

        // scans the assembly for all validators
        services.AddValidatorsFromAssemblyContaining<RefreshValidator>();

        services.AddScoped<IAuditRepository, DatabaseAuditRepository>();
        services.AddScoped<IAuthRepository, DatabaseAuthRepository>();
        services.AddHttpClient<IFirebaseRepository, DatabaseFirebaseRepository>();

        services.AddHostedService<RefreshBackground>();

        return services;
    }
}