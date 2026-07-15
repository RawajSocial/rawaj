namespace Rawaj.Application.Common.Models;

public class PostMetricsResult
{
    public bool Succeeded { get; }
    public long? Impressions { get; }
    public long? Reach { get; }
    public int? Likes { get; }
    public int? Comments { get; }
    public int? Shares { get; }
    public string? ErrorMessage { get; }

    private PostMetricsResult(bool succeeded, long? impressions, long? reach, int? likes, int? comments, int? shares, string? errorMessage)
    {
        Succeeded = succeeded;
        Impressions = impressions;
        Reach = reach;
        Likes = likes;
        Comments = comments;
        Shares = shares;
        ErrorMessage = errorMessage;
    }

    public static PostMetricsResult Success(long? impressions, long? reach, int? likes, int? comments, int? shares) =>
        new(true, impressions, reach, likes, comments, shares, null);

    public static PostMetricsResult Failure(string errorMessage) => new(false, null, null, null, null, null, errorMessage);
}
