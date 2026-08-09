using Rawaj.Application.Features.Content.Common;
using Rawaj.Domain.Entities.Campaigns;
using Rawaj.Domain.Entities.Tenants;
using Rawaj.Domain.Enums;
using Xunit;

namespace Rawaj.Application.Tests.Features.Campaigns;

/// <summary>
/// Guards two properties of the campaign content prompt that are invisible at runtime — nothing
/// fails loudly if either regresses, the output just quietly gets worse:
/// <list type="number">
/// <item>The approved strategy actually reaches the model. Content generation is gated on
/// <c>PlanApprovedAt</c>, so if the strategy isn't in the prompt the approval step is decorative
/// and the user paid for a plan that influences nothing.</item>
/// <item>The model is asked for an English image prompt. The image model renders Arabic prompts
/// badly, and the post copy is persuasion text rather than a description of a picture.</item>
/// </list>
/// </summary>
public class CampaignContentPromptTests
{
    private static TenantBrandProfile Brand() => new()
    {
        Id = Guid.NewGuid(),
        TenantId = Guid.NewGuid(),
        Name = "Test Brand",
        Status = BrandProfileStatus.Active,
    };

    private static MarketingCampaign Campaign(string? aiPlanJson = null, string? briefJson = null) => new()
    {
        Id = Guid.NewGuid(),
        BrandProfileId = Guid.NewGuid(),
        Name = "Summer Push",
        Objective = "sales",
        TargetPlatforms = ["Instagram"],
        AiPlanJson = aiPlanJson,
        BriefJson = briefJson,
    };

    private static string Build(MarketingCampaign campaign) =>
        ContentPromptBuilder.BuildCampaignContentPlanPrompt(
            Brand(), campaign, competitorInsights: [], postingTimeSummary: "",
            postCount: 3, platforms: [SocialPlatform.Instagram], language: Language.Ar,
            strategyJson: campaign.AiPlanJson, briefJson: campaign.BriefJson);

    [Fact]
    public void ContentPlanPrompt_IncludesTheApprovedStrategy()
    {
        const string strategy = "{\"campaignBlueprint\":{\"pillars\":[\"SUMMER_PILLAR_MARKER\"]}}";

        var prompt = Build(Campaign(aiPlanJson: strategy));

        Assert.Contains(strategy, prompt);
        Assert.Contains("reviewed and approved", prompt);
        // Named explicitly so the model is told which parts to follow, not just handed a blob.
        Assert.Contains("campaignBlueprint.pillars", prompt);
        Assert.Contains("contentMix", prompt);
    }

    [Fact]
    public void ContentPlanPrompt_IncludesTheOnboardingBrief()
    {
        const string brief = "{\"targetDescription\":\"BRIEF_MARKER\"}";

        var prompt = Build(Campaign(briefJson: brief));

        Assert.Contains(brief, prompt);
    }

    [Fact]
    public void ContentPlanPrompt_AsksForAnEnglishImagePromptPerPost()
    {
        var prompt = Build(Campaign());

        Assert.Contains("imagePrompt", prompt);
        Assert.Contains("MUST be written in English", prompt);
        // The image prompt must describe a picture, not repeat the ad copy.
        Assert.Contains("not the marketing message", prompt);
    }

    [Fact]
    public void ContentPlanPrompt_OmitsStrategyAndBriefSectionsWhenAbsent()
    {
        // A campaign with neither (legacy rows, or a brief that was never filled) must still
        // produce a valid prompt rather than a stray "Approved strategy JSON: " with nothing after.
        // Note: the standalone injection guard sentence (present in every prompt, unconditionally)
        // happens to say "onboarding answers" too — these checks use phrases specific to the
        // strategy/brief sections themselves, not that generic guard text.
        var prompt = Build(Campaign());

        Assert.DoesNotContain("Approved strategy JSON", prompt);
        Assert.DoesNotContain("audience and tone specifics", prompt);
        Assert.Contains("Create 3 distinct social media posts", prompt);
    }

    [Fact]
    public void ImagePrompt_ForbidsRenderedTextAndDeclaresEnglish()
    {
        // Diffusion models mangle lettering, and Arabic script worst of all — the caption belongs
        // next to the image, never inside it.
        var prompt = ContentPromptBuilder.BuildImagePrompt(
            Brand(), Campaign(), "Image", "a barista pouring latte art in a sunlit cafe");

        Assert.Contains("no words, letters, captions, logos or watermarks", prompt);
        Assert.Contains("Interpret this prompt as English", prompt);
    }

    [Fact]
    public void StandaloneImagePrompt_CarriesTheSameGuard()
    {
        var prompt = ContentPromptBuilder.BuildStandaloneImagePrompt("Image", "a mountain at sunrise");

        Assert.Contains("no words, letters, captions, logos or watermarks", prompt);
        Assert.Contains("Interpret this prompt as English", prompt);
    }
}
