using MediatR;
using Rawaj.Application.Common.Interfaces;
using Rawaj.Application.Common.Models;
using Rawaj.Domain.Enums;

namespace Rawaj.Application.Features.Brands.CreateBrandProfile;

public record CreateBrandProfileCommand(
    string Name,
    string? Description,
    BrandVoice? BrandVoice,
    string? Tagline,
    string? Industry,
    string? TargetAudience,
    List<string>? Colors,
    string? LogoUrl,
    string? WebsiteUrl,
    List<string>? SupportedLanguages,
    List<string>? Keywords) : IRequest<Result<CreateBrandProfileResponse>>, IRequireTenantRole
{
    public TenantMemberRole MinimumRole => TenantMemberRole.Admin;
}
