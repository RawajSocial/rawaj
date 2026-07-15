using MediatR;
using Rawaj.Application.Common.Interfaces;
using Rawaj.Application.Common.Models;
using Rawaj.Domain.Enums;

namespace Rawaj.Application.Features.Analytics.SyncPostAnalytics;

public record SyncPostAnalyticsCommand(Guid ScheduledPostId)
    : IRequest<Result<PostAnalyticsSnapshot>>, IRequireTenantRole
{
    public TenantMemberRole MinimumRole => TenantMemberRole.Viewer;
}
