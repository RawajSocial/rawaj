using MediatR;
using Rawaj.Application.Common.Exceptions;
using Rawaj.Application.Common.Interfaces;
using Rawaj.Application.Common.Policies;
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

        var resolved = await TenantResolutionPolicy.ResolveAsync(
            dbContext, userId, currentUserService.RequestedTenantId, cancellationToken);

        if (resolved is null)
        {
            throw new ForbiddenAccessException("You do not belong to an organization.");
        }

        if (!resolved.Role.HasAtLeast(requirement.MinimumRole))
        {
            throw new ForbiddenAccessException("You do not have permission to perform this action.");
        }

        currentTenantContext.Set(resolved.TenantId, resolved.Role);

        return await next(cancellationToken);
    }
}
