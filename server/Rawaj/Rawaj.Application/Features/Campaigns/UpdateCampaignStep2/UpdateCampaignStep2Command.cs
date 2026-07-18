using MediatR;
using Rawaj.Application.Common.Models;
using Rawaj.Application.Features.Campaigns.Common;
using Rawaj.Application.Features.Campaigns.Common.CampaignBriefRequests;

namespace Rawaj.Application.Features.Campaigns.UpdateCampaignStep2;

public record UpdateCampaignStep2Command(
    Guid CampaignId,
    string? CampaignName,
    string? CampaignGoal,
    DateOnly? CampaignStartDate,
    string? CampaignDuration,
    string? CampaignOutcome,
    CampaignBriefRequest Brief) : IRequest<Result<CampaignResponse>>;
