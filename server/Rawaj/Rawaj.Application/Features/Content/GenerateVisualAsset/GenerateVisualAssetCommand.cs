using MediatR;
using Rawaj.Application.Common.Interfaces;
using Rawaj.Application.Common.Models;
using Rawaj.Domain.Enums;

namespace Rawaj.Application.Features.Content.GenerateVisualAsset;

/// <summary>BrandProfileId is optional — see GenerateContentItemCommand's doc comment for why.</summary>
public record GenerateVisualAssetCommand(
    Guid? BrandProfileId,
    Guid? CampaignId,
    Guid? ContentItemId,
    VisualAssetType Type,
    string Prompt)
    : IRequest<Result<GenerateVisualAssetResponse>>, IRequireTenantRole, IRequireResolvedBrandAccess
{
    public TenantMemberRole MinimumRole => TenantMemberRole.Editor;

    public Task<Guid?> ResolveBrandProfileIdAsync(IApplicationDbContext dbContext, CancellationToken cancellationToken) =>
        Task.FromResult(BrandProfileId);
}
