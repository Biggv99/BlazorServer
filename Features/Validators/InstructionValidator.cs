using FluentValidation;
using OptimusPrime.Features.Contracts.Commands;

namespace OptimusPrime.Features.Validators;

public class InstructionValidator : AbstractValidator<InstructionCommand> {
    public InstructionValidator() {

        RuleFor(x => x.Tipo)
            .NotEmpty().WithMessage("Instruction type is required")
            .Must(t => InstructionTypes.IsValid(t)).WithMessage("Unsupported instruction type");
    }
}

public static class InstructionTypes {
    public const string Aggiungi = "aggiungi";
    public const string Azzera = "azzera";

    public static readonly HashSet<string> ValidTypes = [
        Aggiungi,
        Azzera,
    ];

    public static bool IsValid(string tipo) => ValidTypes.Contains(tipo?.ToLowerInvariant() ?? "");
}
