using MediatR;
using Microsoft.EntityFrameworkCore;
using Rawaj.Application.Common.Exceptions;
using Rawaj.Application.Common.Interfaces;
using Rawaj.Domain.Enums;

namespace Rawaj.Application.Common.Behaviors;

public class TenantAuthorizationBehavior<TRequest, TResponse>(
    ICurrentUserService currentUserService,
    ICurrentTenantContext currentTenantContext,
    IApplicationDbContext dbContext) : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        if (request is not IRequireTenantRole requirement)
        {
            return await next(cancellationToken);
        }

        var userId = currentUserService.UserId
            ?? throw new ForbiddenAccessException("You must be signed in to perform this action.");

        var membership = await dbContext.TenantMembers
            .Where(m => m.UserId == userId && m.InvitationStatus == InvitationStatus.Accepted)
            .Select(m => new { m.TenantId, m.Role })
            .FirstOrDefaultAsync(cancellationToken);

        if (membership is null)
        {
            throw new ForbiddenAccessException("You do not belong to an organization.");
        }

        if (!membership.Role.HasAtLeast(requirement.MinimumRole))
        {
            throw new ForbiddenAccessException("You do not have permission to perform this action.");
        }

        currentTenantContext.Set(membership.TenantId, membership.Role);

        return await next(cancellationToken);
    }
}
