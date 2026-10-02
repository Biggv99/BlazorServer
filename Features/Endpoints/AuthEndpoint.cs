using OptimusPrime.Core.Network;
using OptimusPrime.Core.Utils;
using OptimusPrime.Features.Contracts.Commands;
using OptimusPrime.Features.Contracts.Models;
using OptimusPrime.Features.Endpoints.Shared;
using OptimusPrime.Features.Handlers;
using OptimusPrime.Features.Validators;

namespace OptimusPrime.Features.Endpoints;

public static class AuthEndpoint {

    private const string entityName = "AUTH";

    // ==========================
    // Registration
    // ==========================
    public static void MapRegisterEndpoint(this IEndpointRouteBuilder app) {
        app.MapPost(ApiRoutes.Register, async (
            RegisterCommand command,
            HttpContext context,
            AuthHandler handler, 
            AuditHandler auditHandler ) => {

                const string actionType = "REGISTRATION";

                // headers extraction
                var headerResult = command.ExtractHeaders(context, false, false);
                if(!headerResult.IsSuccess) {
                    await auditHandler.HandleSave(command, actionType, entityName, headerResult.Message);
                    return Result<string>.Failure(headerResult).ToHttpResult();
                }

                Result<string> result = await handler.HandleRegistration(command);
                return result.ToHttpResult();

            })
            .RequireRateLimiting("Protected")
            .AddEndpointFilter<ValidationFilter<RegisterCommand, RegisterValidator>>();
    }

    // ==========================
    // Login
    // ==========================
    public static void MapLoginEndpoint(this IEndpointRouteBuilder app) {
        app.MapPost(ApiRoutes.Login, async (
            LoginCommand command,
            HttpContext context,
            AuthHandler handler,
            AuditHandler auditHandler ) => {

                const string actionType = "LOGIN";
                bool flagged = false;

                // headers extraction
                var headerResult = command.ExtractHeaders(context, requireAccessToken: false, requireLoginCheck: true);
                if(!headerResult.IsSuccess) {
                    await auditHandler.HandleSave(command, actionType, entityName, headerResult.Message);
                    return Result<JwtTokenModel>.Failure(headerResult).ToHttpResult();
                }

                if(headerResult.Message == "LoginFlagged") flagged = true;

                Result<JwtTokenModel> result = await handler.HandleLogin(command, flagged);
                return result.ToHttpResult();

            })
            .RequireRateLimiting("Protected")
            .AddEndpointFilter<ValidationFilter<LoginCommand, LoginValidator>>();
    }

    // ==========================
    // Confirmation email
    // ==========================
    public static void MapConfirmEmailEndpoint(this IEndpointRouteBuilder app) {
        app.MapGet(ApiRoutes.ConfirmEmail, async (
            string token,
            HttpContext context,
            AuthHandler handler ) => {

                ConfirmEmailCommand command = new() {
                    Token = token,
                    IpAddress = context.GetClientIpAddress(),
                    UserAgent = context.GetClientUserAgent()
                };

                Result<bool> result = await handler.HandleConfirmEmail(command);
                return result.ToHttpResult();

            })
            .RequireRateLimiting("Security_secret")
            .AddEndpointFilter<ValidationFilter<ConfirmEmailCommand, ConfirmEmailValidator>>();
    }

    // ==========================
    // Resend confirmation email
    // ==========================
    public static void MapResendConfirmationEmailEndpoint(this IEndpointRouteBuilder app) {
        app.MapPost(ApiRoutes.ResendConfirmEmail, async (
            EmailCommand command,
            HttpContext context,
            AuthHandler handler,
            AuditHandler auditHandler ) => {

                const string actionType = "RESEND CONFIRM EMAIL";

                // headers extraction
                var headerResult = command.ExtractHeaders(context, false, false);
                if (!headerResult.IsSuccess) {
                    await auditHandler.HandleSave(command, actionType, entityName, headerResult.Message);
                    return Result<bool>.Failure(headerResult).ToHttpResult();
                }

                Result<bool> result = await handler.HandleResendConfirmEmail(command);
                return result.ToHttpResult();

            })
            .RequireRateLimiting("Protected")
            .AddEndpointFilter<ValidationFilter<EmailCommand, EmailValidator>>();
    }

