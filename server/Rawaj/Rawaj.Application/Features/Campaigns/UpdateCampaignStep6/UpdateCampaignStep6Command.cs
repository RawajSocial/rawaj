using MediatR;
using Rawaj.Application.Common.Models;
using Rawaj.Application.Features.Campaigns.Common;

namespace Rawaj.Application.Features.Campaigns.UpdateCampaignStep6;

public record UpdateCampaignStep6Command(
    Guid CampaignId,
    List<string>? CampaignPhotoUrls,
    string? Hashtags,
    string? AdditionalNotes,
    SocialConnectionDto? Facebook,
    SocialConnectionDto? Instagram) : IRequest<Result<CampaignResponse>>;
