namespace Shared.Common.DTOs;

public class ApiResponse<T>
{
    public bool IsSuccess { get; set; }
    public string Message { get; set; } = string.Empty;
    public T? Data { get; set; }

    public static ApiResponse<T> Success(T data, string message = "Operation successful")
        => new() { IsSuccess = true, Message = message, Data = data };

    public static ApiResponse<T> Fail(string message)
        => new() { IsSuccess = false, Message = message };
}
