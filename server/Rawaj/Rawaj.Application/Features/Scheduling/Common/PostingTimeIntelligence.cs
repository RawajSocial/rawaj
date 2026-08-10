using Microsoft.EntityFrameworkCore;
using Rawaj.Application.Common;
using Rawaj.Application.Common.Interfaces;
using Rawaj.Domain.Enums;

namespace Rawaj.Application.Features.Scheduling.Common;

/// <summary>DayOfWeek/Hour are both Cairo-local — matches the convention every other posting-time
/// value in the app follows (see ContentPlanPrompt, CairoTimeZone).</summary>
public record PostingTimeSuggestion(SocialPlatform Platform, DayOfWeek DayOfWeek, int Hour, bool FromHistoricalData);

public static class PostingTimeIntelligence
{
    private const int MinPublishedPostsForHistoricalData = 10;

    // General industry-standard "safe default" posting windows per platform, in the audience's own
    // (Cairo) local time — used until a brand has enough of its own published-post history to trust
    // real engagement data instead.
    private static readonly Dictionary<SocialPlatform, (DayOfWeek Day, int Hour)> PlatformDefaults = new()
    {
        [SocialPlatform.Instagram] = (DayOfWeek.Wednesday, 11),
        [SocialPlatform.Facebook] = (DayOfWeek.Wednesday, 13),
    };

    public static async Task<List<PostingTimeSuggestion>> GetSuggestionsAsync(
        IApplicationDbContext dbContext,
        Guid brandProfileId,
        IReadOnlyCollection<SocialPlatform> platforms,
        CancellationToken cancellationToken)
    {
        var history = await dbContext.ScheduledPosts
            .Where(s => s.SocialAccount.BrandProfileId == brandProfileId
                && s.Status == Domain.Enums.ScheduledPostStatus.Published
                && s.PublishedAt != null)
            .Select(s => new { s.SocialAccount.Platform, PublishedAt = s.PublishedAt!.Value, s.Analytics })
            .ToListAsync(cancellationToken);

        var suggestions = new List<PostingTimeSuggestion>();

        foreach (var platform in platforms)
        {
            var platformHistory = history.Where(h => h.Platform == platform).ToList();

            if (platformHistory.Count >= MinPublishedPostsForHistoricalData)
            {
                // PublishedAt is a true UTC instant — group by the Cairo-local day/hour it actually
                // published at, not UTC's, or "best performing hour" would be silently off by Cairo's
                // offset from UTC (and potentially the wrong weekday too, near midnight).
                var best = platformHistory
                    .GroupBy(h =>
                    {
                        var cairo = TimeZoneInfo.ConvertTimeFromUtc(h.PublishedAt, CairoTimeZone.Instance);
                        return (cairo.DayOfWeek, cairo.Hour);
                    })
                    .Select(g => new
                    {
                        g.Key.DayOfWeek,
                        g.Key.Hour,
                        Score = g.Sum(x => x.Analytics.Sum(a => (double?)a.EngagementRate) ?? 0) + g.Count(),
                    })
                    .OrderByDescending(g => g.Score)
                    .First();

                suggestions.Add(new PostingTimeSuggestion(platform, best.DayOfWeek, best.Hour, true));
            }
            else if (PlatformDefaults.TryGetValue(platform, out var fallback))
            {
                suggestions.Add(new PostingTimeSuggestion(platform, fallback.Day, fallback.Hour, false));
            }
        }

        return suggestions;
    }

    public static string BuildSummary(List<PostingTimeSuggestion> suggestions)
    {
        if (suggestions.Count == 0)
        {
            return string.Empty;
        }

        var lines = suggestions.Select(s =>
            $"{s.Platform}: best posting window is {s.DayOfWeek} around {s.Hour:00}:00" +
            (s.FromHistoricalData ? " (based on this brand's own historical engagement)." : " (general platform best practice)."));

        return string.Join(" ", lines);
    }
}
