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
    BrandVoice? BrandVoice,
    string? Tagline,
    string? Industry,
    string? TargetAudience,
    List<string>? Colors,
    string? LogoUrl,
    string? WebsiteUrl,
    List<string>? SupportedLanguages,
    List<string>? Keywords) : IRequest<Result<UpdateBrandProfileResponse>>, IRequireTenantRole
{
    public TenantMemberRole MinimumRole => TenantMemberRole.Admin;
}
