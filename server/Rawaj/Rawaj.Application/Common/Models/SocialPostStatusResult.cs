namespace Rawaj.Application.Common.Models;

public class SocialPostStatusResult
{
    public bool Succeeded { get; }
    public bool IsPublished { get; }
    public string? ErrorMessage { get; }

    private SocialPostStatusResult(bool succeeded, bool isPublished, string? errorMessage)
    {
        Succeeded = succeeded;
        IsPublished = isPublished;
        ErrorMessage = errorMessage;
    }

    public static SocialPostStatusResult Success(bool isPublished) => new(true, isPublished, null);

    public static SocialPostStatusResult Failure(string errorMessage) => new(false, false, errorMessage);
}
