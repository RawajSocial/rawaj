using Rawaj.Domain.Enums;

namespace Rawaj.Application.Features.Tenants.GetMyTenant;

public record GetMyTenantResponse(
    Guid TenantId,
    string Name,
    string Subdomain,
    TenantType TenantType,
    TenantMemberRole Role,
    bool IsActive,
    int CoinBalance,
    bool IsActivated,
    string PlanName,
    int MaxBrands,
    int BrandProfileCount,
    Guid? DefaultBrandProfileId,
    string? Phone,
    string? Industry,
    string? Country,
    string? City,
    string? Website,
    string? AgencySize,
    List<string> ServicesOffered);
