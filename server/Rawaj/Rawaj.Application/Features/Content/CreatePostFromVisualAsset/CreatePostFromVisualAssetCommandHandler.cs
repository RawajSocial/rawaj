using MediatR;
using Microsoft.EntityFrameworkCore;
using Rawaj.Application.Common.Interfaces;
using Rawaj.Application.Common.Models;
using Rawaj.Domain.Entities.Campaigns;
using Rawaj.Domain.Enums;

namespace Rawaj.Application.Features.Content.CreatePostFromVisualAsset;

public class CreatePostFromVisualAssetCommandHandler(
    IApplicationDbContext dbContext,
    ICurrentUserService currentUserService,
    ICurrentTenantContext currentTenantContext)
    : IRequestHandler<CreatePostFromVisualAssetCommand, Result<CreatePostFromVisualAssetResponse>>
{
    public async Task<Result<CreatePostFromVisualAssetResponse>> Handle(
        CreatePostFromVisualAssetCommand request, CancellationToken cancellationToken)
    {
        var tenantId = currentTenantContext.TenantId!.Value;
        var userId = currentUserService.UserId!.Value;

        var visualAsset = await dbContext.VisualAssets
            .FirstOrDefaultAsync(v => v.Id == request.VisualAssetId && v.TenantId == tenantId, cancellationToken);
        if (visualAsset is null)
        {
            return Result<CreatePostFromVisualAssetResponse>.Failure("Visual asset not found.");
        }

        if (visualAsset.ContentItemId is not null)
        {
            return Result<CreatePostFromVisualAssetResponse>.Failure("This image is already linked to a post.");
        }

        var now = DateTime.UtcNow;
        var contentItem = new ContentItem
        {
            Id = Guid.NewGuid(),
            CampaignId = visualAsset.CampaignId,
            TenantId = tenantId,
            BrandProfileId = visualAsset.BrandProfileId,
            GenerationMode = visualAsset.GenerationMode,
            CreatedBy = userId,
            ContentType = ContentType.Post,
            Platform = request.Platform,
            Language = request.Language,
            // The image's own generation prompt is the closest real description of what this post
            // is — there's no separate caption collected anywhere in the image-only generation flow.
            Content = string.IsNullOrWhiteSpace(visualAsset.AiPrompt) ? "منشور صورة" : visualAsset.AiPrompt,
            Status = ContentStatus.Approved,
            ReviewedBy = userId,
            ReviewedAt = now,
            CreatedAt = now,
            UpdatedAt = now,
        };

        dbContext.ContentItems.Add(contentItem);
        visualAsset.ContentItemId = contentItem.Id;

        await dbContext.SaveChangesAsync(cancellationToken);

        return Result<CreatePostFromVisualAssetResponse>.Success(new CreatePostFromVisualAssetResponse(contentItem.Id));
    }
}
