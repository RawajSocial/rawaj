using MediatR;
using Rawaj.Application.Common.Models;
using Rawaj.Application.Features.Campaigns.Common;

namespace Rawaj.Application.Features.Campaigns.UpdateCampaignStep3;

public record UpdateCampaignStep3Command(
    Guid CampaignId,
    string? BrandName,
    string? Tagline,
    string? Instagram,
    string? Website,
    string? Sector,
    string? Location,
    string? BusinessAge,
    string? Stage,
    List<string>? BrandWords,
    List<string>? BrandTone,
    string? HasGuidelines,
    string? GuidelinesFileUrl,
    List<string>? BrandColors,
    string? LogoFileUrl,
    List<string>? Languages,
    string? ProductDesc,
    string? UniqueValue,
    string? PricePositioning,
    string? StorePresence,
    List<string>? ExistingPlatforms) : IRequest<Result<CampaignResponse>>;
