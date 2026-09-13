namespace TalentFlow.Application.Common;

/// <summary>
/// Generic result wrapper for service operations.
/// Provides a consistent pattern for returning success/failure from service methods.
/// </summary>
public class Result<T>
{
    public bool IsSuccess { get; }
    public T? Data { get; }
    public string? Error { get; }
    public string? ErrorCode { get; }
    public int StatusCode { get; }

    private Result(bool isSuccess, T? data, string? error, string? errorCode, int statusCode)
    {
        IsSuccess = isSuccess;
        Data = data;
        Error = error;
        ErrorCode = errorCode;
        StatusCode = statusCode;
    }

    public static Result<T> Success(T data, int statusCode = 200)
        => new(true, data, null, null, statusCode);

    public static Result<T> Created(T data)
        => new(true, data, null, null, 201);

    public static Result<T> Failure(string error, string? errorCode = null, int statusCode = 400)
        => new(false, default, error, errorCode, statusCode);

    public static Result<T> NotFound(string error)
        => new(false, default, error, "NOT_FOUND", 404);

    public static Result<T> Forbidden(string error = "You do not have permission to perform this action.")
        => new(false, default, error, "FORBIDDEN", 403);

    public static Result<T> Conflict(string error, string? errorCode = null)
        => new(false, default, error, errorCode ?? "CONFLICT", 409);
}

/// <summary>
/// Non-generic Result for operations that don't return data.
/// </summary>
public class Result
{
    public bool IsSuccess { get; }
    public string? Error { get; }
    public string? ErrorCode { get; }
    public int StatusCode { get; }

    private Result(bool isSuccess, string? error, string? errorCode, int statusCode)
    {
        IsSuccess = isSuccess;
        Error = error;
        ErrorCode = errorCode;
        StatusCode = statusCode;
    }

    public static Result Success(int statusCode = 200)
        => new(true, null, null, statusCode);

    public static Result Failure(string error, string? errorCode = null, int statusCode = 400)
        => new(false, error, errorCode, statusCode);

    public static Result NotFound(string error)
        => new(false, error, "NOT_FOUND", 404);

    public static Result Forbidden(string error = "You do not have permission to perform this action.")
        => new(false, error, "FORBIDDEN", 403);

    public static Result Conflict(string error, string? errorCode = null)
        => new(false, error, errorCode ?? "CONFLICT", 409);
}
