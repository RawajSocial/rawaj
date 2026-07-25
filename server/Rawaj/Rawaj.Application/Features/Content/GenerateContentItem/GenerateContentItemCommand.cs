using MediatR;
using Rawaj.Application.Common.Interfaces;
using Rawaj.Application.Common.Models;
using Rawaj.Domain.Enums;

namespace Rawaj.Application.Features.Content.GenerateContentItem;

/// <summary>
/// BrandProfileId is optional — omitting it produces a "standalone" generation not tied to any
/// brand, for trying the product out before a brand profile exists. When present, the normal
/// brand-scoped access check (Editor/Viewer restricted to their assigned brands) still applies;
/// when absent, only the tenant-role check runs (see ResolveBrandProfileIdAsync).
/// </summary>
public record GenerateContentItemCommand(
    Guid? BrandProfileId,
    Guid? CampaignId,
    ContentType ContentType,
    SocialPlatform Platform,
    Language Language,
    string? Tone,
    string? AdditionalInstructions,
    ContentTemplateStyle TemplateStyle = ContentTemplateStyle.Auto)
    : IRequest<Result<GenerateContentItemResponse>>, IRequireTenantRole, IRequireResolvedBrandAccess
{
    public TenantMemberRole MinimumRole => TenantMemberRole.Editor;

    public Task<Guid?> ResolveBrandProfileIdAsync(IApplicationDbContext dbContext, CancellationToken cancellationToken) =>
        Task.FromResult(BrandProfileId);
}
