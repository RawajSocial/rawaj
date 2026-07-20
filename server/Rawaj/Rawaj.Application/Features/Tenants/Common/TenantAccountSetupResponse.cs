using Rawaj.Domain.Entities.Tenants;
using Rawaj.Domain.ValueObjects;

namespace Rawaj.Application.Features.Tenants.Common;

public record TenantAccountSetupResponse(
    Guid Id,
    Guid BrandProfileId,
    string AccountType,
    string Country,
    string? City,
    string? Website,
    string? Facebook,
    string? Instagram,
    string? Youtube,
    string? TikTok,
    string? LinkedIn,
    string? X,
    string? Snapchat,
    string? AgencyName,
    string? AgencyPhone,
    string? AgencyCountryCode,
    string? AgencySize,
    string? ActiveClients,
    List<string> PrimaryServices,
    string? BusinessName,
    string? BusinessPhone,
    string? BusinessCountryCode,
    string? Industry,
    string? BusinessSize,
    string? HearAboutUs,
    DateTime CreatedAt,
    DateTime UpdatedAt)
{
    // Website and Industry are not stored on TenantAccountSetup — they live on the same brand's
    // TenantBrandProfile.BrandInfo (single source of truth, see AccountSetupService.SaveAsync).
    public static TenantAccountSetupResponse FromEntity(TenantAccountSetup a, BrandInfo? brandInfo) => new(
        a.Id,
        a.BrandProfileId,
        a.AccountType,
        a.Country,
        a.City,
        brandInfo?.WebsiteUrl,
        a.SocialLinks?.Facebook,
        a.SocialLinks?.Instagram,
        a.SocialLinks?.Youtube,
        a.SocialLinks?.TikTok,
        a.SocialLinks?.LinkedIn,
        a.SocialLinks?.X,
        a.SocialLinks?.Snapchat,
        a.AgencyDetails?.AgencyName,
        a.AgencyDetails?.AgencyPhone,
        a.AgencyDetails?.AgencyCountryCode,
        a.AgencyDetails?.AgencySize,
        a.AgencyDetails?.ActiveClients,
        a.AgencyDetails?.PrimaryServices ?? [],
        a.BusinessDetails?.BusinessName,
        a.BusinessDetails?.BusinessPhone,
        a.BusinessDetails?.BusinessCountryCode,
        brandInfo?.Industry,
        a.BusinessDetails?.BusinessSize,
        a.BusinessDetails?.HearAboutUs,
        a.CreatedAt,
        a.UpdatedAt);
}
