namespace Rawaj.Application.Common.Models;

public class PostMetricsResult
{
    public bool Succeeded { get; }
    public bool InsightsAvailable { get; }
    public long? Views { get; }
    public long? UniqueViewers { get; }
    public int? Likes { get; }
    public int? Comments { get; }
    public int? Shares { get; }
    public string? ErrorMessage { get; }

    private PostMetricsResult(
        bool succeeded,
        bool insightsAvailable,
        long? views,
        long? uniqueViewers,
        int? likes,
        int? comments,
        int? shares,
        string? errorMessage)
    {
        Succeeded = succeeded;
        InsightsAvailable = insightsAvailable;
        Views = views;
        UniqueViewers = uniqueViewers;
        Likes = likes;
        Comments = comments;
        Shares = shares;
        ErrorMessage = errorMessage;
    }

    /// <summary>
    /// A partial result is still a Success: a field the provider couldn't retrieve (e.g. the insights
    /// call failing while likes/comments/shares succeeded) is simply null, distinguishable from a
    /// total failure via <see cref="InsightsAvailable"/> and <paramref name="errorMessage"/>.
    /// </summary>
    public static PostMetricsResult Success(
        long? views,
        long? uniqueViewers,
        int? likes,
        int? comments,
        int? shares,
        bool insightsAvailable,
        string? errorMessage = null) =>
        new(true, insightsAvailable, views, uniqueViewers, likes, comments, shares, errorMessage);

    /// <summary>Total failure — no field could be retrieved at all.</summary>
    public static PostMetricsResult Failure(string errorMessage) =>
        new(false, false, null, null, null, null, null, errorMessage);
}
