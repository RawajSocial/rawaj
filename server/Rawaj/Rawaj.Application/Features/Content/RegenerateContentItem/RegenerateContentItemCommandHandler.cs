using System.Text.Json;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Rawaj.Application.Common.Interfaces;
using Rawaj.Application.Common.Models;
using Rawaj.Application.Common.Policies;
using Rawaj.Application.Features.Content.Common;
using Rawaj.Domain.Entities.AiOperations;
using Rawaj.Domain.Entities.Campaigns;
using Rawaj.Domain.Enums;

namespace Rawaj.Application.Features.Content.RegenerateContentItem;

/// <summary>
/// Remakes a content item's copy AND its photo together from reviewer feedback, keeping a full
/// history in content_revisions (Previous/Current pair per revision) rather than overwriting
/// silently. This is a genuine "redo the whole post" — not just a caption tweak — so the new copy
/// and the new photo stay paired the same way they were at original generation, instead of the old
/// photo silently going stale under a caption that's since moved on to a different idea.
/// </summary>
public class RegenerateContentItemCommandHandler(
    IApplicationDbContext dbContext,
    ICurrentUserService currentUserService,
    ICurrentTenantContext currentTenantContext,
    IAiTextGenerationService textGenerationService,
    IAiImageGenerationService imageGenerationService,
    IMediaStorageService mediaStorageService,
    ICoinCostProvider coinCostProvider)
    : IRequestHandler<RegenerateContentItemCommand, Result<RegenerateContentItemResponse>>
{
    public async Task<Result<RegenerateContentItemResponse>> Handle(RegenerateContentItemCommand request, CancellationToken cancellationToken)
    {
        var tenantId = currentTenantContext.TenantId!.Value;
        var userId = currentUserService.UserId!.Value;
        var role = currentTenantContext.Role!.Value;

        var contentItem = await dbContext.ContentItems
            .FirstOrDefaultAsync(c => c.Id == request.ContentItemId && c.TenantId == tenantId, cancellationToken);
        if (contentItem is null)
        {
            return Result<RegenerateContentItemResponse>.Failure("Content item not found.");
        }

        if (contentItem.Status == ContentStatus.Published)
        {
            return Result<RegenerateContentItemResponse>.Failure("Published content cannot be regenerated.");
        }

        var campaign = contentItem.CampaignId.HasValue
            ? await dbContext.MarketingCampaigns.FirstOrDefaultAsync(c => c.Id == contentItem.CampaignId, cancellationToken)
            : null;
        var brand = await dbContext.TenantBrandProfiles
            .FirstAsync(b => b.Id == contentItem.BrandProfileId, cancellationToken);

        var textCoinCost = await CoinPricingPolicy.GetDiscountedCostAsync(dbContext, tenantId, coinCostProvider.ContentGeneration, cancellationToken);
        var imageCoinCost = await CoinPricingPolicy.GetDiscountedCostAsync(dbContext, tenantId, coinCostProvider.VisualGeneration, cancellationToken);
        var totalCoinCost = textCoinCost + imageCoinCost;
        var coinBalance = await CoinPolicy.GetBalanceAsync(dbContext, tenantId, userId, role, cancellationToken);
        if (coinBalance < totalCoinCost)
        {
            return Result<RegenerateContentItemResponse>.Failure(
                CoinPolicy.InsufficientCoinsMessage(totalCoinCost, coinBalance, "remake this post"));
        }

        var prompt = ContentPromptBuilder.BuildFullRevisionPrompt(brand, campaign, contentItem, request.Feedback);
        var startedAt = DateTime.UtcNow;
        var generation = await textGenerationService.GenerateTextAsync(prompt, cancellationToken);

        var now = DateTime.UtcNow;

        var textJob = new AiJob
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            BrandProfileId = brand.Id,
            TriggeredBy = userId,
            JobType = AiJobType.ContentGeneration,
            Status = generation.Succeeded ? AiJobStatus.Completed : AiJobStatus.Failed,
            InputParams = JsonSerializer.Serialize(new { prompt }),
            OutputRefId = generation.Succeeded ? contentItem.Id : null,
            OutputRefType = "content_item",
            Tokens = generation.TokensUsed,
            ErrorMessage = generation.ErrorMessage,
            StartedAt = startedAt,
            CompletedAt = now,
            CreatedAt = now
        };
        dbContext.AiJobs.Add(textJob);

        if (!generation.Succeeded)
        {
            await dbContext.SaveChangesAsync(cancellationToken);
            return Result<RegenerateContentItemResponse>.Failure(
                generation.ErrorMessage ?? "Content regeneration failed. Please try again.");
        }

        var payload = AiJsonResponseParser.ExtractJsonPayload(generation.Text);
        if (payload is null)
        {
            await dbContext.SaveChangesAsync(cancellationToken);
            return Result<RegenerateContentItemResponse>.Failure("Could not read the AI's response. Please try again.");
        }

        string newContent;
        var newHashtags = new List<string>();
        string? newCta = null;
        string? newImagePrompt = null;
        try
        {
            using var document = JsonDocument.Parse(payload);
            var root = document.RootElement;

            if (!root.TryGetProperty("content", out var contentProp) || string.IsNullOrWhiteSpace(contentProp.GetString()))
            {
                await dbContext.SaveChangesAsync(cancellationToken);
                return Result<RegenerateContentItemResponse>.Failure("Could not read the AI's response. Please try again.");
            }
            newContent = contentProp.GetString()!;

            if (root.TryGetProperty("hashtags", out var hashtagsProp) && hashtagsProp.ValueKind == JsonValueKind.Array)
            {
                newHashtags = hashtagsProp.EnumerateArray()
                    .Select(h => h.GetString())
                    .Where(h => !string.IsNullOrWhiteSpace(h))
                    .Select(h => h!)
                    .ToList();
            }

            if (root.TryGetProperty("cta", out var ctaProp) && ctaProp.ValueKind == JsonValueKind.String)
            {
                newCta = ctaProp.GetString();
            }

            if (root.TryGetProperty("imagePrompt", out var imagePromptProp) && imagePromptProp.ValueKind == JsonValueKind.String)
            {
                var value = imagePromptProp.GetString();
                newImagePrompt = string.IsNullOrWhiteSpace(value) ? null : value;
            }
        }
        catch (JsonException)
        {
            await dbContext.SaveChangesAsync(cancellationToken);
            return Result<RegenerateContentItemResponse>.Failure("Could not read the AI's response. Please try again.");
        }

        var revisionNumber = await dbContext.ContentRevisions
            .Where(r => r.ContentItemId == contentItem.Id)
            .CountAsync(cancellationToken) + 1;

        dbContext.ContentRevisions.Add(new ContentRevision
        {
            Id = Guid.NewGuid(),
            ContentItemId = contentItem.Id,
            RevisedBy = userId,
            RevisionPrompt = request.Feedback,
            Previous = contentItem.Content,
            Current = newContent,
            RevisionNumber = revisionNumber,
            CreatedAt = now
        });

        contentItem.Content = newContent;
        contentItem.Hashtags = newHashtags;
        contentItem.Cta = newCta;
        if (newImagePrompt is not null)
        {
            contentItem.ImagePrompt = newImagePrompt;
        }
        contentItem.Status = ContentStatus.Draft;
        contentItem.ReviewedBy = null;
        contentItem.ReviewedAt = null;
        contentItem.UpdatedAt = now;

        // Same preference as ContentImageExecutor: the model's own English scene description over
        // the (Arabic, persuasion-not-description) post copy, falling back to the copy only if the
        // model didn't return one.
        var imagePrompt = ContentPromptBuilder.BuildImagePrompt(
            brand, campaign, contentItem.ContentType.ToString(), newImagePrompt ?? newContent);

        var imageStartedAt = DateTime.UtcNow;
        var imageGeneration = await imageGenerationService.GenerateImageAsync(imagePrompt, cancellationToken);
        var visualAssetId = Guid.NewGuid();
        var imageNow = DateTime.UtcNow;

        var imageJob = new AiJob
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            BrandProfileId = brand.Id,
            TriggeredBy = userId,
            JobType = AiJobType.ImageGeneration,
            Status = imageGeneration.Succeeded ? AiJobStatus.Completed : AiJobStatus.Failed,
            InputParams = JsonSerializer.Serialize(new { prompt = imagePrompt }),
            OutputRefId = imageGeneration.Succeeded ? visualAssetId : null,
            OutputRefType = "visual_asset",
            ErrorMessage = imageGeneration.ErrorMessage,
            StartedAt = imageStartedAt,
            CompletedAt = imageNow,
            CreatedAt = imageNow
        };
        dbContext.AiJobs.Add(imageJob);

        if (!imageGeneration.Succeeded)
        {
            // The copy already changed and is worth keeping — only the text half of the cost is
            // charged, and the existing photo is left in place until the user retries the image
            // on its own (the per-item "retry image" action already handles that).
            await CoinPolicy.TrySpendAsync(dbContext, tenantId, userId, role, textCoinCost, cancellationToken, reason: "content_regeneration");
            await dbContext.SaveChangesAsync(cancellationToken);
            return Result<RegenerateContentItemResponse>.Failure(
                $"تم تحديث النص، لكن تعذّر توليد صورة جديدة له: {imageGeneration.ErrorMessage}. يمكنك إعادة توليد الصورة فقط لاحقًا.");
        }

        var visualAsset = new VisualAsset
        {
            Id = visualAssetId,
            ContentItemId = contentItem.Id,
            CampaignId = campaign?.Id,
            BrandProfileId = brand.Id,
            TenantId = tenantId,
            GenerationMode = contentItem.GenerationMode,
            Type = VisualAssetType.Image,
            SourceType = VisualAssetSourceType.AiGenerated,
            AiPrompt = imagePrompt,
            Format = imageGeneration.ContentType?.Split('/').Last(),
            IsApproved = false,
            CreatedAt = imageNow
        };

        if (mediaStorageService.IsConfigured)
        {
            var upload = await mediaStorageService.UploadImageAsync(
                imageGeneration.ImageBytes!, imageGeneration.ContentType ?? "image/jpeg", $"visual-assets/{tenantId}", cancellationToken);
            visualAsset.FileUrl = upload.Url;
            visualAsset.PublicId = upload.PublicId;
            visualAsset.WidthPx = upload.WidthPx;
            visualAsset.HeightPx = upload.HeightPx;
            visualAsset.FileSizeBytes = upload.FileSizeBytes;
            visualAsset.MimeType = upload.MimeType;
            visualAsset.StorageProvider = "Cloudinary";
            visualAsset.UploadedAt = imageNow;
        }
        else
        {
            visualAsset.FileUrl = $"data:{imageGeneration.ContentType};base64,{Convert.ToBase64String(imageGeneration.ImageBytes!)}";
        }

        dbContext.VisualAssets.Add(visualAsset);

        await CoinPolicy.TrySpendAsync(dbContext, tenantId, userId, role, totalCoinCost, cancellationToken, reason: "content_full_regeneration");

        var contentSubject = campaign is not null ? $"for \"{campaign.Name}\"" : $"for {brand.Name}";
        NotificationPublisher.Notify(
            dbContext,
            userId,
            brand.Id,
            NotificationType.Info,
            NotificationCategory.ReviewNeeded,
            "Revised content ready for review",
            $"Revision #{revisionNumber} of your {contentItem.ContentType} {contentSubject} is ready for review.",
            contentItem.Id,
            "content_item");

        await dbContext.SaveChangesAsync(cancellationToken);

        return Result<RegenerateContentItemResponse>.Success(
            new RegenerateContentItemResponse(contentItem.Id, contentItem.Content, contentItem.Status, revisionNumber));
    }
}
