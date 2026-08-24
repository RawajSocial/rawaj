using System.Text.Json.Serialization;

namespace Rawaj.Common;

public class ApiResponse<T>
{
    [JsonPropertyName("status")]
    public string Status { get; }

    [JsonPropertyName("data")]
    public T? Data { get; }

    [JsonPropertyName("message")]
    public string? Message { get; }

    [JsonPropertyName("errors")]
    public IReadOnlyDictionary<string, string[]>? Errors { get; }

    private ApiResponse(string status, T? data, string? message, IReadOnlyDictionary<string, string[]>? errors)
    {
        Status = status;
        Data = data;
        Message = message;
        Errors = errors;
    }

    public static ApiResponse<T> Success(T data) => new("success", data, null, null);

    public static ApiResponse<T> Fail(string message, IReadOnlyDictionary<string, string[]>? errors = null) =>
        new("fail", default, message, errors);

    public static ApiResponse<T> Error(string message) => new("error", default, message, null);
}
