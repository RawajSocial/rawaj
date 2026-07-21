using Rawaj.Domain.Enums;

namespace Rawaj.Application.Features.Tenants.CreateTenant;

public record CreateTenantResponse(
    Guid TenantId,
    string Name,
    string Subdomain,
    TenantType TenantType,
    Guid SubscriptionId);
