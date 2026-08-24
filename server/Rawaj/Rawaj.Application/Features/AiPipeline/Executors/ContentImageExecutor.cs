using Microsoft.EntityFrameworkCore;
using Rawaj.Application.Common.Interfaces;
using Rawaj.Application.Common.Policies;
using Rawaj.Application.Features.AiPipeline.Common;
using Rawaj.Application.Features.AiPipeline.Prompts;
using Rawaj.Domain.Entities.Campaigns;
using Rawaj.Domain.Enums;

namespace Rawaj.Application.Features.AiPipeline.Executors;

/// <summary>
/// Generates the image for one post. One stage row per post, keyed by
/// <c>Stage.TargetRefId = ContentItemId</c> — set by the orchestrator when it fans out
/// <see cref="ContentPlanExecutor"/>'s result — which is what makes a single post's image retryable
/// without regenerating the batch and lets several run in parallel under the provider's concurrency
/// limit.
///
/// <para>Produces no <c>AiArtifact</c>: the output is a <c>VisualAsset</c> row, written directly,
/// matching how the legacy handler never treated a generated image as pipeline "artifact" data.</para>
///
/// <para><b>The placeholder is not this executor's job.</b> <c>ContentImage</c> is
/// <c>IsOptional</c>, and per docs/AI_PIPELINE.md §6 a permanent failure attaches the
/// <c>/text-post.png</c> placeholder <i>before skipping</i> — but only once every retry is
/// exhausted, which is a call only the orchestrator (deciding Skipped vs. a further retry) can make.
/// Attaching a placeholder here on every failed attempt would leave a post with several placeholder
/// rows by the time a genuine retry finally succeeds. This executor reports failure and stops; C15
/// wires the exhausted-retries placeholder.</para>
/// </summary>
public class ContentImageExecutor(
    IApplicationDbContext dbContext,
    IAiImageGenerationService imageGenerationService,
    IMediaStorageService mediaStorageService) : IPipelineStageExecutor
{
    public AiPipelineStageKind Kind => AiPipelineStageKind.ContentImage;

    public async Task<StageResult> ExecuteAsync(StageContext context, CancellationToken cancellationToken)
    {
        if (context.Campaign is null)
        {
            return StageResult.Failure(AiFailureKind.Validation, "This step is missing information it needs and cannot run.");
        }

        var contentItemId = context.Stage.TargetRefId;
        if (contentItemId is null)
        {
            return StageResult.Failure(AiFailureKind.Validation, "This image stage has no post to generate an image for.");
        }

        var contentItem = await dbContext.ContentItems
            .FirstOrDefaultAsync(c => c.Id == contentItemId, cancellationToken);

        if (contentItem is null)
        {
            return StageResult.Failure(AiFailureKind.Validation, "The post this image belongs to no longer exists.");
        }

        // Prefer the model's own English scene description over the post copy. The copy is
        // persuasion text in the post's language, which the image model neither understands nor
        // should be trying to illustrate literally; falling back to it only happens when the content
        // stage didn't return an imagePrompt at all.
        var prompt = VisualPrompt.Build(
            context.Brand, context.Campaign, contentItem.ContentType.ToString(), contentItem.ImagePrompt ?? contentItem.Content);

        var startedAt = DateTime.UtcNow;
        var generation = await imageGenerationService.GenerateImageAsync(prompt, cancellationToken);
        var visualAssetId = Guid.NewGuid();

        var job = AiJobRecorder.RecordImage(
            dbContext, context, prompt, generation, startedAt, generation.Succeeded ? visualAssetId : null);

        if (!generation.Succeeded)
        {
            return StageResult.Failure(
                AiPipelinePolicy.ClassifyProviderError(generation.ErrorMessage),
                generation.ErrorMessage ?? "Image generation failed.",
                [job.Id]);
        }

        var now = DateTime.UtcNow;

        var visualAsset = new VisualAsset
        {
            Id = visualAssetId,
            ContentItemId = contentItem.Id,
            CampaignId = context.Campaign.Id,
            BrandProfileId = context.Brand.Id,
            TenantId = context.TenantId,
            GenerationMode = GenerationMode.Campaign,
            Type = VisualAssetType.Image,
            SourceType = VisualAssetSourceType.AiGenerated,
            AiPrompt = prompt,
            Format = generation.ContentType?.Split('/').Last(),
            IsApproved = false,
            CreatedAt = now
        };

        if (mediaStorageService.IsConfigured)
        {
            // Retried here, separately from the pipeline's own stage-level retry: Cloudflare has
            // already been paid for and has already succeeded by this point, so a transient
            // Cloudinary hiccup should not throw away that result and force a second image
            // generation. Only once these attempts are exhausted does the exception propagate and
            // fall back to the stage-level retry (which does redo the Cloudflare call too — at that
            // point something more than a transient blip is going on).
            MediaUploadResult upload;
            try
            {
                upload = await UploadWithRetryAsync(
                    generation.ImageBytes!, generation.ContentType ?? "image/jpeg", $"visual-assets/{context.TenantId}", cancellationToken);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                return StageResult.Failure(AiFailureKind.Provider, $"Image storage failed: {ex.Message}", [job.Id]);
            }

            visualAsset.FileUrl = upload.Url;
            visualAsset.PublicId = upload.PublicId;
            visualAsset.WidthPx = upload.WidthPx;
            visualAsset.HeightPx = upload.HeightPx;
            visualAsset.FileSizeBytes = upload.FileSizeBytes;
            visualAsset.MimeType = upload.MimeType;
            visualAsset.StorageProvider = "Cloudinary";
            visualAsset.UploadedAt = now;
        }
        else
        {
            visualAsset.FileUrl = $"data:{generation.ContentType};base64,{Convert.ToBase64String(generation.ImageBytes!)}";
        }

        dbContext.VisualAssets.Add(visualAsset);

        return StageResult.Completed([job.Id]);
    }

    /// <summary>Three quick attempts at the Cloudinary upload alone, short fixed delay between —
    /// deliberately not the minutes-long backoff a full stage retry uses, since this only needs to
    /// ride out a brief network blip, not wait out a real outage (a real outage still falls through
    /// to the stage-level retry once these are exhausted).</summary>
    private async Task<MediaUploadResult> UploadWithRetryAsync(
        byte[] imageBytes, string contentType, string folder, CancellationToken cancellationToken)
    {
        const int maxAttempts = 3;
        var delay = TimeSpan.FromSeconds(2);

        for (var attempt = 1; ; attempt++)
        {
            try
            {
                return await mediaStorageService.UploadImageAsync(imageBytes, contentType, folder, cancellationToken);
            }
            catch (Exception) when (attempt < maxAttempts)
            {
                await Task.Delay(delay, cancellationToken);
            }
        }
    }
}
