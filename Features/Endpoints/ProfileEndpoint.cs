using OptimusPrime.Core.Network;
using OptimusPrime.Core.Utils;
using OptimusPrime.Features.Contracts.Commands.Shared;
using OptimusPrime.Features.Contracts.Models;
using OptimusPrime.Features.Endpoints.Shared;
using OptimusPrime.Features.Handlers;

namespace OptimusPrime.Features.Endpoints;

public static class ProfileEndpoint {

    private const string entityName = "PROFILE";

    // ==========================
    // Me
    // ==========================
    public static void MapMeEndpoint(this IEndpointRouteBuilder app) {
        app.MapGet(ApiRoutes.Me, async (
            HttpContext context,
            ProfileHandler handler,
            AuditHandler auditHandler ) => {

                const string actionType = "ME";

                BaseTrackedCommand command = new();

                // headers extraction
                var headerResult = command.ExtractHeaders(context, requireDeviceChallenge: false);
                if (!headerResult.IsSuccess) {
                    await auditHandler.HandleSave(command, actionType, entityName, headerResult.Message);
                    return Result<UserModel>.Failure(headerResult).ToHttpResult();
                }

                Result<UserModel> result = await handler.HandleMe(command);
                return result.ToHttpResult();

            })
            .RequireAuthorization();
    }
}