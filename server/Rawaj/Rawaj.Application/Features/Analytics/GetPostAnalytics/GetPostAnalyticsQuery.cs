using MediatR;
using Rawaj.Application.Common.Interfaces;
using Rawaj.Application.Common.Models;
using Rawaj.Domain.Enums;

namespace Rawaj.Application.Features.Analytics.GetPostAnalytics;

public record GetPostAnalyticsQuery(Guid ScheduledPostId)
    : IRequest<Result<List<PostAnalyticsSnapshot>>>, IRequireTenantRole
{
    public TenantMemberRole MinimumRole => TenantMemberRole.Viewer;
}
