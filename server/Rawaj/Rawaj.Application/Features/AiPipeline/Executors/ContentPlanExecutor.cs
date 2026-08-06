using Microsoft.EntityFrameworkCore;
using Rawaj.Application.Common.Interfaces;
using Rawaj.Application.Common.Models;
using Rawaj.Application.Common.Policies;
using Rawaj.Application.Features.AiPipeline.Common;
using Rawaj.Application.Features.AiPipeline.Prompts;
using Rawaj.Application.Features.Content.Common;
using Rawaj.Application.Features.Scheduling.Common;
using Rawaj.Domain.Entities.Campaigns;
using Rawaj.Domain.Enums;

namespace Rawaj.Application.Features.AiPipeline.Executors;

/// <summary>
/// The campaign content batch — the text half of what used to be one call that also generated every
/// image inline. <see cref="Executors.ContentImageExecutor"/> is the other half: this stage fans out
/// one stage row per post it creates, via <see cref="StageResult.FanOutTargets"/>, which is what
/// makes a single post's image retryable without regenerating the whole batch.
///
/// <para>Not a <see cref="Common.TextStageExecutor"/>: gathering competitor insights and posting-time
/// guidance needs its own database queries before the prompt can even be built, which
/// <c>TextStageExecutor.BuildPrompt</c>'s synchronous contract has no room for.</para>
///
/// <para>Post count, language, template style and whether to generate images at all come from
/// <see cref="StageContext.Run"/>'s content parameters (<c>AiPipelineRun.ContentPostCount</c> etc.) —
/// the one piece of run-level configuration the graph carries, since only this stage and
/// <see cref="Executors.ContentImageExecutor"/> need any.</para>
/// </summary>
public class ContentPlanExecutor(
    IApplicationDbContext dbContext,
    IAiTextGenerationService textGenerationService,
    IPromptTemplateProvider templates) : IPipelineStageExecutor
{
    private const int MaxCompetitorInsights = 5;

    public AiPipelineStageKind Kind => AiPipelineStageKind.ContentPlan;

    public async Task<StageResult> ExecuteAsync(StageContext context, CancellationToken cancellationToken)
    {
        if (context.Campaign is null)
        {
            return StageResult.Failure(AiFailureKind.Validation, "This step is missing information it needs and cannot run.");
        }

        var campaign = context.Campaign;

        var platforms = await dbContext.SocialAccounts
            .Where(s => s.BrandProfileId == context.Brand.Id && s.IsActive)
            .Select(s => s.Platform)
            .Distinct()
            .ToListAsync(cancellationToken);

        if (platforms.Count == 0)
        {
            platforms = campaign.TargetPlatforms
                .Select(p => Enum.TryParse<SocialPlatform>(p, true, out var parsed) ? (SocialPlatform?)parsed : null)
                .Where(p => p.HasValue)
                .Select(p => p!.Value)
                .Distinct()
                .ToList();
        }

        if (platforms.Count == 0)
        {
            platforms = [SocialPlatform.Instagram, SocialPlatform.Facebook];
        }

        var competitorInsights = await dbContext.RagDocuments
            .Where(d => d.BrandProfileId == context.Brand.Id && d.CompetitorsData != null)
            .OrderByDescending(d => d.CreatedAt)
            .Take(MaxCompetitorInsights)
            .Select(d => d.CompetitorsData!)
            .ToListAsync(cancellationToken);

        var timingSuggestions = await PostingTimeIntelligence.GetSuggestionsAsync(dbContext, context.Brand.Id, platforms, cancellationToken);
        var timingSummary = PostingTimeIntelligence.BuildSummary(timingSuggestions);

        var prompt = ContentPlanPrompt.Build(
            context.Brand, campaign, competitorInsights, timingSummary, context.Run.ContentPostCount, platforms,
            context.Run.ContentLanguage, context.Input(AiArtifactKind.Strategy), campaign.BriefJson,
            context.Run.ContentTemplateStyle);

        if (context.RepairPrompt)
        {
            prompt += " " + PromptFragments.RepairInstruction;
        }

        var template = templates.For(Kind);
        var startedAt = DateTime.UtcNow;

        var generation = await textGenerationService.GenerateTextAsync(
            prompt, cancellationToken, new AiTextGenerationOptions(template.TaskName, template.JsonMode, template.Temperature));

        var job = AiJobRecorder.RecordText(dbContext, context, AiJobType.ContentGeneration, prompt, generation, startedAt);

        if (!generation.Succeeded)
        {
            return StageResult.Failure(
                AiPipelinePolicy.ClassifyProviderError(generation.ErrorMessage),
                generation.ErrorMessage ?? "The AI provider did not return a response.",
                [job.Id]);
        }

        var payload = AiJsonResponseParser.ExtractJsonPayload(generation.Text);

        if (!ArtifactSchema.IsValid(AiArtifactKind.ContentPlan, payload, out var validationError))
        {
            return StageResult.Failure(
                payload is null ? AiFailureKind.Parse : AiFailureKind.Validation,
                validationError ?? "The AI response could not be read.",
                [job.Id]);
        }

        // A campaign's StartDate can be in the past by the time content is generated (drafted, then
        // approved days later) — falling back to today keeps suggested post times out of the past,
        // which the scheduling step can no longer recover from.
        var now = DateTime.UtcNow;
        var startDate = campaign.StartDate?.ToDateTime(TimeOnly.MinValue);
        var baseDate = startDate is { } sd && sd > now.Date ? sd : now.Date;
        var drafts = ContentPlanParser.Parse(generation.Text!, platforms, baseDate);

        if (drafts.Count == 0)
        {
            return StageResult.Failure(
                AiFailureKind.Validation, "Could not parse the AI-generated campaign posts.", [job.Id]);
        }

        var createdIds = new List<Guid>();

        foreach (var draft in drafts)
        {
            var entity = new ContentItem
            {
                Id = Guid.NewGuid(),
                CampaignId = campaign.Id,
                TenantId = context.TenantId,
                BrandProfileId = context.Brand.Id,
                CreatedBy = context.UserId,
                GenerationMode = GenerationMode.Campaign,
                ContentType = draft.ContentType,
                Platform = draft.Platform,
                Language = context.Run.ContentLanguage,
                Content = draft.Content,
                Hashtags = draft.Hashtags,
                Cta = draft.Cta,
                ImagePrompt = draft.ImagePrompt,
                PipelineStageId = context.Stage.Id,
                Status = ContentStatus.Draft,
                SuggestedPostAt = draft.SuggestedPostAt,
                CreatedAt = now,
                UpdatedAt = now
            };

            dbContext.ContentItems.Add(entity);
            createdIds.Add(entity.Id);
        }

        // Opting out of images means no ContentImage stages at all — no VisualAsset rows, not even
        // placeholders, matching GenerateCampaignContentCommandHandler's behaviour when a caller set
        // IncludeImages to false.
        return context.Run.ContentIncludeImages
            ? StageResult.FannedOut(AiArtifactKind.ContentPlan, payload!, createdIds, [job.Id])
            : StageResult.Success(AiArtifactKind.ContentPlan, payload!, [job.Id]);
    }
}
