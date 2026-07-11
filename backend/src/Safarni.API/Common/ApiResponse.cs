namespace Safarni.API.Common;

public sealed class ApiResponse<T>
{
    public bool Success { get; init; }

    public string Message { get; init; } = string.Empty;

    public T? Data { get; init; }

    public IReadOnlyCollection<ApiError> Errors { get; init; } = Array.Empty<ApiError>();

    public int StatusCode { get; init; }

    public DateTime Timestamp { get; init; } = DateTime.UtcNow;

    public static ApiResponse<T> SuccessResponse(T data, string message, int statusCode)
    {
        return new ApiResponse<T>
        {
            Success = true,
            Data = data,
            Message = message,
            StatusCode = statusCode
        };
    }

    public static ApiResponse<T> FailureResponse(IEnumerable<ApiError> errors, string message, int statusCode)
    {
        return new ApiResponse<T>
        {
            Success = false,
            Message = message,
            StatusCode = statusCode,
            Errors = errors.ToArray()
        };
    }
}