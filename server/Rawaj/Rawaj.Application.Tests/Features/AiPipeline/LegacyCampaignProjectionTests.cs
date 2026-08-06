using System.Text.Json;
using Rawaj.Application.Features.AiPipeline.Common;
using Xunit;

namespace Rawaj.Application.Tests.Features.AiPipeline;

public class LegacyCampaignProjectionTests
{
    [Fact]
    public void ProjectCompetitorResearch_MapsPositioningMapToSummary_AndCarriesTheRest()
    {
        const string artifact = """
            {"competitors":[{"name":"Roast Co","url":"https://roast.example","snippet":"a rival roaster"}],
             "positioningMap":"a synthesis of the competitive landscape",
             "contentPatterns":["reels"],"gaps":["no loyalty program"],
             "sources":[{"title":"t","url":"u"}],"unavailable":false,"note":null}
            """;

        var projected = JsonDocument.Parse(LegacyCampaignProjection.ProjectCompetitorResearch(artifact)).RootElement;

        Assert.Equal("a synthesis of the competitive landscape", projected.GetProperty("summary").GetString());
        Assert.Equal(1, projected.GetProperty("competitors").GetArrayLength());
        Assert.Equal(1, projected.GetProperty("sources").GetArrayLength());
        Assert.False(projected.GetProperty("unavailable").GetBoolean());
        // Fields the legacy shape never had (positioningMap's siblings) are not carried over —
        // ResearchCampaignCompetitorsCommandHandler never wrote them either.
        Assert.False(projected.TryGetProperty("contentPatterns", out _));
        Assert.False(projected.TryGetProperty("gaps", out _));
    }

    [Fact]
    public void ProjectCompetitorResearch_PreservesTheUnavailableSentinel()
    {
        const string artifact = """
            {"competitors":[],"sources":[],"unavailable":true,"note":"No competitor data was found for this brand."}
            """;

        var projected = JsonDocument.Parse(LegacyCampaignProjection.ProjectCompetitorResearch(artifact)).RootElement;

        Assert.True(projected.GetProperty("unavailable").GetBoolean());
        Assert.Equal("No competitor data was found for this brand.", projected.GetProperty("note").GetString());
        Assert.Equal(0, projected.GetProperty("competitors").GetArrayLength());
    }

    [Fact]
    public void ProjectDiagnosis_TakesBrandLevelReadingsFromBrandAnalysis_AndRisksFromCampaignAnalysis()
    {
        const string brandAnalysis = """
            {"businessSummary":"ملخص العمل","identityCore":"هوية","voiceProfile":{"tone":"ودود"},
             "valuePropositions":["جودة"],"differentiators":["محلي"],
             "swot":{"strengths":["s"],"weaknesses":[],"opportunities":[],"threats":[]},
             "businessMaturity":"نمو","marketingReadiness":"جاهز","missingInformation":["الميزانية"]}
            """;

        const string campaignAnalysis = """
            {"objectiveClassification":"وعي","audienceSegments":[{"name":"طلاب"}],"successCriteria":["وصول"],
             "risks":["منافسة قوية"],"opportunities":["موسم الصيف"],
             "researchQueries":{"market":[],"competitor":[]}}
            """;

        var projected = JsonDocument.Parse(
            LegacyCampaignProjection.ProjectDiagnosis(brandAnalysis, campaignAnalysis)).RootElement;

        Assert.Equal("ملخص العمل", projected.GetProperty("businessSummary").GetString());
        Assert.Equal("نمو", projected.GetProperty("businessMaturity").GetString());
        Assert.Equal("جاهز", projected.GetProperty("marketingReadiness").GetString());
        Assert.Equal("الميزانية", projected.GetProperty("missingInformation")[0].GetString());
        Assert.Equal("منافسة قوية", projected.GetProperty("currentRisks")[0].GetString());
        Assert.Equal("موسم الصيف", projected.GetProperty("opportunities")[0].GetString());
        // growthStage has no home in the split schema — deliberately absent, not null. The one
        // reader (onboarding-plan-approval.html) guards it with @if, so absence renders as nothing.
        Assert.False(projected.TryGetProperty("growthStage", out _));
    }
}
