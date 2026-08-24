using MediatR;
using Rawaj.Application.Common.Interfaces;
using Rawaj.Application.Common.Models;
using Rawaj.Domain.Enums;

namespace Rawaj.Application.Features.Campaigns.GetCampaigns;

/// <param name="IncludeArchived">
/// Archiving is this product's soft delete — the onboarding wizard archives every abandoned draft,
/// so returning archived campaigns by default filled the client's list (and its pagination) with
/// rows the UI immediately discarded. Callers that genuinely want them (the "مؤرشفة" filter) opt in.
/// </param>
public record GetCampaignsQuery(Guid? BrandProfileId, int Page = 1, int PageSize = 20, bool IncludeArchived = false)
    : IRequest<Result<PagedResult<CampaignSummary>>>, IRequireTenantRole, IRequireResolvedBrandAccess
{
    public TenantMemberRole MinimumRole => TenantMemberRole.Viewer;

    // When BrandProfileId is omitted, there's nothing to check here - the handler restricts an
    // Editor/Viewer caller to their accessible brands itself in that case.
    public Task<Guid?> ResolveBrandProfileIdAsync(IApplicationDbContext dbContext, CancellationToken cancellationToken) =>
        Task.FromResult(BrandProfileId);
}
