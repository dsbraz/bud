namespace Bud.Server.Application.Common.Results;

public sealed class ServiceResult
{
    public bool IsSuccess { get; }
    public string? Error { get; }
    public ServiceErrorType ErrorType { get; }

    private ServiceResult(bool isSuccess, string? error = null, ServiceErrorType errorType = ServiceErrorType.None)
    {
        IsSuccess = isSuccess;
        Error = error;
        ErrorType = errorType;
    }

    public static ServiceResult Success() => new(true);

    public static ServiceResult Failure(string error, ServiceErrorType errorType = ServiceErrorType.Validation)
        => new(false, error, errorType);

    public static ServiceResult NotFound(string error)
        => new(false, error, ServiceErrorType.NotFound);

    public static ServiceResult Forbidden(string error)
        => new(false, error, ServiceErrorType.Forbidden);
}

#pragma warning disable CA1000 // static factory methods are intentional for Result pattern ergonomics
public sealed class ServiceResult<T>
{
    public bool IsSuccess { get; }
    public T? Value { get; }
    public string? Error { get; }
    public ServiceErrorType ErrorType { get; }

    private ServiceResult(bool isSuccess, T? value = default, string? error = null, ServiceErrorType errorType = ServiceErrorType.None)
    {
        IsSuccess = isSuccess;
        Value = value;
        Error = error;
        ErrorType = errorType;
    }

    public static ServiceResult<T> Success(T value) => new(true, value);

    public static ServiceResult<T> Failure(string error, ServiceErrorType errorType = ServiceErrorType.Validation)
        => new(false, default, error, errorType);

    public static ServiceResult<T> NotFound(string error)
        => new(false, default, error, ServiceErrorType.NotFound);

    public static ServiceResult<T> Forbidden(string error)
        => new(false, default, error, ServiceErrorType.Forbidden);
}
#pragma warning restore CA1000

public enum ServiceErrorType
{
    None,
    Validation,
    NotFound,
    Conflict,
    Forbidden
}
