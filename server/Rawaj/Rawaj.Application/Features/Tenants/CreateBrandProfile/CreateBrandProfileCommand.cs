using MediatR;
using Rawaj.Application.Common.Models;
using Rawaj.Application.Features.Tenants.Common;
using Rawaj.Domain.Enums;

namespace Rawaj.Application.Features.Tenants.CreateBrandProfile;

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
    List<string>? Keywords) : IRequest<Result<TenantBrandProfileResponse>>;
