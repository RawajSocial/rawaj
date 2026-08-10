namespace Rawaj.Application.Common;

/// <summary>
/// Rawaj's tenants are all Cairo-based today, and "what hour should this post go out" only makes
/// sense relative to that business's own clock — not the app server's, not UTC. Anything that turns
/// a bare hour/date (from the AI or a user's date/time picker) into a stored instant must go through
/// here rather than assuming the ambient offset, which is how the AI-suggested and manually-picked
/// scheduled times ended up ~2 hours off in practice.
/// </summary>
public static class CairoTimeZone
{
    public static readonly TimeZoneInfo Instance = Resolve();

    private static TimeZoneInfo Resolve()
    {
        try
        {
            return TimeZoneInfo.FindSystemTimeZoneById("Africa/Cairo");
        }
        catch (TimeZoneNotFoundException)
        {
            // Windows-only ID, kept as a fallback for environments without IANA tzdata (ICU is on by
            // default on .NET 6+, so this branch should not normally be hit).
            return TimeZoneInfo.FindSystemTimeZoneById("Egypt Standard Time");
        }
    }

    /// <summary>Converts a Cairo wall-clock value (e.g. "6pm" with no timezone attached) to the true
    /// UTC instant it represents. <paramref name="cairoLocal"/>'s <c>Kind</c> is ignored/overwritten
    /// — the value is always treated as Cairo-local regardless of how it was constructed.</summary>
    public static DateTime ToUtc(DateTime cairoLocal) =>
        TimeZoneInfo.ConvertTimeToUtc(DateTime.SpecifyKind(cairoLocal, DateTimeKind.Unspecified), Instance);
}