    // ==========================
    // Forgot password
    // ==========================
    public static void MapForgotPasswordEndpoint(this IEndpointRouteBuilder app) {
        app.MapPost(ApiRoutes.ForgotPassword, async (
            EmailCommand command,
            HttpContext context,
            AuthHandler handler,
            AuditHandler auditHandler ) => {

                const string actionType = "FORGOT PASSWORD";

                // headers extraction
                var headerResult = command.ExtractHeaders(context, false, false);
                if (!headerResult.IsSuccess) {
                    await auditHandler.HandleSave(command, actionType, entityName, headerResult.Message);
                    return Result<bool>.Failure(headerResult).ToHttpResult();
                }

                Result<bool> result = await handler.HandleForgotPassword(command);
                return result.ToHttpResult();

            })
            .RequireRateLimiting("Protected")
            .AddEndpointFilter<ValidationFilter<EmailCommand, EmailValidator>>();
    }

    // ==========================
    // Reset password
    // ==========================
    public static void MapResetPasswordEndpoint(this IEndpointRouteBuilder app) {
        app.MapPost(ApiRoutes.ResetPassword, async (
            ResetPasswordCommand command,
            HttpContext context,
            AuthHandler handler,
            AuditHandler auditHandler ) => {

                const string actionType = "RESET PASSWORD";

                // headers extraction
                var headerResult = command.ExtractHeaders(context, requireAccessToken: false);
                if (!headerResult.IsSuccess) {
                    await auditHandler.HandleSave(command, actionType, entityName, headerResult.Message);
                    return Result<bool>.Failure(headerResult).ToHttpResult();
                }

                Result<bool> result = await handler.HandleResetPassword(command);
                return result.ToHttpResult();

            })
            .RequireRateLimiting("Protected")
            .AddEndpointFilter<ValidationFilter<ResetPasswordCommand, ResetPasswordValidator>>();
    }

    // ==========================
    // Refresh
    // ==========================
    public static void MapRefreshEndpoint(this IEndpointRouteBuilder app) {

        app.MapPost(ApiRoutes.Refresh, async (
            HttpContext context,
            RefreshCommand command,
            AuthHandler handler,
            AuditHandler auditHandler ) => {

                const string actionType = "REFRESH";

                // headers extraction
                var headerResult = command.ExtractHeaders(context);
                if (!headerResult.IsSuccess) {
                    await auditHandler.HandleSave(command, actionType, entityName, headerResult.Message);
                    return Result<JwtTokenModel>.Failure(headerResult).ToHttpResult();
                }

                Result<JwtTokenModel> result = await handler.HandleRefresh(command);
                return result.ToHttpResult();

            })
            .RequireRateLimiting("Protected")
            .AddEndpointFilter<ValidationFilter<RefreshCommand, RefreshValidator>>();
    }

    // ==========================
    // Reset device
    // ==========================
    public static void MapResetDeviceEndpoint(this IEndpointRouteBuilder app) {
        app.MapPost(ApiRoutes.ResetDevice, async (
            ResetDeviceCommand command,
            HttpContext context,
            AuthHandler handler,
            AuditHandler auditHandler) => {

                const string actionType = "RESET DEVICE";

                // headers extraction
                var headerResult = command.ExtractHeaders(context, false, false);
                if(!headerResult.IsSuccess) {
                    await auditHandler.HandleSave(command, actionType, entityName, headerResult.Message);
                    return Result<JwtTokenModel_DeviceReset>.Failure(headerResult).ToHttpResult();
                }

                Result<JwtTokenModel_DeviceReset> result = await handler.HandleResetDevice(command);
                return result.ToHttpResult();

            })
            .RequireRateLimiting("Protected")
            .AddEndpointFilter<ValidationFilter<ResetDeviceCommand, ResetDeviceValidator>>();
    }
}