using MediatR;
using Rawaj.Application.Common.Models;
using Rawaj.Application.Features.Campaigns.Common;

namespace Rawaj.Application.Features.Campaigns.UpdateCampaignStep4;

public record UpdateCampaignStep4Command(
    Guid CampaignId,
    string? Gender,
    string? CustomerType,
    List<string>? AgeRanges,
    List<string>? IncomeLevel,
    string? CustomerLocation,
    string? EducationLevel,
    string? TargetDescription,
    List<string>? Interests,
    string? PainPoints,
    List<string>? BuyingBehavior,
    string? HasExistingCustomers,
    List<string>? AudiencePlatforms) : IRequest<Result<CampaignResponse>>;
