using MediatR;
using Rawaj.Application.Common.Interfaces;
using Rawaj.Application.Common.Models;
using Rawaj.Domain.Enums;

namespace Rawaj.Application.Features.Content.ReviewVisualAsset;

public record ReviewVisualAssetCommand(Guid VisualAssetId, bool Approve)
    : IRequest<Result<ReviewVisualAssetResponse>>, IRequireTenantRole
{
    public TenantMemberRole MinimumRole => TenantMemberRole.Admin;
}
