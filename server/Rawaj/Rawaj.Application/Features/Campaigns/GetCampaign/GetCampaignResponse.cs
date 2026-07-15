using Rawaj.Domain.Enums;

namespace Rawaj.Application.Features.Campaigns.GetCampaign;

public record GetCampaignResponse(
    Guid CampaignId,
    Guid BrandProfileId,
    string Name,
    string? Objective,
    List<string> TargetPlatforms,
    DateOnly? StartDate,
    DateOnly? EndDate,
    decimal? BudgetAmount,
    string? BudgetCurrency,
    CampaignStatus Status,
    string? AiPlanJson,
    DateTime? AiGeneratedAt,
    DateTime CreatedAt,
    DateTime UpdatedAt);
