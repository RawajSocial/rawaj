using MediatR;
using Rawaj.Application.Common.Models;
using Rawaj.Application.Features.Campaigns.Common;

namespace Rawaj.Application.Features.Campaigns.UpdateCampaignStep5;

public record UpdateCampaignStep5Command(
    Guid CampaignId,
    string? PositioningVs,
    string? CampaignOutcome,
    List<string>? SuccessMetrics,
    List<string>? BrandsAdmired,
    string? MonthlyBudget,
    decimal? BudgetFrom,
    decimal? BudgetTo,
    List<string>? PlatformRanking,
    List<string>? Goals,
    string? Timeframe,
    string? AgencyExperience,
    string? TargetSales) : IRequest<Result<CampaignResponse>>;
