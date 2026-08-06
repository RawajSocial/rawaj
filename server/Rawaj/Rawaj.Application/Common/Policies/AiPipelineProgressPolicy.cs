using Rawaj.Domain.Entities.AiOperations;
using Rawaj.Domain.Enums;

namespace Rawaj.Application.Common.Policies;

/// <summary>Progress of a run, as the user should see it.</summary>
/// <param name="Percent">0–100, by finished stages over total stages.</param>
/// <param name="CompletedStages">Stages that reached a successful terminal state — including
/// skipped ones, which are finished as far as the user is concerned.</param>
/// <param name="TotalStages">Stages currently known. Grows when the content-image fan-out is
/// created, which is honest: nobody knows how many images there will be until the posts exist.</param>
/// <param name="CurrentStageLabel">Arabic label for what is happening now, or for why the run has
/// stopped.</param>
/// <param name="ImagesCompleted">Finished image stages, for the "3 of 10 images" readout.</param>
/// <param name="ImagesTotal">Total image stages, zero before the fan-out exists.</param>
public sealed record PipelineProgress(
    int Percent,
    int CompletedStages,
    int TotalStages,
    string CurrentStageLabel,
    int ImagesCompleted,
    int ImagesTotal);

/// <summary>
/// Derives what the UI shows about a run's progress, server-side, from the stage rows themselves.
///
/// <para>This exists to delete a guess. The content page currently animates a bar toward a 92% cap
/// over <c>2500 + postCount × 1600</c> milliseconds, because there is no server-side notion of
/// progress to report — a reload mid-generation shows nothing at all, which is the gap that lets an
/// impatient re-click pay for the same batch twice. Deriving it here rather than in the component
/// also means the label and the percentage cannot drift from what the backend is actually doing.</para>
/// </summary>
public static class AiPipelineProgressPolicy
{
    /// <summary>
    /// Arabic, because every user-facing string in this product is. These name what the user is
    /// waiting for, not the internal stage — "دراسة السوق" rather than "MarketResearch".
    /// </summary>
    private static readonly Dictionary<AiPipelineStageKind, string> StageLabels = new()
    {
        [AiPipelineStageKind.BrandAnalysis] = "تحليل هوية العلامة التجارية",
        [AiPipelineStageKind.CampaignAnalysis] = "تحليل أهداف الحملة",
        [AiPipelineStageKind.MarketResearch] = "دراسة السوق",
        [AiPipelineStageKind.CompetitorResearch] = "تحليل المنافسين",
        [AiPipelineStageKind.StrategyPositioning] = "بناء التموضع والاستراتيجية",
        [AiPipelineStageKind.StrategyBlueprint] = "إعداد خطة المحتوى",
        [AiPipelineStageKind.StrategyRoadmap] = "إعداد خارطة التنفيذ",
        [AiPipelineStageKind.StrategyAssemble] = "تجميع الاستراتيجية",
        [AiPipelineStageKind.HumanApproval] = "بانتظار اعتماد الاستراتيجية",
        [AiPipelineStageKind.ContentPlan] = "توليد المنشورات",
        [AiPipelineStageKind.ContentImage] = "توليد الصور"
    };

    public static string Label(AiPipelineStageKind kind) =>
        StageLabels.TryGetValue(kind, out var label) ? label : kind.ToString();

    public static PipelineProgress Calculate(
        IReadOnlyCollection<AiPipelineStage> stages, AiPipelineRunStatus runStatus)
    {
        if (stages.Count == 0)
        {
            return new PipelineProgress(0, 0, 0, "لم تبدأ بعد", 0, 0);
        }

        var completed = stages.Count(s => AiPipelinePolicy.SatisfiesDependency(s.Status));
        var percent = (int)Math.Round(completed * 100.0 / stages.Count);

        var images = stages.Where(s => s.Kind == AiPipelineStageKind.ContentImage).ToList();
        var imagesCompleted = images.Count(s => AiPipelinePolicy.SatisfiesDependency(s.Status));

        return new PipelineProgress(
            percent,
            completed,
            stages.Count,
            DescribeCurrent(stages, runStatus),
            imagesCompleted,
            images.Count);
    }

    private static string DescribeCurrent(
        IReadOnlyCollection<AiPipelineStage> stages, AiPipelineRunStatus runStatus)
    {
        // A stopped run's reason is more useful than the name of whatever stage it stopped on.
        switch (runStatus)
        {
            case AiPipelineRunStatus.Completed:
                return "اكتملت جميع الخطوات";
            case AiPipelineRunStatus.Cancelled:
                return "تم إلغاء العملية";
            case AiPipelineRunStatus.AwaitingCoins:
                return "الرصيد غير كافٍ لإكمال الخطوة التالية";
            case AiPipelineRunStatus.AwaitingApproval:
                return Label(AiPipelineStageKind.HumanApproval);
            case AiPipelineRunStatus.Failed:
                var failed = stages
                    .Where(s => s.Status == AiPipelineStageStatus.Failed)
                    .OrderBy(s => s.Ordinal)
                    .FirstOrDefault();
                return failed is null ? "تعذّر إكمال العملية" : $"تعذّر إكمال: {Label(failed.Kind)}";
        }

        // Prefer whatever is genuinely executing; fall back to what is next in line, so a run that
        // is between stages still says something truthful rather than going blank.
        var running = stages
            .Where(s => s.Status == AiPipelineStageStatus.Running)
            .OrderBy(s => s.Ordinal)
            .FirstOrDefault();

        var current = running ?? stages
            .Where(s => s.Status == AiPipelineStageStatus.Pending)
            .OrderBy(s => s.Ordinal)
            .FirstOrDefault();

        return current is null ? "جارٍ العمل…" : Label(current.Kind);
    }
}
