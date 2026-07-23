using Microsoft.EntityFrameworkCore;
using Rawaj.Application.Common.Exceptions;
using Rawaj.Application.Common.Interfaces;
using Rawaj.Domain.Enums;

namespace Rawaj.Application.Common.Policies;

public record ResolvedTenant(Guid TenantId, TenantMemberRole Role);

/// <summary>
/// A user can belong to more than one tenant now — their own auto-provisioned tenant plus any
/// tenant they've accepted an invite into. This is the single place that decides which one a
/// request acts in, shared by <see cref="Rawaj.Application.Common.Behaviors.TenantAuthorizationBehavior{TRequest,TResponse}"/>
/// and <c>GetMyTenantQueryHandler</c> so the two can never drift out of sync.
/// </summary>
public static class TenantResolutionPolicy
{
    public static async Task<ResolvedTenant?> ResolveAsync(
        IApplicationDbContext dbContext, Guid userId, Guid? requestedTenantId, CancellationToken cancellationToken)
    {
        if (requestedTenantId is not null)
        {
            var requested = await dbContext.TenantMembers
                .Where(m => m.UserId == userId && m.TenantId == requestedTenantId && m.InvitationStatus == InvitationStatus.Accepted)
                .Select(m => new ResolvedTenant(m.TenantId, m.Role))
                .FirstOrDefaultAsync(cancellationToken);

            if (requested is not null)
            {
                return requested;
            }

            // The caller asked for a specific tenant and isn't an accepted member of it — never
            // silently fall back to a different tenant here, that would leak data across tenants.
            throw new ForbiddenAccessException("You do not belong to this organization.");
        }

        var ownedTenant = await dbContext.TenantMembers
            .Where(m => m.UserId == userId
                && m.InvitationStatus == InvitationStatus.Accepted
                && dbContext.Tenants.Any(t => t.Id == m.TenantId && t.OwnerUserId == userId))
            .Select(m => new ResolvedTenant(m.TenantId, m.Role))
            .FirstOrDefaultAsync(cancellationToken);

        if (ownedTenant is not null)
        {
            return ownedTenant;
        }

        return await dbContext.TenantMembers
            .Where(m => m.UserId == userId && m.InvitationStatus == InvitationStatus.Accepted)
            .Select(m => new ResolvedTenant(m.TenantId, m.Role))
            .FirstOrDefaultAsync(cancellationToken);
    }
}
