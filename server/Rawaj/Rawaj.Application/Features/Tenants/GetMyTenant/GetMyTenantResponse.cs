using Rawaj.Domain.Enums;

namespace Rawaj.Application.Features.Tenants.GetMyTenant;

public record GetMyTenantResponse(
    Guid TenantId,
    string Name,
    string Subdomain,
    TenantType TenantType,
    TenantMemberRole Role,
    bool IsActive);
