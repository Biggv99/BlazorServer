using OptimusPrime.Core.Utils;
using OptimusPrime.Features.Contracts.Commands.Shared;
using OptimusPrime.Features.Contracts.Models;
using OptimusPrime.Features.Handlers.Shared;
namespace OptimusPrime.Features.Handlers;

public class ProfileHandler(
    AuditHandler auditHandler,
    CheckHandler checkHandler ) {

    private readonly AuditHandler _auditHandler = auditHandler;
    private readonly CheckHandler _checkHandler = checkHandler;

    private const string entityName = "PROFILE";

    // ==========================
    // Me 
    // ==========================
    public async Task<Result<UserModel>> HandleMe(BaseTrackedCommand request) {

        const string actionType = "ME";

        // checks
        Result<UserModel> checkResult = await _checkHandler.Check(
            request,
            new CheckOptions { RequireAdmin = false, RequireSignature = false }
        );

        if (!checkResult.IsSuccess || checkResult.Value == null) {
            await _auditHandler.HandleSave(request, actionType, entityName, checkResult.Message, checkResult.Value?.Email ?? string.Empty, checkResult.Value?.Id);
            return checkResult;
        }

        // request: return user profile -> da cambiare per ora ritorna solo il ruolo dell'utente
        UserModel user = new() {
            Role = checkResult.Value.Role
        };

        await _auditHandler.HandleSave(request, actionType, entityName, "Retrieve user information", user.Email, user.Id, true);
        return Result<UserModel>.Success(user, "Retrieve user information");
    }
}


