using FluentValidation;
using OptimusPrime.Core.Network;
using OptimusPrime.Core.Utils;

namespace OptimusPrime.Features.Endpoints.Shared;

public class ValidationFilter<TCommand, TValidator> : IEndpointFilter
    where TCommand : class
    where TValidator : IValidator<TCommand> 
{
    public async ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext context, EndpointFilterDelegate next) {

        var validator = context.HttpContext.RequestServices.GetService<TValidator>();
        if(validator != null) {
            var command = context.Arguments.OfType<TCommand>().FirstOrDefault();
            if(command != null) {
                var validationResult = await validator.ValidateAsync(command);
                if(!validationResult.IsValid) {
                    string errorMessages = string.Join("\n", validationResult.Errors.Select(e => e.ErrorMessage));
                    return Result<object>.Failure(errorMessages, ErrorType.ValidationError).ToHttpResult();
                }
            }
        }
        return await next(context);
    }
}