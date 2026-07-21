namespace Rawaj.Application.Common.Interfaces;

/// <summary>
/// Marks a request as scoped to a brand profile that isn't known directly (e.g. only a
/// CampaignId/ContentItemId/CompetitorId/ScheduledPostId is on the request). Checked by
/// BrandAccessAuthorizationBehavior the same way as IRequireBrandAccess, except the
/// BrandProfileId is resolved via a lookup first. Returning null (the referenced entity doesn't
/// exist, or doesn't belong to the caller's tenant) fails the check the same as a mismatched
/// brand - the handler still re-validates existence/tenant ownership itself as usual.
/// </summary>
public interface IRequireResolvedBrandAccess
{
    Task<Guid?> ResolveBrandProfileIdAsync(IApplicationDbContext dbContext, CancellationToken cancellationToken);
}
