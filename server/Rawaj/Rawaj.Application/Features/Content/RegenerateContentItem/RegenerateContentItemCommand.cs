using MediatR;
using Rawaj.Application.Common.Interfaces;
using Rawaj.Application.Common.Models;
using Rawaj.Domain.Enums;

namespace Rawaj.Application.Features.Content.RegenerateContentItem;

public record RegenerateContentItemCommand(Guid ContentItemId, string Feedback)
    : IRequest<Result<RegenerateContentItemResponse>>, IRequireTenantRole
{
    public TenantMemberRole MinimumRole => TenantMemberRole.Editor;
}
