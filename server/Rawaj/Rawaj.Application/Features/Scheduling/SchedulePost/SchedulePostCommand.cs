using MediatR;
using Rawaj.Application.Common.Interfaces;
using Rawaj.Application.Common.Models;
using Rawaj.Domain.Enums;

namespace Rawaj.Application.Features.Scheduling.SchedulePost;

public record SchedulePostCommand(
    Guid ContentItemId,
    Guid? VisualAssetId,
    Guid SocialAccountId,
    DateTime ScheduledAt) : IRequest<Result<SchedulePostResponse>>, IRequireTenantRole
{
    public TenantMemberRole MinimumRole => TenantMemberRole.Editor;
}
