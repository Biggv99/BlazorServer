using OptimusPrime.Features.Contracts.Commands.Shared;

namespace OptimusPrime.Features.Contracts.Commands;

public class AppVersionCommand : BaseTrackedCommand {

    public string AppVersion { get; set; } = string.Empty;
}

