namespace Rawaj.Application.Features.Campaigns.ResearchCampaignCompetitors;

/// <summary>
/// Result of a (best-effort) competitor research pass. <c>Succeeded</c> reflects whether Tavily
/// actually returned usable data — this command itself always returns <c>Result.Success</c>
/// (never blocks the campaign flow), so the caller checks this flag to decide whether to show a
/// "competitor research unavailable" note instead of treating it as an error.
/// </summary>
public record ResearchCampaignCompetitorsResponse(
    Guid CampaignId, string CompetitorResearchJson, bool Succeeded, string? Note, DateTime ResearchedAt);
