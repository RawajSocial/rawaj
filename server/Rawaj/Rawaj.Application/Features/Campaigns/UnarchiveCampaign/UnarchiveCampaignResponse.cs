using Rawaj.Domain.Enums;

namespace Rawaj.Application.Features.Campaigns.UnarchiveCampaign;

/// <param name="Status">
/// The status the campaign was restored to — the client needs it to know which step the campaign
/// resumes at, since a restored campaign can land on either side of the plan-approval gate.
/// </param>
public record UnarchiveCampaignResponse(Guid CampaignId, CampaignStatus Status);
