using Rawaj.Application.Features.Scheduling.Common;
using Xunit;

namespace Rawaj.Application.Tests.Features.Scheduling;

public class SchedulingWindowTests
{
    [Fact]
    public void ClampForward_DesiredTimeComfortablyInFuture_ReturnsDesiredTimeUnchanged()
    {
        var now = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var desired = now.AddDays(2);

        var result = SchedulingWindow.ClampForward(desired, now);

        Assert.Equal(desired, result);
    }

    [Fact]
    public void ClampForward_DesiredTimeInThePast_ClampsToDefaultLead()
    {
        var now = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var desired = now.AddDays(-3);

        var result = SchedulingWindow.ClampForward(desired, now);

        Assert.Equal(now.Add(SchedulingWindow.DefaultLead), result);
    }

    [Fact]
    public void ClampForward_NoDesiredTime_ReturnsDefaultLead()
    {
        var now = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        var result = SchedulingWindow.ClampForward(null, now);

        Assert.Equal(now.Add(SchedulingWindow.DefaultLead), result);
    }

    [Fact]
    public void ClampForward_WithinMinimumLead_StillClamps()
    {
        // 5 minutes out is under MinimumLead (10 min) even though it's technically "in the future".
        var now = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var desired = now.AddMinutes(5);

        var result = SchedulingWindow.ClampForward(desired, now);

        Assert.Equal(now.Add(SchedulingWindow.DefaultLead), result);
    }

    [Fact]
    public void ClampForward_MultipleOrdinalsWhenClamped_AreStaggeredApart()
    {
        var now = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        var first = SchedulingWindow.ClampForward(null, now, ordinal: 0);
        var second = SchedulingWindow.ClampForward(null, now, ordinal: 1);

        Assert.Equal(SchedulingWindow.BatchStagger, second - first);
    }
}
