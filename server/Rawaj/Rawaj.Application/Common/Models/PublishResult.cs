namespace Rawaj.Application.Common.Models;

public class PublishResult
{
    public bool Succeeded { get; }
    public string? ExternalPostId { get; }
    public string? ErrorMessage { get; }

    private PublishResult(bool succeeded, string? externalPostId, string? errorMessage)
    {
        Succeeded = succeeded;
        ExternalPostId = externalPostId;
        ErrorMessage = errorMessage;
    }

    public static PublishResult Success(string externalPostId) => new(true, externalPostId, null);

    public static PublishResult Failure(string errorMessage) => new(false, null, errorMessage);
}
