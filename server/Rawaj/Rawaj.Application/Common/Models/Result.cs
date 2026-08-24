namespace Rawaj.Application.Common.Models;

public class Result<T>
{
    public bool Succeeded { get; }
    public T? Data { get; }
    public string? ErrorMessage { get; }
    public IReadOnlyDictionary<string, string[]>? ValidationErrors { get; }

    private Result(bool succeeded, T? data, string? errorMessage, IReadOnlyDictionary<string, string[]>? validationErrors)
    {
        Succeeded = succeeded;
        Data = data;
        ErrorMessage = errorMessage;
        ValidationErrors = validationErrors;
    }

    public static Result<T> Success(T data) => new(true, data, null, null);

    public static Result<T> Failure(string errorMessage) => new(false, default, errorMessage, null);

    public static Result<T> ValidationFailure(IReadOnlyDictionary<string, string[]> errors) =>
        new(false, default, "Validation failed.", errors);
}
