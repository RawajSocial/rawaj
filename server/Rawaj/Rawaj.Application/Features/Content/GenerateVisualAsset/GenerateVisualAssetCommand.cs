using MediatR;
using Rawaj.Application.Common.Interfaces;
using Rawaj.Application.Common.Models;
using Rawaj.Domain.Enums;

namespace Rawaj.Application.Features.Content.GenerateVisualAsset;

public record GenerateVisualAssetCommand(
    Guid BrandProfileId,
    Guid? CampaignId,
    Guid? ContentItemId,
    VisualAssetType Type,
    string Prompt) : IRequest<Result<GenerateVisualAssetResponse>>, IRequireTenantRole, IRequireBrandAccess
{
    public TenantMemberRole MinimumRole => TenantMemberRole.Editor;
}
