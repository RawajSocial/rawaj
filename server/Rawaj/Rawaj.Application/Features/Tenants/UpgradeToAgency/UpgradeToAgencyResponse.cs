using Rawaj.Domain.Enums;

namespace Rawaj.Application.Features.Tenants.UpgradeToAgency;

public record UpgradeToAgencyResponse(
    Guid TenantId,
    TenantType TenantType,
    string PlanName,
    int MaxBrands);
