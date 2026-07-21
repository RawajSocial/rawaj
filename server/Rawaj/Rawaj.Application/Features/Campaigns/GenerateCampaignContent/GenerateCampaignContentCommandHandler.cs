using System.Text.Json;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Rawaj.Application.Common.Interfaces;
using Rawaj.Application.Common.Models;
using Rawaj.Application.Common.Policies;
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
    IAiImageGenerationService imageGenerationService)
    : IRequestHandler<GenerateCampaignContentCommand, Result<GenerateCampaignContentResponse>>
{
    private const int MaxCompetitorInsights = 5;

    public async Task<Result<GenerateCampaignContentResponse>> Handle(
        GenerateCampaignContentCommand request, CancellationToken cancellationToken)
    {
        var tenantId = currentTenantContext.TenantId!.Value;
        var userId = currentUserService.UserId!.Value;

        var campaign = await dbContext.MarketingCampaigns
            .FirstOrDefaultAsync(c => c.Id == request.CampaignId && c.BrandProfile.TenantId == tenantId, cancellationToken);
        if (campaign is null)
        {
            return Result<GenerateCampaignContentResponse>.Failure("Campaign not found.");
        }

        var brand = await dbContext.TenantBrandProfiles
            .FirstAsync(b => b.Id == campaign.BrandProfileId, cancellationToken);

        var creditsUsage = await AiCreditsPolicy.GetUsageAsync(dbContext, tenantId, cancellationToken);
        if (!creditsUsage.HasCreditsRemaining)
        {
            return Result<GenerateCampaignContentResponse>.Failure(
                $"Your subscription plan allows {creditsUsage.MaxCreditsMonthly} AI credits per month. Upgrade for more.");
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

        var prompt = ContentPromptBuilder.BuildCampaignContentPlanPrompt(
            brand, campaign, competitorInsights, timingSummary, request.PostCount, platforms, request.Language, request.TemplateStyle);

        var generation = await textGenerationService.GenerateTextAsync(prompt, cancellationToken);

        var now = DateTime.UtcNow;

        var job = new AiJob
        {
            Id = Guid.NewGuid(),
            BrandProfileId = brand.Id,
            TriggeredBy = userId,
            JobType = AiJobType.ContentGeneration,
            Status = generation.Succeeded ? AiJobStatus.Completed : AiJobStatus.Failed,
            InputParams = JsonSerializer.Serialize(new { prompt }),
            OutputRefId = generation.Succeeded ? campaign.Id : null,
            OutputRefType = "marketing_campaign_batch",
            Tokens = generation.TokensUsed,
            ErrorMessage = generation.ErrorMessage,
            StartedAt = now,
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

        var baseDate = campaign.StartDate.HasValue ? campaign.StartDate.Value.ToDateTime(TimeOnly.MinValue) : now.Date;
        var createdItems = ParseGeneratedPosts(generation.Text!, platforms, baseDate);

        if (createdItems.Count == 0)
        {
            await dbContext.SaveChangesAsync(cancellationToken);
            return Result<GenerateCampaignContentResponse>.Failure(
                "Could not parse the AI-generated campaign posts. Please try again.");
        }

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

        if (request.IncludeImages)
        {
            var creditsRemaining = creditsUsage.MaxCreditsMonthly - creditsUsage.UsedThisMonth - 1;

            foreach (var entity in contentItemEntities)
            {
                if (creditsRemaining <= 0)
                {
                    imagesSkippedForCredits++;
                    continue;
                }

                var imagePrompt = ContentPromptBuilder.BuildImagePrompt(
                    brand, campaign, entity.ContentType.ToString(), entity.Content, request.TemplateStyle);

                var imageGeneration = await imageGenerationService.GenerateImageAsync(imagePrompt, cancellationToken);
                creditsRemaining--;

                var imageNow = DateTime.UtcNow;
                var visualAssetId = Guid.NewGuid();

                dbContext.AiJobs.Add(new AiJob
                {
                    Id = Guid.NewGuid(),
                    BrandProfileId = brand.Id,
                    TriggeredBy = userId,
                    JobType = AiJobType.ImageGeneration,
                    Status = imageGeneration.Succeeded ? AiJobStatus.Completed : AiJobStatus.Failed,
                    InputParams = JsonSerializer.Serialize(new { prompt = imagePrompt }),
                    OutputRefId = imageGeneration.Succeeded ? visualAssetId : null,
                    OutputRefType = "visual_asset",
                    ErrorMessage = imageGeneration.ErrorMessage,
                    StartedAt = imageNow,
                    CompletedAt = imageNow,
                    CreatedAt = imageNow
                });

                if (!imageGeneration.Succeeded)
                {
                    continue;
                }

                var dataUrl = $"data:{imageGeneration.ContentType};base64,{Convert.ToBase64String(imageGeneration.ImageBytes!)}";

                dbContext.VisualAssets.Add(new VisualAsset
                {
                    Id = visualAssetId,
                    ContentItemId = entity.Id,
                    CampaignId = campaign.Id,
                    BrandProfileId = brand.Id,
                    Type = VisualAssetType.Image,
                    FileUrl = dataUrl,
                    SourceType = VisualAssetSourceType.AiGenerated,
                    AiPrompt = imagePrompt,
                    Format = imageGeneration.ContentType?.Split('/').Last(),
                    IsApproved = false,
                    CreatedAt = imageNow
                });

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

    private record GeneratedPostDraft(
        SocialPlatform Platform, ContentType ContentType, string Content, List<string> Hashtags, string? Cta, DateTime SuggestedPostAt);

    private static List<GeneratedPostDraft> ParseGeneratedPosts(string rawJson, List<SocialPlatform> allowedPlatforms, DateTime baseDate)
    {
        var drafts = new List<GeneratedPostDraft>();

        JsonDocument document;
        try
        {
            document = JsonDocument.Parse(rawJson);
        }
        catch (JsonException)
        {
            return drafts;
        }

        using (document)
        {
            if (!document.RootElement.TryGetProperty("posts", out var postsElement) || postsElement.ValueKind != JsonValueKind.Array)
            {
                return drafts;
            }

            foreach (var post in postsElement.EnumerateArray())
            {
                try
                {
                    if (!post.TryGetProperty("platform", out var platformProp) ||
                        !Enum.TryParse<SocialPlatform>(platformProp.GetString(), true, out var platform) ||
                        !allowedPlatforms.Contains(platform))
                    {
                        continue;
                    }

                    if (!post.TryGetProperty("content", out var contentProp) || string.IsNullOrWhiteSpace(contentProp.GetString()))
                    {
                        continue;
                    }

                    var contentType = ContentType.Post;
                    if (post.TryGetProperty("contentType", out var contentTypeProp))
                    {
                        Enum.TryParse(contentTypeProp.GetString(), true, out contentType);
                    }

                    var dayOffset = 0;
                    if (post.TryGetProperty("dayOffset", out var dayOffsetProp) && dayOffsetProp.TryGetInt32(out var parsedDayOffset))
                    {
                        dayOffset = Math.Clamp(parsedDayOffset, 0, 90);
                    }

                    var hour = 12;
                    if (post.TryGetProperty("hour", out var hourProp) && hourProp.TryGetInt32(out var parsedHour))
                    {
                        hour = Math.Clamp(parsedHour, 0, 23);
                    }

                    var hashtags = new List<string>();
                    if (post.TryGetProperty("hashtags", out var hashtagsProp) && hashtagsProp.ValueKind == JsonValueKind.Array)
                    {
                        hashtags = hashtagsProp.EnumerateArray()
                            .Select(h => h.GetString())
                            .Where(h => !string.IsNullOrWhiteSpace(h))
                            .Select(h => h!)
                            .ToList();
                    }

                    string? cta = null;
                    if (post.TryGetProperty("cta", out var ctaProp) && ctaProp.ValueKind == JsonValueKind.String)
                    {
                        cta = ctaProp.GetString();
                    }

                    drafts.Add(new GeneratedPostDraft(
                        platform, contentType, contentProp.GetString()!, hashtags, cta, baseDate.AddDays(dayOffset).AddHours(hour)));
                }
                catch (JsonException)
                {
                    // Skip malformed individual entries rather than discarding the whole batch.
                }
            }
        }

        return drafts;
    }
}
