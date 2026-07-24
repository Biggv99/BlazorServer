using Microsoft.Extensions.Caching.Memory;
using OptimusPrime.Core.Utils;
using OptimusPrime.Features.Contracts.Commands;
using OptimusPrime.Features.Contracts.Commands.Shared;
using OptimusPrime.Features.Contracts.Models;
using OptimusPrime.Features.Handlers.Shared;
using OptimusPrime.Features.Interfaces;

namespace OptimusPrime.Features.Handlers;

public class AuditHandler(
    IAuditRepository repositoryAudit,
    IMemoryCache memoryCache,
    CheckHandler checkHandler ) {

    private readonly IAuditRepository _repositoryAudit = repositoryAudit;
    private readonly IMemoryCache _memoryCache = memoryCache;
    private readonly CheckHandler _checkHandler = checkHandler;

    private const string entityName = "AUDIT";

    // ==========================
    // Get 
    // ==========================
    public async Task<Result<List<AuditModel>>> HandleGet(AuditCommand request) {

        const string actionType = "GET";

        // checks
        Result<UserModel> checkResult = await _checkHandler.Check(
            request,
            new CheckOptions { RequireDeviceId = false, RequireSignature = false }
        );

        if (!checkResult.IsSuccess || checkResult.Value == null) {
            await HandleSave(request, actionType, entityName, checkResult.Message, checkResult.Value?.Email ?? string.Empty, checkResult.Value?.Id);
            return Result<List<AuditModel>>.Failure(checkResult);
        }

        // request: get audits
        Result<List<AuditModel>> audits = await _repositoryAudit.GetAudits(request);
        if(!audits.IsSuccess || audits.Value == null) {
            await HandleSave(request, actionType, entityName, audits.Message, checkResult.Value.Email, checkResult.Value.Id);
            return audits;
        }

        // check if the cache contains the key
        string cacheKey = $"AuditGet_internal";
        if(!_memoryCache.TryGetValue(cacheKey, out _)) {
            await HandleSave(request, actionType, entityName, "Audit logs retrieved", checkResult.Value.Email, checkResult.Value.Id, true);
            _memoryCache.Set(cacheKey, true, TimeSpan.FromMinutes(5));
        }

        return audits;
    }

    // ==========================
    // Save
    // ==========================
    public async Task HandleSave(
        BaseTrackedCommand request,
        string actionType,
        string entityName,
        string message,
        string email = "",
        Guid? id = null,
        bool isSuccess = false) {

        AuditModel audit = new() {
            IpAddress = request.IpAddress,
            UserAgent = request.UserAgent,
            ActionType = actionType,
            EntityName = entityName, 
            Message = message,
            UserEmail = email,
            UserId = id,
            EntityId = request.DeviceId,
            IsSuccess = isSuccess,
        };

        await _repositoryAudit.SaveAudit(audit);
    }
}
