using Rawaj.Domain.Common;

namespace Rawaj.Domain.Entities.Campaigns;

public class ContentRevision : BaseEntity
{
    public Guid ContentItemId { get; set; }
    public Guid RevisedBy { get; set; }
    public string RevisionPrompt { get; set; } = null!;
    public string Previous { get; set; } = null!;
    public string Current { get; set; } = null!;
    public int RevisionNumber { get; set; }
    public DateTime CreatedAt { get; set; }

    public ContentItem ContentItem { get; set; } = null!;
}
