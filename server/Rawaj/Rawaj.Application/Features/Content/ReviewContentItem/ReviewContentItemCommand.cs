using MediatR;
using Rawaj.Application.Common.Interfaces;
using Rawaj.Application.Common.Models;
using Rawaj.Domain.Enums;

namespace Rawaj.Application.Features.Content.ReviewContentItem;

public record ReviewContentItemCommand(Guid ContentItemId, bool Approve)
    : IRequest<Result<ReviewContentItemResponse>>, IRequireTenantRole
{
    public TenantMemberRole MinimumRole => TenantMemberRole.Admin;
}
