using Microsoft.Extensions.Options;
using OptimusPrime.Core.Settings;
using OptimusPrime.Core.Utils;
using OptimusPrime.Features.Contracts.Commands;
using OptimusPrime.Features.Contracts.Models;
namespace OptimusPrime.Features.Handlers;

public class SystemHandler(
    IOptions<AppSettings> options,
    AuditHandler auditHandler ) {

    private readonly AuditHandler _auditHandler = auditHandler;
    private readonly AppSettings _settings = options.Value;

    private const string entityName = "SYSTEM";

    public async Task<Result<AppVersionModel>> HandleAppVersion(AppVersionCommand request) {

        const string actionType = "APP VERSION";

        // check: app version
        if (request.AppVersion == _settings.Version) {
            await _auditHandler.HandleSave(request, actionType, entityName, "Version is up to date", isSuccess: true);
            return Result<AppVersionModel>.Success("Version is up to date");
        }

        // request: latest version of the app
        AppVersionModel app = new() {
            AppVersion = _settings.Version,
            Description = _settings.Description
        };

        await _auditHandler.HandleSave(request, actionType, entityName, "Need update", isSuccess: true);
        return Result<AppVersionModel>.Success(app, "Version is not up to date");
    }
}


