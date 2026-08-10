namespace Rawaj.Domain.Enums;

public enum ScheduledPostStatus
{
    Pending,
    Published,
    Failed,
    Cancelled,
    /// <summary>Was genuinely live on the platform, then deleted there on purpose by the user
    /// (distinct from Cancelled, which means it never went out in the first place).</summary>
    TakenDown
}
