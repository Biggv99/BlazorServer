using OptimusPrime.Core.Utils;
using OptimusPrime.Features.Contracts.Commands;
using OptimusPrime.Features.Contracts.Models;

namespace OptimusPrime.Features.Interfaces;

public interface IAuditRepository {

    Task<Result<bool>> SaveAudit(AuditModel audit);
    Task<Result<List<AuditModel>>> GetAudits(AuditCommand filters);
}
