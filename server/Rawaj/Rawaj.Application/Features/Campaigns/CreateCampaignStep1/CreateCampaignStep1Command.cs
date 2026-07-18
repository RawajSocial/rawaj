using MediatR;
using Rawaj.Application.Common.Models;
using Rawaj.Application.Features.Campaigns.Common;

namespace Rawaj.Application.Features.Campaigns.CreateCampaignStep1;

public record CreateCampaignStep1Command(
    Guid BrandProfileId,
    string CampaignType) : IRequest<Result<CampaignResponse>>;
