using OptimusPrime.Core.Utils;

namespace OptimusPrime.Core.Network;

// ==========================
// Mapping HTTP status code
// ==========================
public enum ErrorType {
    None,

    // client errors
    ValidationError,
    InvalidOperation,
    Unauthorized,
    Forbidden,
    NotFound,
    Conflict,
    TooManyRequests,

    // server errors
    DatabaseError,
    ExternalServiceError,
    InternalError
}

public static class HttpResult { 

    public static IResult ToHttpResult<T>(this Result<T> result) {

        // success
        if(result.IsSuccess) {
            return Results.Ok(result);
        }

        // failure
        return result.Error switch {
            ErrorType.Unauthorized => Results.Json(result, statusCode: StatusCodes.Status401Unauthorized),
            ErrorType.Forbidden => Results.Json(result, statusCode: StatusCodes.Status403Forbidden),
            ErrorType.TooManyRequests => Results.Json(result, statusCode: StatusCodes.Status429TooManyRequests),

            ErrorType.NotFound => Results.NotFound(result),
            ErrorType.Conflict => Results.Conflict(result),
            ErrorType.ValidationError => Results.BadRequest(result),
            ErrorType.InvalidOperation => Results.BadRequest(result),

            ErrorType.DatabaseError => Results.Json(result, statusCode: StatusCodes.Status500InternalServerError),
            ErrorType.ExternalServiceError => Results.Json(result, statusCode: StatusCodes.Status502BadGateway),
            ErrorType.InternalError => Results.Json(result, statusCode: StatusCodes.Status500InternalServerError),

            _ => Results.BadRequest(result)
        };
    }
}