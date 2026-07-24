namespace Rawaj.Application.Features.Scheduling.Common;

/// <summary>
/// Single source of truth for how far ahead a post may be scheduled. Native platform scheduling
/// (Meta) rejects times under ~10 minutes out, so SchedulePostCommandValidator rejects anything
/// closer for user-picked times, and the bulk campaign path clamps AI-suggested times forward
/// to DefaultLead rather than failing a whole batch on a stale campaign start date.
/// </summary>
public static class SchedulingWindow
{
    public static readonly TimeSpan MinimumLead = TimeSpan.FromMinutes(10);
    public static readonly TimeSpan DefaultLead = TimeSpan.FromMinutes(20);
    public static readonly TimeSpan BatchStagger = TimeSpan.FromMinutes(10);

    /// <summary>
    /// Returns <paramref name="desired"/> when it is comfortably in the future, otherwise the
    /// earliest acceptable slot, offset by <paramref name="ordinal"/> so a whole batch of
    /// past-dated posts doesn't collapse onto the same minute.
    /// </summary>
    public static DateTime ClampForward(DateTime? desired, DateTime now, int ordinal = 0)
    {
        var earliest = now.Add(DefaultLead).Add(TimeSpan.FromTicks(BatchStagger.Ticks * ordinal));
        return desired is { } d && d > now.Add(MinimumLead) ? d : earliest;
    }
}
