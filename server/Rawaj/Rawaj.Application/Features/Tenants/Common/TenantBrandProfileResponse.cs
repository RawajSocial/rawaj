using Rawaj.Domain.Entities.Tenants;
using Rawaj.Domain.Enums;

namespace Rawaj.Application.Features.Tenants.Common;

public record TenantBrandProfileResponse(
    Guid Id,
    Guid TenantId,
    string Name,
    string? Description,
    BrandVoice? BrandVoice,
    BrandProfileStatus Status,
    string? Tagline,
    string? Industry,
    string? TargetAudience,
    List<string> Colors,
    string? LogoUrl,
    string? WebsiteUrl,
    List<string> SupportedLanguages,
    List<string> Keywords,
    bool IsDefault,
    DateTime CreatedAt,
    DateTime UpdatedAt)
{
    public static TenantBrandProfileResponse FromEntity(TenantBrandProfile p) => new(
        p.Id,
        p.TenantId,
        p.Name,
        p.Description,
        p.BrandVoice,
        p.Status,
        p.BrandInfo?.Tagline,
        p.BrandInfo?.Industry,
        p.BrandInfo?.TargetAudience,
        p.BrandInfo?.Colors ?? [],
        p.BrandInfo?.LogoUrl,
        p.BrandInfo?.WebsiteUrl,
        p.BrandInfo?.SupportedLanguages ?? [],
        p.BrandInfo?.Keywords ?? [],
        p.BrandInfo?.IsDefault ?? false,
        p.CreatedAt,
        p.UpdatedAt);
}
