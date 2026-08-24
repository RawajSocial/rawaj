using Rawaj.Domain.Enums;

namespace Rawaj.Application.Features.Campaigns.GetCampaigns;

/// <summary>
/// List-row projection of a campaign. Objective/TargetPlatforms/Budget are carried here (rather
/// than only on GetCampaignResponse) because the campaigns list renders them per card - without
/// them the card had nothing to bind to and fell back to hardcoded zeros. PlanApprovedAt lets the
/// list send the user to the right next step (review the strategy vs. review the content) instead
/// of dead-ending on the detail page.
/// </summary>
public record CampaignSummary(
    Guid CampaignId,
    Guid BrandProfileId,
    string Name,
    CampaignStatus Status,
    DateOnly? StartDate,
    DateOnly? EndDate,
    DateTime CreatedAt,
    string? Objective,
    List<string> TargetPlatforms,
    decimal? BudgetAmount,
    string? BudgetCurrency,
    DateTime? PlanApprovedAt,
    int ContentItemCount,
    DateTime? OnboardingCompletedAt);
