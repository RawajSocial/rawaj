using Rawaj.Domain.Common;
using Rawaj.Domain.Entities.Tenants;
using Rawaj.Domain.Enums;

namespace Rawaj.Domain.Entities.BrandIntelligence;

public class Competitor : BaseEntity
{
    public Guid BrandProfileId { get; set; }
    public string Name { get; set; } = null!;
    public string? Url { get; set; }
    public Dictionary<string, string> SocialHandles { get; set; } = [];
    public string? Notes { get; set; }
    public bool Ragged { get; set; }
    public CompetitorStatus Status { get; set; }
    public DateTime? LastScrapedAt { get; set; }

    public TenantBrandProfile BrandProfile { get; set; } = null!;
    public ICollection<RagDocument> RagDocuments { get; set; } = [];
}
