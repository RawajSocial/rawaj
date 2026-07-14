using Rawaj.Domain.Common;
using Rawaj.Domain.Entities.Tenants;
using Rawaj.Domain.Enums;

namespace Rawaj.Domain.Entities.BrandIntelligence;

public class RagDocument : BaseEntity
{
    public Guid? CompetitorId { get; set; }
    public Guid? BrandProfileId { get; set; }
    public RagSourceType SourceType { get; set; }
    public string? SourceUrl { get; set; }
    public string? VectorId { get; set; }
    public string? CompetitorsData { get; set; }
    public DateTime? IndexedAt { get; set; }

    public Competitor? Competitor { get; set; }
    public TenantBrandProfile? BrandProfile { get; set; }
}
