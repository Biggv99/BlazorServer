using OptimusPrime.Core.Network;
using OptimusPrime.Core.Utils;
using OptimusPrime.Features.Contracts.Commands;
using OptimusPrime.Features.Contracts.Models;
using OptimusPrime.Features.Handlers;
using OptimusPrime.Presentation.Utils.Storage;

namespace OptimusPrime.Presentation.Services;

public class AuditService(
    AuditHandler auditHandler,
    SessionStorage sessionStorage) {

    private readonly AuditHandler _auditHandler = auditHandler;
    private readonly SessionStorage _sessionStorage = sessionStorage;

    // ==========================
    // Get audits
    // ==========================

    public async Task<Result<List<AuditModel>>> AuditLog(AuditCommand command) {

        JwtTokenModel? token = await _sessionStorage.GetToken();
        if (token == null) {
            return Result<List<AuditModel>>.Failure("Missing required data. Try logging again", ErrorType.Unauthorized);
        }

        command.AccessToken = token.AccessToken;

        return await _auditHandler.HandleGet(command);
    }
}