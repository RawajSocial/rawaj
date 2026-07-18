using MediatR;
using Rawaj.Application.Common.Models;
using Rawaj.Application.Features.Campaigns.Common;

namespace Rawaj.Application.Features.Campaigns.UpdateCampaignStep7;

public record UpdateCampaignStep7Command(
    Guid CampaignId,
    List<StrategistQADto> Answers) : IRequest<Result<CampaignResponse>>;
