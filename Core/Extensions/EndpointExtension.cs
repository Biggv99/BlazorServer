using OptimusPrime.Features.Endpoints;

namespace OptimusPrime.Core.Extensions;

public static class EndpointExtension {
    public static IEndpointRouteBuilder MapAppEndpoint(this IEndpointRouteBuilder app) {

        // ==========================
        // Endpoint mapping
        // ==========================

        // system
        app.MapAppVersionEndpoint();
        app.MapApplication();

        // auth
        app.MapRegisterEndpoint();
        app.MapLoginEndpoint();

        app.MapConfirmEmailEndpoint();
        app.MapResendConfirmationEmailEndpoint();

        app.MapForgotPasswordEndpoint();
        app.MapResetPasswordEndpoint();

        app.MapRefreshEndpoint();

        app.MapResetDeviceEndpoint();

        // profile
        app.MapMeEndpoint();

        // instruction
        app.MapInstructionEndpoint();

        return app;
    }
}