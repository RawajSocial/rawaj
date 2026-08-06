using System.Text.Json;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Rawaj.Application.Common.Interfaces;
using Rawaj.Application.Common.Models;
using Rawaj.Application.Common.Policies;
using Rawaj.Application.Features.AiPipeline.Common;
using Rawaj.Application.Features.Content.Common;
using Rawaj.Application.Features.Scheduling.Common;
using Rawaj.Domain.Entities.AiOperations;
using Rawaj.Domain.Entities.Campaigns;
using Rawaj.Domain.Enums;

namespace Rawaj.Application.Features.Campaigns.GenerateCampaignContent;

public class GenerateCampaignContentCommandHandler(
    IApplicationDbContext dbContext,
    ICurrentUserService currentUserService,
    ICurrentTenantContext currentTenantContext,
    IAiTextGenerationService textGenerationService,
    IAiImageGenerationService imageGenerationService,
    IMediaStorageService mediaStorageService,
    ICoinCostProvider coinCostProvider)
    : IRequestHandler<GenerateCampaignContentCommand, Result<GenerateCampaignContentResponse>>
{
    private const int MaxCompetitorInsights = 5;

    public async Task<Result<GenerateCampaignContentResponse>> Handle(
        GenerateCampaignContentCommand request, CancellationToken cancellationToken)
    {
        var tenantId = currentTenantContext.TenantId!.Value;
        var userId = currentUserService.UserId!.Value;
        var role = currentTenantContext.Role!.Value;

        var campaign = await dbContext.MarketingCampaigns
            .FirstOrDefaultAsync(c => c.Id == request.CampaignId && c.BrandProfile.TenantId == tenantId, cancellationToken);
        if (campaign is null)
        {
            return Result<GenerateCampaignContentResponse>.Failure("Campaign not found.");
        }

        if (campaign.PlanApprovedAt is null)
        {
            return Result<GenerateCampaignContentResponse>.Failure("Approve the campaign strategy before generating content.");
        }

        var brand = await dbContext.TenantBrandProfiles
            .FirstAsync(b => b.Id == campaign.BrandProfileId, cancellationToken);

        var creditsUsage = await AiCreditsPolicy.GetUsageAsync(dbContext, tenantId, cancellationToken);
        if (!creditsUsage.HasCreditsRemaining)
        {
            return Result<GenerateCampaignContentResponse>.Failure(
                $"Your subscription plan allows {creditsUsage.MaxCreditsMonthly} AI credits per month. Upgrade for more.");
        }

        // One flat charge for the whole batch, regardless of how many posts/images come out of
        // it — the per-post cost accounting that GenerateContentItem/GenerateVisualAsset do would
        // be unpredictable here up front, since the model decides how many posts to produce.
        var coinCost = await CoinPricingPolicy.GetDiscountedCostAsync(dbContext, tenantId, coinCostProvider.CampaignContentGeneration, cancellationToken);
        var coinBalance = await CoinPolicy.GetBalanceAsync(dbContext, tenantId, userId, role, cancellationToken);
        if (coinBalance < coinCost)
        {
            return Result<GenerateCampaignContentResponse>.Failure(
                CoinPolicy.InsufficientCoinsMessage(coinCost, coinBalance, "generate campaign content"));
        }

        // Prefer the brand's actually-connected accounts over the campaign's TargetPlatforms -
        // that field is set once at campaign creation (often defaulted, e.g. by the onboarding
        // wizard) and easily goes stale once accounts are connected/disconnected later. Content
        // can only ever be scheduled to a connected account anyway, so generating for connected
        // platforms first keeps the batch immediately actionable.
        var platforms = await dbContext.SocialAccounts
            .Where(s => s.BrandProfileId == brand.Id && s.IsActive)
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
            .Where(d => d.BrandProfileId == brand.Id && d.CompetitorsData != null)
            .OrderByDescending(d => d.CreatedAt)
            .Take(MaxCompetitorInsights)
            .Select(d => d.CompetitorsData!)
            .ToListAsync(cancellationToken);

        var timingSuggestions = await PostingTimeIntelligence.GetSuggestionsAsync(dbContext, brand.Id, platforms, cancellationToken);
        var timingSummary = PostingTimeIntelligence.BuildSummary(timingSuggestions);

        // The approved strategy and the onboarding brief are what these posts are supposed to
        // execute. This handler already refuses to run until PlanApprovedAt is set, but it used to
        // then generate from the brand's identity fields and the campaign objective alone — the
        // strategy the user paid ~12,000 coins for, reviewed and approved never reached the model,
        // which made the whole approve-then-generate flow decorative.
        var prompt = ContentPromptBuilder.BuildCampaignContentPlanPrompt(
            brand, campaign, competitorInsights, timingSummary, request.PostCount, platforms, request.Language,
            campaign.AiPlanJson, campaign.BriefJson, request.TemplateStyle);

        var startedAt = DateTime.UtcNow;
        var generation = await textGenerationService.GenerateTextAsync(prompt, cancellationToken);

        var now = DateTime.UtcNow;

        var job = new AiJob
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            BrandProfileId = brand.Id,
            TriggeredBy = userId,
            JobType = AiJobType.ContentGeneration,
            Status = generation.Succeeded ? AiJobStatus.Completed : AiJobStatus.Failed,
            InputParams = JsonSerializer.Serialize(new { prompt }),
            OutputRefId = generation.Succeeded ? campaign.Id : null,
            OutputRefType = "marketing_campaign_batch",
            Tokens = generation.TokensUsed,
            ErrorMessage = generation.ErrorMessage,
            StartedAt = startedAt,
            CompletedAt = now,
            CreatedAt = now
        };
        dbContext.AiJobs.Add(job);

        if (!generation.Succeeded)
        {
            await dbContext.SaveChangesAsync(cancellationToken);
            return Result<GenerateCampaignContentResponse>.Failure(
                generation.ErrorMessage ?? "Campaign content generation failed. Please try again.");
        }

        // A campaign's StartDate can be in the past by the time content is generated (drafted,
        // then approved days later) - falling back to today keeps suggested post times from
        // being stamped into the past, which the scheduling step can no longer recover from.
        var startDate = campaign.StartDate?.ToDateTime(TimeOnly.MinValue);
        var baseDate = startDate is { } sd && sd > now.Date ? sd : now.Date;
        var createdItems = ContentPlanParser.Parse(generation.Text!, platforms, baseDate);

        if (createdItems.Count == 0)
        {
            await dbContext.SaveChangesAsync(cancellationToken);
            return Result<GenerateCampaignContentResponse>.Failure(
                "Could not parse the AI-generated campaign posts. Please try again.");
        }

        await CoinPolicy.TrySpendAsync(dbContext, tenantId, userId, role, coinCost, cancellationToken, reason: "campaign_content_generation");

        var contentItemEntities = new List<ContentItem>();
        foreach (var draft in createdItems)
        {
            var entity = new ContentItem
            {
                Id = Guid.NewGuid(),
                CampaignId = campaign.Id,
                TenantId = tenantId,
                BrandProfileId = brand.Id,
                CreatedBy = userId,
                ContentType = draft.ContentType,
                Platform = draft.Platform,
                Language = request.Language,
                Content = draft.Content,
                Hashtags = draft.Hashtags,
                Cta = draft.Cta,
                ImagePrompt = draft.ImagePrompt,
                AiPromptUsed = prompt,
                Status = ContentStatus.Draft,
                SuggestedPostAt = draft.SuggestedPostAt,
                CreatedAt = now,
                UpdatedAt = now
            };
            contentItemEntities.Add(entity);
            dbContext.ContentItems.Add(entity);
        }

        var imagesGenerated = 0;
        var imagesSkippedForCredits = 0;

        // A ContentItem must never end up with no image at all — when the real image model can't
        // produce one (monthly AI credits exhausted mid-batch, or the model itself fails/runs out
        // of its own quota), attach this static placeholder instead of leaving ImageUrl null. The
        // user can retry for a real image later (see VisualAssetsController's generate endpoint,
        // reused per-item by the content-review page for exactly that).
        VisualAsset CreatePlaceholderVisualAsset(ContentItem entity) => new()
        {
            Id = Guid.NewGuid(),
            ContentItemId = entity.Id,
            CampaignId = campaign.Id,
            BrandProfileId = brand.Id,
            TenantId = tenantId,
            GenerationMode = GenerationMode.Campaign,
            Type = VisualAssetType.Image,
            SourceType = VisualAssetSourceType.Placeholder,
            FileUrl = "/text-post.png",
            IsApproved = false,
            CreatedAt = DateTime.UtcNow
        };

        if (request.IncludeImages)
        {
            var creditsRemaining = creditsUsage.MaxCreditsMonthly - creditsUsage.UsedThisMonth - 1;

            foreach (var entity in contentItemEntities)
            {
                if (creditsRemaining <= 0)
                {
                    imagesSkippedForCredits++;
                    dbContext.VisualAssets.Add(CreatePlaceholderVisualAsset(entity));
                    continue;
                }

                // Prefer the model's own English scene description over the post copy. The copy is
                // Arabic persuasion text ("احصل على خصم ٥٠٪"), which FLUX neither understands nor
                // should be trying to illustrate literally; falling back to it only happens when the
                // model didn't return an imagePrompt at all.
                var imagePrompt = ContentPromptBuilder.BuildImagePrompt(
                    brand, campaign, entity.ContentType.ToString(), entity.ImagePrompt ?? entity.Content, request.TemplateStyle);

                var imageStartedAt = DateTime.UtcNow;
                var imageGeneration = await imageGenerationService.GenerateImageAsync(imagePrompt, cancellationToken);
                creditsRemaining--;

                var imageNow = DateTime.UtcNow;
                var visualAssetId = Guid.NewGuid();

                dbContext.AiJobs.Add(new AiJob
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
                });

                if (!imageGeneration.Succeeded)
                {
                    dbContext.VisualAssets.Add(CreatePlaceholderVisualAsset(entity));
                    continue;
                }

                var visualAsset = new VisualAsset
                {
                    Id = visualAssetId,
                    ContentItemId = entity.Id,
                    CampaignId = campaign.Id,
                    BrandProfileId = brand.Id,
                    TenantId = tenantId,
                    GenerationMode = GenerationMode.Campaign,
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

                imagesGenerated++;
            }
        }

        NotificationPublisher.Notify(
            dbContext,
            userId,
            brand.Id,
            NotificationType.Info,
            NotificationCategory.ReviewNeeded,
            "Campaign drafts ready for review",
            $"{createdItems.Count} new draft post(s) for \"{campaign.Name}\" are ready for your review.",
            campaign.Id,
            "marketing_campaign");

        await dbContext.SaveChangesAsync(cancellationToken);

        return Result<GenerateCampaignContentResponse>.Success(
            new GenerateCampaignContentResponse(campaign.Id, createdItems.Count, imagesGenerated, imagesSkippedForCredits));
    }

}
