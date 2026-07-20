using MediatR;
using Rawaj.Application.Common.Interfaces;
using Rawaj.Application.Common.Models;
using Rawaj.Application.Features.Campaigns.Common;

namespace Rawaj.Application.Features.Campaigns.GetCampaigns;

public class GetCampaignsQueryHandler(
    ICampaignOnboardingService onboardingService,
    ICurrentUserService currentUserService)
    : IRequestHandler<GetCampaignsQuery, Result<PagedResult<CampaignSummaryResponse>>>
{
    public async Task<Result<PagedResult<CampaignSummaryResponse>>> Handle(GetCampaignsQuery request, CancellationToken cancellationToken)
    {
        if (currentUserService.UserId is not { } userId)
        {
            return Result<PagedResult<CampaignSummaryResponse>>.Failure("Not authenticated.");
        }

        var page = request.Page < 1 ? 1 : request.Page;
        var pageSize = request.PageSize switch
        {
            < 1 => 20,
            > 100 => 100,
            _ => request.PageSize
        };

        var (items, totalCount) = await onboardingService.GetPagedAsync(userId, request.Search, request.Status, request.Platform, request.From, request.To, page, pageSize, cancellationToken);

        var responses = items.Select(CampaignSummaryResponse.FromEntity).ToList();

        return Result<PagedResult<CampaignSummaryResponse>>.Success(new PagedResult<CampaignSummaryResponse>(responses, page, pageSize, totalCount));
    }
}
