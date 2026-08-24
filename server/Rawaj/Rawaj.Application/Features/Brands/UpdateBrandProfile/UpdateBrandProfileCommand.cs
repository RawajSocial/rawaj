using MediatR;
using Rawaj.Application.Common.Interfaces;
using Rawaj.Application.Common.Models;
using Rawaj.Domain.Enums;

namespace Rawaj.Application.Features.Brands.UpdateBrandProfile;

/// <summary>
/// Partial update: only non-null fields are applied. This lets onboarding refine a brand profile
/// that Account Setup already created (with just a name) without needing to resend every field.
/// </summary>
public record UpdateBrandProfileCommand(
    Guid BrandProfileId,
    string? Name,
    string? Description,
    List<BrandVoice>? Tones,
    string? Tagline,
    string? Industry,
    string? TargetAudience,
    List<string>? Colors,
    string? LogoUrl,
    string? WebsiteUrl,
    List<string>? SupportedLanguages,
    List<string>? Keywords,
    string? Location,
    string? Instagram,
    string? BusinessAge,
    DateTime? BusinessEstablishDate,
    string? Stage,
    string? UniqueValue,
    string? PricePositioning,
    string? StorePresence,
    List<string>? ExistingPlatforms,
    string? AdmiredBrand1,
    string? AdmiredBrand2,
    string? AdmiredBrand3) : IRequest<Result<UpdateBrandProfileResponse>>, IRequireTenantRole, IRequireBrandAccess
{
    // Editor is the role invites use to grant "can edit this brand" access; BrandAccessAuthorizationBehavior
    // still requires a matching TenantMemberBrandAccess row for Editor/Viewer (Owner/Admin bypass it).
    public TenantMemberRole MinimumRole => TenantMemberRole.Editor;
}
