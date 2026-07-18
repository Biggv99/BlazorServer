using OptimusPrime.Core.Utils;
using OptimusPrime.Features.Contracts.Models;

namespace OptimusPrime.Features.Interfaces;

public interface IFirebaseRepository {
    Task<Result<bool>> AddInstruction(InstructionModel instuction);
    Task<Result<bool>> UpdateHealthStatus(List<HealthModel> healthModels);
}