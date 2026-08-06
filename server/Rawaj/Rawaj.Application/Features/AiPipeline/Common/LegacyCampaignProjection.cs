using System.Text.Json.Nodes;

namespace Rawaj.Application.Features.AiPipeline.Common;

/// <summary>
/// Projects new pipeline artifacts into the three legacy JSON shapes on <c>MarketingCampaign</c> —
/// the write-through half of Phase 6. <c>Strategy</c> needs no projection here: it is already built
/// byte-compatible with <c>AiPlanJson</c> (see <c>StrategyAssembleExecutor</c>), so the orchestrator
/// assigns it directly.
///
/// <para>Two of these three legacy shapes don't line up field-for-field with the new artifacts they
/// now come from, because the split was designed around what belongs to the brand versus the
/// campaign, not around preserving the old column's exact field list. Both gaps are deliberate and
/// harmless to the pages that read them:</para>
///
/// <para><b>DiagnosisJson.growthStage</b> has no home in <c>BrandAnalysis</c>/<c>CampaignAnalysis</c>
/// (see docs/AI_PIPELINE.md §4) and is simply omitted here. The one reader,
/// <c>onboarding-plan-approval.html</c>, guards it with <c>@if (diag.growthStage)</c>, so an absent
/// field renders as nothing rather than an error.</para>
///
/// <para><b>CompetitorResearchJson.summary</b> used to be Tavily's raw answer text; the new
/// <c>CompetitorResearch</c> artifact doesn't carry a freeform summary; the closest equivalent is
/// <c>positioningMap</c>, a narrative synthesis of the same research, so that's what's projected into
/// <c>summary</c> here.</para>
/// </summary>
public static class LegacyCampaignProjection
{
    /// <summary>Projects a <c>CompetitorResearch</c> artifact into the
    /// <c>{summary, competitors, sources, unavailable, note}</c> shape
    /// <c>ResearchCampaignCompetitorsCommandHandler</c> used to write.</summary>
    public static string ProjectCompetitorResearch(string competitorResearchJson)
    {
        var source = JsonNode.Parse(competitorResearchJson)!.AsObject();

        var projected = new JsonObject
        {
            ["summary"] = source["positioningMap"]?.DeepClone(),
            ["competitors"] = source["competitors"]?.DeepClone() ?? new JsonArray(),
            ["sources"] = source["sources"]?.DeepClone() ?? new JsonArray(),
            ["unavailable"] = source["unavailable"]?.DeepClone() ?? false,
            ["note"] = source["note"]?.DeepClone()
        };

        return projected.ToJsonString();
    }

    /// <summary>Composes <c>BrandAnalysis</c> (brand-level readings) and <c>CampaignAnalysis</c>
    /// (campaign-level readings) into the single <c>DiagnosisJson</c> blob
    /// <c>GenerateBusinessDiagnosisCommandHandler</c> used to write in one call — the split
    /// <c>docs/AI_PIPELINE.md</c> §4 documents as deliberately preserving every field this composition
    /// needs.</summary>
    public static string ProjectDiagnosis(string brandAnalysisJson, string campaignAnalysisJson)
    {
        var brand = JsonNode.Parse(brandAnalysisJson)!.AsObject();
        var campaign = JsonNode.Parse(campaignAnalysisJson)!.AsObject();

        var projected = new JsonObject
        {
            ["businessSummary"] = brand["businessSummary"]?.DeepClone(),
            ["swot"] = brand["swot"]?.DeepClone(),
            ["businessMaturity"] = brand["businessMaturity"]?.DeepClone(),
            ["marketingReadiness"] = brand["marketingReadiness"]?.DeepClone(),
            ["currentRisks"] = campaign["risks"]?.DeepClone() ?? new JsonArray(),
            ["opportunities"] = campaign["opportunities"]?.DeepClone() ?? new JsonArray(),
            ["missingInformation"] = brand["missingInformation"]?.DeepClone() ?? new JsonArray()
        };

        return projected.ToJsonString();
    }
}
