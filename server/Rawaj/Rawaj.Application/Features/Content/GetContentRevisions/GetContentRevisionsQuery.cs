using MediatR;
using Rawaj.Application.Common.Interfaces;
using Rawaj.Application.Common.Models;
using Rawaj.Domain.Enums;

namespace Rawaj.Application.Features.Content.GetContentRevisions;

public record GetContentRevisionsQuery(Guid ContentItemId)
    : IRequest<Result<List<ContentRevisionSummary>>>, IRequireTenantRole
{
    public TenantMemberRole MinimumRole => TenantMemberRole.Viewer;
}
