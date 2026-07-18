using OptimusPrime.Core.Network;

namespace OptimusPrime.Core.Utils;

public class Result<T> {
    public required bool IsSuccess { get; set; }
    public required string Message { get; set; }
    public ErrorType Error { get; set; } = ErrorType.None;
    public T? Value { get; set; }

    // ==============================================================================
    // Encapsulates operation's result: success status, messages, and data
    // ==============================================================================


    // success WITHOUT passing values 
    public static Result<T> Success(string msg) => new() { IsSuccess = true, Message = msg }; 

    // success WITH value 
    public static Result<T> Success(T value, string msg) => new() { IsSuccess = true, Value = value, Message = msg };

    // success propagating the message of another Result
    public static Result<T> Success<U>(Result<U> successResult) => new() { IsSuccess = true, Message = successResult.Message, Error = successResult.Error };


    // failure WITHOUT passing values 
    public static Result<T> Failure(string msg, ErrorType error) => new() { IsSuccess = false, Message = msg, Error = error };

    // failure WITH value
    public static Result<T> Failure(T value, string msg, ErrorType error) => new() { IsSuccess = false, Value = value, Message = msg, Error = error };

    // failure propagating the message of another Result
    public static Result<T> Failure<U>(Result<U> failedResult) => new() { IsSuccess = false, Message = failedResult.Message, Error = failedResult.Error };
}