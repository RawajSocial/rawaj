using MediatR;
using Rawaj.Application.Common.Models;
using Rawaj.Application.Features.Campaigns.Common;
using Rawaj.Domain.Enums;

namespace Rawaj.Application.Features.Campaigns.GetCampaigns;

public record GetCampaignsQuery(
    string? Search,
    CampaignStatus? Status,
    string? Platform,
    DateOnly? From,
    DateOnly? To,
    int Page = 1,
    int PageSize = 20) : IRequest<Result<PagedResult<CampaignSummaryResponse>>>;
