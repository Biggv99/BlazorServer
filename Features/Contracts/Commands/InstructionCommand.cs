using OptimusPrime.Features.Contracts.Commands.Shared;

namespace OptimusPrime.Features.Contracts.Commands;

public class InstructionCommand : BaseTrackedCommand {
    public string Tipo { get; set; } = string.Empty;
    public int Valore { get; set; } = 0;
}