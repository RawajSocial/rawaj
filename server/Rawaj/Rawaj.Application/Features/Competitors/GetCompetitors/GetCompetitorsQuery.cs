using MediatR;
using Rawaj.Application.Common.Interfaces;
using Rawaj.Application.Common.Models;
using Rawaj.Domain.Enums;

namespace Rawaj.Application.Features.Competitors.GetCompetitors;

public record GetCompetitorsQuery(Guid BrandProfileId, int Page = 1, int PageSize = 20)
    : IRequest<Result<PagedResult<CompetitorSummary>>>, IRequireTenantRole, IRequireBrandAccess
{
    public TenantMemberRole MinimumRole => TenantMemberRole.Viewer;
}
