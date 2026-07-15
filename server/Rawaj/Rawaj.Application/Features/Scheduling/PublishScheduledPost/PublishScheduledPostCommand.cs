using MediatR;
using Rawaj.Application.Common.Interfaces;
using Rawaj.Application.Common.Models;
using Rawaj.Domain.Enums;

namespace Rawaj.Application.Features.Scheduling.PublishScheduledPost;

public record PublishScheduledPostCommand(Guid ScheduledPostId)
    : IRequest<Result<PublishScheduledPostResponse>>, IRequireTenantRole
{
    public TenantMemberRole MinimumRole => TenantMemberRole.Editor;
}
