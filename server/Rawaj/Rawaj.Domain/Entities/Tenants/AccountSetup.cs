using Rawaj.Domain.Common;

namespace Rawaj.Domain.Entities.Tenants;

/// <summary>
/// Tracks a USER's (not tenant's) lightweight personal setup — distinct from a tenant's full
/// business-profile activation (<see cref="Tenant.IsActivated"/>/<see cref="Tenant.TenantProfile"/>),
/// which only the tenant owner provides. One row per user. Step-based rather than a flat
/// "IsCompleted" boolean so a second required step (e.g. phone verification) can be added later
/// without another schema change — today registration (self-signup or invite-accept-register) is
/// the only step, so CompletedSteps/CurrentStep are trivial, but the shape is ready to grow.
/// </summary>
public class AccountSetup : BaseEntity
{
    public Guid UserId { get; set; }
    public int CurrentStep { get; set; }
    /// <summary>Comma-delimited step identifiers completed so far (e.g. "registration").</summary>
    public string CompletedSteps { get; set; } = string.Empty;
    public DateTime? CompletedAt { get; set; }
    public DateTime CreatedAt { get; set; }

    public bool IsCompleted => CompletedAt.HasValue;
}
