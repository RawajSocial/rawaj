using Rawaj.Application.Common.Policies;
using Rawaj.Domain.Entities.AiOperations;
using Rawaj.Domain.Enums;
using Xunit;

namespace Rawaj.Application.Tests.Policies;

/// <summary>
/// Server-derived progress — what replaces the content page's simulated bar, which currently ticks
/// toward a 92% cap on a hardcoded duration because the backend has nothing real to report.
/// </summary>
public class AiPipelineProgressPolicyTests
{
    [Fact]
    public void FreshRun_IsZeroPercent_AndNamesTheFirstStage()
    {
        var stages = FreshRun();

        var progress = AiPipelineProgressPolicy.Calculate(stages, AiPipelineRunStatus.Running);

        Assert.Equal(0, progress.Percent);
        Assert.Equal(0, progress.CompletedStages);
        Assert.Equal(AiPipelineProgressPolicy.Label(AiPipelineStageKind.BrandAnalysis), progress.CurrentStageLabel);
    }

    [Fact]
    public void SkippedStages_CountAsProgress()
    {
        // An optional stage that gave up is finished as far as the user is concerned; showing it as
        // outstanding would make a healthy run look stalled.
        var stages = FreshRun();
        stages.Single(s => s.Kind == AiPipelineStageKind.BrandAnalysis).Status = AiPipelineStageStatus.Completed;
        stages.Single(s => s.Kind == AiPipelineStageKind.MarketResearch).Status = AiPipelineStageStatus.Skipped;

        var progress = AiPipelineProgressPolicy.Calculate(stages, AiPipelineRunStatus.Running);

        Assert.Equal(2, progress.CompletedStages);
    }

    [Fact]
    public void RunningStage_IsPreferredOverTheNextPendingOne()
    {
        var stages = FreshRun();
        stages.Single(s => s.Kind == AiPipelineStageKind.BrandAnalysis).Status = AiPipelineStageStatus.Completed;
        stages.Single(s => s.Kind == AiPipelineStageKind.CampaignAnalysis).Status = AiPipelineStageStatus.Running;

        var progress = AiPipelineProgressPolicy.Calculate(stages, AiPipelineRunStatus.Running);

        Assert.Equal(AiPipelineProgressPolicy.Label(AiPipelineStageKind.CampaignAnalysis), progress.CurrentStageLabel);
    }

    [Fact]
    public void ImageFanOut_IsReportedSeparately_ForTheThreeOfTenReadout()
    {
        var stages = FreshRun();
        foreach (var stage in stages)
        {
            stage.Status = AiPipelineStageStatus.Completed;
        }

        for (var i = 0; i < 10; i++)
        {
            stages.Add(new AiPipelineStage
            {
                Id = Guid.NewGuid(),
                Kind = AiPipelineStageKind.ContentImage,
                Ordinal = (int)AiPipelineStageKind.ContentImage,
                Status = i < 3 ? AiPipelineStageStatus.Completed : AiPipelineStageStatus.Pending,
                TargetRefId = Guid.NewGuid(),
                CreatedAt = DateTime.UtcNow
            });
        }

        var progress = AiPipelineProgressPolicy.Calculate(stages, AiPipelineRunStatus.Running);

        Assert.Equal(3, progress.ImagesCompleted);
        Assert.Equal(10, progress.ImagesTotal);
        Assert.Equal(AiPipelineProgressPolicy.Label(AiPipelineStageKind.ContentImage), progress.CurrentStageLabel);
    }

    [Fact]
    public void StoppedRuns_ReportWhyRatherThanWhere()
    {
        var stages = FreshRun();

        Assert.Equal(
            "الرصيد غير كافٍ لإكمال الخطوة التالية",
            AiPipelineProgressPolicy.Calculate(stages, AiPipelineRunStatus.AwaitingCoins).CurrentStageLabel);

        Assert.Equal(
            "تم إلغاء العملية",
            AiPipelineProgressPolicy.Calculate(stages, AiPipelineRunStatus.Cancelled).CurrentStageLabel);

        Assert.Equal(
            AiPipelineProgressPolicy.Label(AiPipelineStageKind.HumanApproval),
            AiPipelineProgressPolicy.Calculate(stages, AiPipelineRunStatus.AwaitingApproval).CurrentStageLabel);
    }

    [Fact]
    public void FailedRun_NamesTheStageThatFailed()
    {
        var stages = FreshRun();
        stages.Single(s => s.Kind == AiPipelineStageKind.CampaignAnalysis).Status = AiPipelineStageStatus.Failed;

        var progress = AiPipelineProgressPolicy.Calculate(stages, AiPipelineRunStatus.Failed);

        Assert.Contains(AiPipelineProgressPolicy.Label(AiPipelineStageKind.CampaignAnalysis), progress.CurrentStageLabel);
    }

    [Fact]
    public void CompletedRun_IsOneHundredPercent()
    {
        var stages = FreshRun();
        foreach (var stage in stages)
        {
            stage.Status = AiPipelineStageStatus.Completed;
        }

        var progress = AiPipelineProgressPolicy.Calculate(stages, AiPipelineRunStatus.Completed);

        Assert.Equal(100, progress.Percent);
        Assert.Equal("اكتملت جميع الخطوات", progress.CurrentStageLabel);
    }

    [Fact]
    public void EveryStageKind_HasAnArabicLabel()
    {
        // A missing entry falls back to the enum name, which would put "StrategyBlueprint" in front
        // of an Arabic-first user.
        foreach (var kind in Enum.GetValues<AiPipelineStageKind>())
        {
            Assert.NotEqual(kind.ToString(), AiPipelineProgressPolicy.Label(kind));
        }
    }

    private static List<AiPipelineStage> FreshRun() =>
    [
        .. AiPipelinePolicy.InitialStages().Select(d => new AiPipelineStage
        {
            Id = Guid.NewGuid(),
            Kind = d.Kind,
            Ordinal = d.Ordinal,
            Status = AiPipelineStageStatus.Pending,
            IsOptional = d.IsOptional,
            MaxAttempts = d.MaxAttempts,
            CreatedAt = DateTime.UtcNow
        })
    ];
}
