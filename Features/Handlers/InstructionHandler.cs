using OptimusPrime.Core.Utils;
using OptimusPrime.Features.Contracts.Commands;
using OptimusPrime.Features.Contracts.Models;
using OptimusPrime.Features.Handlers.Shared;
using OptimusPrime.Features.Interfaces;

namespace OptimusPrime.Features.Handlers;

public class InstructionHandler(
    IFirebaseRepository repositoryInstruction,
    AuditHandler auditHandler,
    CheckHandler checkHandler ) {

    private readonly IFirebaseRepository _repositoryInstruction = repositoryInstruction;
    private readonly AuditHandler _auditHandler = auditHandler;
    private readonly CheckHandler _checkHandler = checkHandler;

    private const string entityName = "INSTRUCTION";

    // ==========================
    // Add
    // ==========================
    public async Task<Result<bool>> HandleAdd(InstructionCommand request) {

        const string actionType = "ADD";

        // checks
        Result<UserModel> checkResult = await _checkHandler.Check(
            request,
            new CheckOptions { }
        );

        if(!checkResult.IsSuccess || checkResult.Value == null) {
            await _auditHandler.HandleSave(request, actionType, entityName, checkResult.Message, checkResult.Value?.Email ?? string.Empty, checkResult.Value?.Id);
            return Result<bool>.Failure(checkResult);
        }

        // request: save instruction
        UserModel user = checkResult.Value;

        var instruction = new InstructionModel {
            Eseguito = false,
            Tipo = request.Tipo,
            Valore = request.Valore,
        };

        Result<bool> saved = await _repositoryInstruction.AddInstruction(instruction);
        if(!saved.IsSuccess) {
            await _auditHandler.HandleSave(request, actionType, entityName, saved.Message, user.Email, user.Id);
            return saved;
        }

        await _auditHandler.HandleSave(request, actionType, entityName, saved.Message, user.Email, user.Id, true);
        return saved;
    }
}