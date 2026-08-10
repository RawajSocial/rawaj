using Rawaj.Application.Features.Analytics.Common;
using Rawaj.Domain.Enums;
using Xunit;

namespace Rawaj.Application.Tests.Features.Analytics.Common;

/// <summary>
/// Unit tests for the campaign/brand/dashboard-level Engagement Rate formula fix (Phase 4): it must
/// be a weighted SUM(Engagements)/SUM(Views) across every post in the group, never an average of each
/// post's own EngagementRate - that unweighted-average shape was the exact P0 bug this replaces.
/// </summary>
public class PostAnalyticsAggregationTests
{
    private static PostAnalyticsAggregation.LatestPostSnapshot Post(
        long? views, int? likes, int? comments, int? shares, decimal? engagementRate) =>
        new(
            Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), null,
            SocialPlatform.Facebook, DateTime.UtcNow,
            views, null, likes, comments, shares, null, null, engagementRate,
            "Title", "Content", DateTime.UtcNow);

    [Fact]
    public void WeightedEngagementRate_SinglePost_EqualsEngagementsOverViews()
    {
        // Engagements = Likes + Comments + Shares = 10 + 3 + 2 = 15; Views = 200 -> 15/200 = 0.075
        var posts = new[] { Post(views: 200, likes: 10, comments: 3, shares: 2, engagementRate: 0.075m) };

        var result = PostAnalyticsAggregation.WeightedEngagementRate(posts);

        Assert.Equal(0.075m, result);
    }

    [Fact]
    public void WeightedEngagementRate_MultiplePosts_UsesWeightedSum_NotNaiveAverageOfPerPostRates()
    {
        // Post A: tiny reach, huge rate (10 engagements / 10 views = 1.0).
        // Post B: huge reach, modest rate (100 engagements / 10,000 views = 0.01).
        // Naive average of the two per-post rates: (1.0 + 0.01) / 2 = 0.505 - wildly wrong, dominated
        // by the low-volume post. Weighted: (10 + 100) / (10 + 10,000) = 110 / 10,010 ~= 0.0110.
        var posts = new[]
        {
            Post(views: 10, likes: 10, comments: 0, shares: 0, engagementRate: 1.0m),
            Post(views: 10_000, likes: 100, comments: 0, shares: 0, engagementRate: 0.01m)
        };

        var weighted = PostAnalyticsAggregation.WeightedEngagementRate(posts);
        var naiveAverage = posts.Average(p => p.EngagementRate!.Value);

        Assert.Equal(Math.Round(110m / 10_010m, 4), weighted);
        Assert.NotEqual(Math.Round(naiveAverage, 4), weighted);
    }

    [Fact]
    public void WeightedEngagementRate_WhenNoPostHasViews_ReturnsNullNotZero()
    {
        // Every post's insights call failed (Views unavailable) - the rate is "unavailable", not a
        // real 0%, so this must not silently present as a genuine zero.
        var posts = new[]
        {
            Post(views: null, likes: 5, comments: 1, shares: 0, engagementRate: null),
            Post(views: null, likes: 0, comments: 0, shares: 0, engagementRate: null)
        };

        var result = PostAnalyticsAggregation.WeightedEngagementRate(posts);

        Assert.Null(result);
    }

    [Fact]
    public void WeightedEngagementRate_WhenTotalViewsIsExplicitlyZero_ReturnsNullNotDivideByZero()
    {
        var posts = new[] { Post(views: 0, likes: 3, comments: 0, shares: 0, engagementRate: null) };

        var result = PostAnalyticsAggregation.WeightedEngagementRate(posts);

        Assert.Null(result);
    }

    [Fact]
    public void WeightedEngagementRate_TreatsNullEngagementFieldsAsZero_NotAsExcludedFromTheSum()
    {
        var posts = new[]
        {
            Post(views: 100, likes: null, comments: null, shares: null, engagementRate: null),
            Post(views: 100, likes: 20, comments: null, shares: null, engagementRate: 0.2m)
        };

        var result = PostAnalyticsAggregation.WeightedEngagementRate(posts);

        // Engagements = (0) + (20) = 20; Views = 100 + 100 = 200 -> 0.1
        Assert.Equal(0.1m, result);
    }

    [Fact]
    public void WeightedEngagementRate_EmptyPostList_ReturnsNull()
    {
        var result = PostAnalyticsAggregation.WeightedEngagementRate([]);

        Assert.Null(result);
    }
}
