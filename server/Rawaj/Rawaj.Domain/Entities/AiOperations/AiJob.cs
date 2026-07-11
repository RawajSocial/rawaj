using Rawaj.Domain.Common;
using Rawaj.Domain.Entities.Tenants;
using Rawaj.Domain.Enums;

namespace Rawaj.Domain.Entities.AiOperations;

public class AiJob : BaseEntity
{
    public Guid BrandProfileId { get; set; }
    public Guid TriggeredBy { get; set; }
    public AiJobType JobType { get; set; }
    public AiJobStatus Status { get; set; }
    public string? InputParams { get; set; }
    public Guid? OutputRefId { get; set; }
    public string? OutputRefType { get; set; }
    public int? Tokens { get; set; }
    public decimal? Cost { get; set; }
    public string? ErrorMessage { get; set; }
    public DateTime? StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public DateTime CreatedAt { get; set; }

    public TenantBrandProfile BrandProfile { get; set; } = null!;
}
