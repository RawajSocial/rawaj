using Rawaj.Domain.Common;
using Rawaj.Domain.Enums;

namespace Rawaj.Domain.Entities.Tenants;

/// <summary>
/// An invite sent to an email address with no existing Rawaj account yet. Once they register
/// through the emailed link, this row is consumed and turned into a normal, Accepted
/// <see cref="TenantMember"/> — kept as a separate entity (rather than a nullable
/// <see cref="TenantMember.UserId"/>) so the existing team-member queries/indexes never have to
/// deal with a memberless row.
/// </summary>
public class TenantInvitation : BaseEntity
{
    public Guid TenantId { get; set; }
    public string Email { get; set; } = null!;
    public TenantMemberRole Role { get; set; }
    public List<Guid> BrandProfileIds { get; set; } = [];
    public int AllocatedCoins { get; set; }
    public Guid InvitedByUserId { get; set; }
    public string TokenHash { get; set; } = null!;
    public DateTime ExpiresAt { get; set; }
    public InvitationStatus Status { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? AcceptedAt { get; set; }

    public Tenant Tenant { get; set; } = null!;
}
