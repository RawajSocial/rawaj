namespace Rawaj.Application.Features.Admin.GetTenants;

public record AdminTenantSummary(
    Guid TenantId,
    string Name,
    string Subdomain,
    bool IsActive,
    Guid OwnerUserId,
    string PlanName,
    int MemberCount,
    DateTime CreatedAt);
