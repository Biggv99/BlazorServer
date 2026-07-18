using OptimusPrime.Presentation.Services;
using OptimusPrime.Presentation.Services.Shared;
using OptimusPrime.Presentation.Utils.States;
using OptimusPrime.Presentation.Utils.Storage;
using Prometheus;

namespace OptimusPrime.Core.Extensions;

public static class PresentationExtension {
    public static IServiceCollection AddAppPresentation(this IServiceCollection services, IConfiguration configuration) {

        // ====================================================
        // Presentation: Services and Components
        // ====================================================
        var baseUrl = configuration.GetValue<string>("AppConfig:BaseUrl");
        if (string.IsNullOrWhiteSpace(baseUrl)) throw new Exception("No base URL configured");

        services.AddRazorComponents().AddInteractiveServerComponents();
        services.AddHttpContextAccessor();

        // interceptor
        services.AddScoped<Interceptor>();

        services.AddHttpClient<InstructionService>(c => c.BaseAddress = new Uri(baseUrl));
        services.AddHttpClient<AuthService>(c => c.BaseAddress = new Uri(baseUrl));
        services.AddHttpClient<SystemService>(c => c.BaseAddress = new Uri(baseUrl));
        services.AddHttpClient<ProfileService>(c => c.BaseAddress = new Uri(baseUrl));
        services.AddScoped<AuditService>();

        services.AddHttpClient("PokeApi").UseHttpClientMetrics();
        services.AddScoped<PokemonService>();

        // state e storage
        services.AddScoped<DashboardState>();
        services.AddScoped<JwtTimerState>();
        services.AddScoped<SessionStorage>();
        services.AddScoped<LocalStorage>();

        return services;
    }
}