using MediatR;
using Microsoft.EntityFrameworkCore;
using Rawaj.Application.Common.Exceptions;
using Rawaj.Application.Common.Interfaces;
using Rawaj.Domain.Enums;

namespace Rawaj.Application.Common.Behaviors;

/// <summary>
/// Runs after TenantAuthorizationBehavior, which has already resolved the caller's tenant role into
/// ICurrentTenantContext. Owner/Admin manage every brand profile in the tenant; Editor/Viewer are
/// restricted to the brand profiles they were explicitly invited to via TenantMemberBrandAccess.
/// Handles two request shapes: IRequireBrandAccess (BrandProfileId known directly) and
/// IRequireResolvedBrandAccess (BrandProfileId only reachable via another id, e.g. CampaignId).
/// </summary>
public class BrandAccessAuthorizationBehavior<TRequest, TResponse>(
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
        Guid? brandProfileId = request switch
        {
            IRequireBrandAccess direct => direct.BrandProfileId,
            IRequireResolvedBrandAccess resolved => await resolved.ResolveBrandProfileIdAsync(dbContext, cancellationToken),
            _ => null,
        };

        if (request is not (IRequireBrandAccess or IRequireResolvedBrandAccess))
        {
            return await next(cancellationToken);
        }

        var tenantId = currentTenantContext.TenantId!.Value;

        // A resolved BrandProfileId of null means the referenced entity (campaign/content item/etc.)
        // doesn't exist or doesn't resolve to a brand at all - let the handler produce its own
        // "not found" result rather than treating it as an access decision here.
        if (brandProfileId is null)
        {
            return await next(cancellationToken);
        }

        var brandBelongsToTenant = await dbContext.TenantBrandProfiles.AnyAsync(
            b => b.Id == brandProfileId.Value && b.TenantId == tenantId, cancellationToken);
        if (!brandBelongsToTenant)
        {
            throw new ForbiddenAccessException("You do not have access to this brand profile.");
        }

        if (currentTenantContext.Role!.Value.HasAtLeast(TenantMemberRole.Admin))
        {
            return await next(cancellationToken);
        }

        var userId = currentUserService.UserId!.Value;

        var hasAccess = await dbContext.TenantMemberBrandAccesses.AnyAsync(
            a => a.BrandProfileId == brandProfileId.Value
                && a.TenantMember.TenantId == tenantId
                && a.TenantMember.UserId == userId,
            cancellationToken);

        if (!hasAccess)
        {
            throw new ForbiddenAccessException("You do not have access to this brand profile.");
        }

        return await next(cancellationToken);
    }
}
