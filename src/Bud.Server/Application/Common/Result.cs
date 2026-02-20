namespace Bud.Server.Application.Common;

public sealed class Result
{
    public bool IsSuccess { get; }
    public string? Error { get; }
    public ErrorType ErrorType { get; }

    private Result(bool isSuccess, string? error = null, ErrorType errorType = ErrorType.None)
    {
        IsSuccess = isSuccess;
        Error = error;
        ErrorType = errorType;
    }

    public static Result Success() => new(true);

    public static Result Failure(string error, ErrorType errorType = ErrorType.Validation)
        => new(false, error, errorType);

    public static Result NotFound(string error)
        => new(false, error, ErrorType.NotFound);

    public static Result Forbidden(string error)
        => new(false, error, ErrorType.Forbidden);
}

#pragma warning disable CA1000 // static factory methods are intentional for Result pattern ergonomics
public sealed class Result<T>
{
    public bool IsSuccess { get; }
    public T? Value { get; }
    public string? Error { get; }
    public ErrorType ErrorType { get; }

    private Result(bool isSuccess, T? value = default, string? error = null, ErrorType errorType = ErrorType.None)
    {
        IsSuccess = isSuccess;
        Value = value;
        Error = error;
        ErrorType = errorType;
    }

    public static Result<T> Success(T value) => new(true, value);

    public static Result<T> Failure(string error, ErrorType errorType = ErrorType.Validation)
        => new(false, default, error, errorType);

    public static Result<T> NotFound(string error)
        => new(false, default, error, ErrorType.NotFound);

    public static Result<T> Forbidden(string error)
        => new(false, default, error, ErrorType.Forbidden);
}
#pragma warning restore CA1000

public enum ErrorType
{
    None,
    Validation,
    NotFound,
    Conflict,
    Forbidden
}
