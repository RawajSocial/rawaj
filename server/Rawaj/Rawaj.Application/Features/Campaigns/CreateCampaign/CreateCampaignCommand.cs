using MediatR;
using Rawaj.Application.Common.Interfaces;
using Rawaj.Application.Common.Models;
using Rawaj.Domain.Enums;

namespace Rawaj.Application.Features.Campaigns.CreateCampaign;

public record CreateCampaignCommand(
    Guid BrandProfileId,
    string Name,
    string? Objective,
    List<string> TargetPlatforms,
    DateOnly? StartDate,
    DateOnly? EndDate,
    decimal? BudgetAmount,
    string? BudgetCurrency) : IRequest<Result<CreateCampaignResponse>>, IRequireTenantRole
{
    public TenantMemberRole MinimumRole => TenantMemberRole.Editor;
}
