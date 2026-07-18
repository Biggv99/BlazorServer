using OptimusPrime.Features.Contracts.Commands.Shared;

namespace OptimusPrime.Features.Contracts.Commands; 
public class AuditCommand : BaseTrackedCommand {
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 100; 

    public string Timestamp { get; set; } = string.Empty;
    public string UserEmail { get; set; } = string.Empty;
    public string ActionType { get; set; } = string.Empty;
    public string EntityName { get; set; } = string.Empty;
    public string Result { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
}

