using FirebaseAdmin.Messaging;
using OptimusPrime.Core.Network;
using OptimusPrime.Core.Utils;
using OptimusPrime.Features.Contracts.Commands;
using OptimusPrime.Features.Contracts.Commands.Shared;
using OptimusPrime.Features.Contracts.Models;
using OptimusPrime.Features.Endpoints.Shared;
using OptimusPrime.Features.Handlers;

namespace OptimusPrime.Features.Endpoints;

public static class SystemEndpoint {

    private const string entityName = "SYSTEM";

    // ==========================
    // App Version
    // ==========================
    public static void MapAppVersionEndpoint(this IEndpointRouteBuilder app) {
        app.MapPost(ApiRoutes.AppVersion, async (
            HttpContext context,
            AppVersionCommand command,
            SystemHandler handler,
            AuditHandler auditHandler ) => {

                const string actionType = "APP VERSION";

                // headers extraction
                var headerResult = command.ExtractHeaders(context, false, false);
                if (!headerResult.IsSuccess) {
                    await auditHandler.HandleSave(command, actionType, entityName, headerResult.Message);
                    return Result<AppVersionModel>.Failure(headerResult).ToHttpResult();
                }

                Result<AppVersionModel> result = await handler.HandleAppVersion(command);
                return result.ToHttpResult();

            })
            .RequireRateLimiting("Protected");
    }

    // ==========================
    // Get latest application
    // ==========================
    public static void MapApplication(this IEndpointRouteBuilder app) {
        app.MapGet(ApiRoutes.Application, async (
            HttpContext context,
            AuditHandler auditHandler) => {

                const string actionType = "APPLICATION DOWNLOAD";

                BaseTrackedCommand command = new();

                // headers extraction
                var headerResult = command.ExtractHeaders(context, false, false);
                if(!headerResult.IsSuccess) {
                    await auditHandler.HandleSave(command, actionType, entityName, headerResult.Message);
                    return Result<bool>.Failure(headerResult).ToHttpResult();
                }

                var filePath = "/app/downloads/app-release.apk";
                if(!File.Exists(filePath)) {
                    await auditHandler.HandleSave(command, actionType, entityName, "APK not found");
                    return Result<bool>.Failure("APK not found", ErrorType.NotFound).ToHttpResult();
                }

                await auditHandler.HandleSave(command, actionType, entityName, "APK delivered");

                return Results.File(
                    filePath,
                    contentType: "application/vnd.android.package-archive",
                    fileDownloadName: "CasadelCristallo.apk"
                );
            })
            .RequireRateLimiting("Protected");
    }
}