namespace Rawaj.Application.Common.Models;

public class FollowerCountResult
{
    public bool Succeeded { get; }
    public int? FollowerCount { get; }
    public string? ErrorMessage { get; }

    private FollowerCountResult(bool succeeded, int? followerCount, string? errorMessage)
    {
        Succeeded = succeeded;
        FollowerCount = followerCount;
        ErrorMessage = errorMessage;
    }

    public static FollowerCountResult Success(int followerCount) => new(true, followerCount, null);

    public static FollowerCountResult Failure(string errorMessage) => new(false, null, errorMessage);
}
