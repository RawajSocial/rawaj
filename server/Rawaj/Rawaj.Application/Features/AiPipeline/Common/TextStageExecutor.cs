using Rawaj.Application.Common.Interfaces;
using Rawaj.Application.Common.Models;
using Rawaj.Application.Common.Policies;
using Rawaj.Application.Features.AiPipeline.Prompts;
using Rawaj.Application.Features.Content.Common;
using Rawaj.Domain.Enums;

namespace Rawaj.Application.Features.AiPipeline.Common;

/// <summary>
/// Base for every stage whose work is "build a prompt, call the text model, validate the JSON".
///
/// <para>The sequence it owns — prompt, repair instruction when retrying a shape failure, model call
/// with this stage's template settings, provider-log row, fence-tolerant extraction, schema
/// validation — is identical for eight of the eleven stages. Each executor supplies only the two
/// things that differ: the prompt and the artifact kind.</para>
/// </summary>
public abstract class TextStageExecutor(
    IApplicationDbContext dbContext,
    IAiTextGenerationService textGenerationService,
    IPromptTemplateProvider templates) : IPipelineStageExecutor
{
    public abstract AiPipelineStageKind Kind { get; }

    protected abstract AiArtifactKind ArtifactKind { get; }

    protected virtual AiJobType JobType => AiJobType.PlanGeneration;

    /// <summary>Returns null when this stage cannot run for want of an input it needed. Rare — the
    /// graph normally guarantees dependencies — but the optional research stages can legitimately be
    /// absent, so a stage that genuinely cannot proceed says so rather than prompting with holes.</summary>
    protected abstract string? BuildPrompt(StageContext context);

    public virtual async Task<StageResult> ExecuteAsync(StageContext context, CancellationToken cancellationToken)
    {
        var prompt = BuildPrompt(context);
        if (prompt is null)
        {
            return StageResult.Failure(
                AiFailureKind.Validation, "This step is missing information it needs and cannot run.");
        }

        // Only ever appended on the attempt straight after a shape failure — see
        // AiPipelinePolicy.ShouldRepairPrompt for why it is not offered twice.
        if (context.RepairPrompt)
        {
            prompt += " " + PromptFragments.RepairInstruction;
        }

        var template = templates.For(Kind);
        var startedAt = DateTime.UtcNow;

        var generation = await textGenerationService.GenerateTextAsync(
            prompt,
            cancellationToken,
            new AiTextGenerationOptions(template.TaskName, template.JsonMode, template.Temperature));

        var job = AiJobRecorder.RecordText(dbContext, context, JobType, prompt, generation, startedAt);

        if (!generation.Succeeded)
        {
            // Classified rather than assumed transient: a quota rejection earns a long backoff, a
            // 503 a short one, and retrying either on the wrong schedule wastes provider quota.
            return StageResult.Failure(
                AiPipelinePolicy.ClassifyProviderError(generation.ErrorMessage),
                generation.ErrorMessage ?? "The AI provider did not return a response.",
                [job.Id]);
        }

        // JSON mode is requested but not guaranteed, and it constrains syntax rather than shape, so
        // the extractor stays in the path: it recovers a fenced or commentary-wrapped object that
        // would otherwise be discarded after the call had already been paid for.
        var payload = AiJsonResponseParser.ExtractJsonPayload(generation.Text);

        if (!ArtifactSchema.IsValid(ArtifactKind, payload, out var validationError))
        {
            return StageResult.Failure(
                payload is null ? AiFailureKind.Parse : AiFailureKind.Validation,
                validationError ?? "The AI response could not be read.",
                [job.Id]);
        }

        return BuildResult(context, payload!, [job.Id]);
    }

    /// <summary>Overridden by stages that do more than store their artifact — the content plan also
    /// creates rows and fans out, and research reports whether it found anything billable.</summary>
    protected virtual StageResult BuildResult(StageContext context, string artifactJson, IReadOnlyList<Guid> aiJobIds) =>
        StageResult.Success(ArtifactKind, artifactJson, aiJobIds);
}
