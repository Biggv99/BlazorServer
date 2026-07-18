using OptimusPrime.Core.Network;
using OptimusPrime.Core.Utils;
using OptimusPrime.Features.Contracts.Commands;
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
}