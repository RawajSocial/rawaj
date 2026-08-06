using Rawaj.Application.Common.Models;
using Rawaj.Application.Common.Services;
using Rawaj.Domain.Enums;
using Rawaj.Infrastructure.Ai;
using Xunit;

namespace Rawaj.Application.Tests.Common;

/// <summary>
/// Per-task model selection and the per-stage generation settings that drive it. The behaviour that
/// matters most here is the boring one: with nothing configured, everything must resolve exactly as
/// it did before per-task models existed.
/// </summary>
public class PromptTemplateAndModelSelectionTests
{
    [Fact]
    public void WithNothingConfigured_EveryTaskUsesTheDefaultModel()
    {
        var settings = new GroqSettings { Model = "llama-3.3-70b-versatile" };

        Assert.Equal("llama-3.3-70b-versatile", settings.ResolveModel(null));
        Assert.Equal("llama-3.3-70b-versatile", settings.ResolveModel("Strategy"));
        Assert.Equal("llama-3.3-70b-versatile", settings.ResolveModel("AnythingElse"));
    }

    [Fact]
    public void AConfiguredTask_OverridesTheDefault_WithoutAffectingOtherTasks()
    {
        var settings = new GroqSettings
        {
            Model = "llama-3.3-70b-versatile",
            Models = { ["Strategy"] = "a-better-and-pricier-model" }
        };

        Assert.Equal("a-better-and-pricier-model", settings.ResolveModel("Strategy"));
        Assert.Equal("llama-3.3-70b-versatile", settings.ResolveModel("Content"));
    }

    [Fact]
    public void AnEmptyConfiguredValue_FallsBackRatherThanRequestingAnEmptyModel()
    {
        // A half-edited config file should degrade to the working default, not send model: "" and
        // fail every generation.
        var settings = new GroqSettings
        {
            Model = "llama-3.3-70b-versatile",
            Models = { ["Strategy"] = "   " }
        };

        Assert.Equal("llama-3.3-70b-versatile", settings.ResolveModel("Strategy"));
    }

    [Fact]
    public void DefaultOptions_RequestNothingExtra()
    {
        // The guarantee that makes this commit safe: an existing caller passing no options sends the
        // same request it always did.
        var options = AiTextGenerationOptions.Default;

        Assert.Null(options.TaskName);
        Assert.False(options.JsonMode);
        Assert.Null(options.Temperature);
        Assert.Null(options.Seed);
    }

    [Fact]
    public void EveryStageHasATemplate()
    {
        var provider = new PromptTemplateProvider();

        foreach (var kind in Enum.GetValues<AiPipelineStageKind>())
        {
            var template = provider.For(kind);
            Assert.False(string.IsNullOrWhiteSpace(template.TaskName));
        }
    }

    [Fact]
    public void StagesWhoseOutputIsParsed_AskForJsonMode()
    {
        var provider = new PromptTemplateProvider();

        AiPipelineStageKind[] jsonProducing =
        [
            AiPipelineStageKind.BrandAnalysis,
            AiPipelineStageKind.CampaignAnalysis,
            AiPipelineStageKind.MarketResearch,
            AiPipelineStageKind.CompetitorResearch,
            AiPipelineStageKind.StrategyPositioning,
            AiPipelineStageKind.StrategyBlueprint,
            AiPipelineStageKind.StrategyRoadmap,
            AiPipelineStageKind.ContentPlan
        ];

        Assert.All(jsonProducing, kind => Assert.True(provider.For(kind).JsonMode));

        // The image stage sends its prompt to a diffusion model, which has no such concept.
        Assert.False(provider.For(AiPipelineStageKind.ContentImage).JsonMode);
    }

    [Fact]
    public void AnalysisRunsColderThanCopywriting()
    {
        // Analysis is structural — the same inputs should yield the same reading. A batch of posts
        // that all sound identical is a defect, so content generation runs warm.
        var provider = new PromptTemplateProvider();

        var analysis = provider.For(AiPipelineStageKind.CampaignAnalysis).Temperature;
        var content = provider.For(AiPipelineStageKind.ContentPlan).Temperature;

        Assert.NotNull(analysis);
        Assert.NotNull(content);
        Assert.True(analysis < content);
    }
}
