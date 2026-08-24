using Rawaj.Domain.Common;
using Rawaj.Domain.Enums;

namespace Rawaj.Domain.Entities.SocialMedia;

public class PostAnalytics : BaseEntity
{
    public Guid ScheduledPostId { get; set; }
    public SocialPlatform Platform { get; set; }
    public DateTime RecordedAt { get; set; }
    public long? Views { get; set; }
    public long? UniqueViewers { get; set; }
    public int? Likes { get; set; }
    public int? Comments { get; set; }
    public int? Shares { get; set; }
    public int? Saves { get; set; }
    public int? Clicks { get; set; }
    public decimal? EngagementRate { get; set; }
    public string? RawData { get; set; }

    /// <summary>Diagnostic only - set when part of the sync (e.g. the engagement or insights call)
    /// failed while the other half still produced a row. A null field above is not necessarily a
    /// real zero; check this before assuming so.</summary>
    public string? SyncError { get; set; }

    public ScheduledPost ScheduledPost { get; set; } = null!;
}
