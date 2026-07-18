using OptimusPrime.Core.Network;
using OptimusPrime.Core.Utils;
using OptimusPrime.Features.Contracts.Commands;
using OptimusPrime.Features.Endpoints.Shared;
using OptimusPrime.Features.Handlers;
using OptimusPrime.Features.Validators;

namespace OptimusPrime.Features.Endpoints;

public static class InstructionEndpoint {

    private const string entityName = "INSTRUCTION";

    // ==========================
    // Add
    // ==========================
    public static void MapInstructionEndpoint(this IEndpointRouteBuilder app) {
        app.MapPost(ApiRoutes.Instruction, async (
            HttpContext context,
            InstructionCommand command,
            InstructionHandler handler,
            AuditHandler auditHandler ) => {

                const string actionType = "ADD";

                // headers extraction
                var headerResult = command.ExtractHeaders(context);
                if(!headerResult.IsSuccess) {
                    await auditHandler.HandleSave(command, actionType, entityName, headerResult.Message);
                    return Result<bool>.Failure(headerResult).ToHttpResult();
                }

                Result<bool> result = await handler.HandleAdd(command);
                return result.ToHttpResult();

            })
            .RequireRateLimiting("Protected")
            .RequireAuthorization("AdminOnly")
            .AddEndpointFilter<ValidationFilter<InstructionCommand, InstructionValidator>>();
    }
}