namespace Rawaj.Application.Features.Campaigns.GetCampaignDeleteSummary;

/// <summary>What deleting this campaign will actually do, computed up front so the confirmation
/// modal can show real numbers instead of a generic warning. No coin figure — coin-ledger entries
/// aren't attributed back to a campaign anywhere today (every spend site records only a reason
/// string), so any total shown here would be a guess dressed up as a fact.</summary>
public record CampaignDeleteSummaryResponse(
    Guid CampaignId,
    string Name,
    int ContentItemCount,
    int ImageCount,
    int PendingScheduledCount,
    int PublishedScheduledCount);
