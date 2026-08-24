using MediatR;
using Rawaj.Application.Common.Interfaces;
using Rawaj.Application.Common.Models;
using Rawaj.Application.Features.AiTrial.Common;
using Rawaj.Domain.Entities.AiOperations;
using Rawaj.Domain.Enums;

namespace Rawaj.Application.Features.AiTrial.GenerateTrialImage;

public class GenerateTrialImageCommandHandler(
    IApplicationDbContext dbContext,
    ICurrentUserService currentUserService,
    ICurrentTenantContext currentTenantContext,
    IAiImageGenerationService imageGenerationService,
    IMediaStorageService mediaStorageService)
    : IRequestHandler<GenerateTrialImageCommand, Result<GenerateTrialImageResponse>>
{
    public async Task<Result<GenerateTrialImageResponse>> Handle(
        GenerateTrialImageCommand request, CancellationToken cancellationToken)
    {
        var userId = currentUserService.UserId;
        if (userId is null)
        {
            return Result<GenerateTrialImageResponse>.Failure("Unauthorized.");
        }

        var usedToday = await TrialUsagePolicy.CountTodayAsync(dbContext, userId.Value, cancellationToken);
        if (usedToday >= TrialUsagePolicy.DailyLimit)
        {
            return Result<GenerateTrialImageResponse>.Failure(
                $"You've reached today's free trial limit ({TrialUsagePolicy.DailyLimit}). Subscribe to keep generating images.");
        }

        var startedAt = DateTime.UtcNow;
        var generation = await imageGenerationService.GenerateImageAsync(request.Prompt, cancellationToken);

        var now = DateTime.UtcNow;
        var job = new AiJob
        {
            Id = Guid.NewGuid(),
            // See GenerateTrialContentCommandHandler — trial calls are authenticated but not
            // tenant-scoped, so this is recorded when known and left empty when it genuinely isn't.
            TenantId = currentTenantContext.TenantId ?? Guid.Empty,
            BrandProfileId = null,
            TriggeredBy = userId.Value,
            JobType = AiJobType.ImageGeneration,
            Status = generation.Succeeded ? AiJobStatus.Completed : AiJobStatus.Failed,
            InputParams = System.Text.Json.JsonSerializer.Serialize(new { prompt = request.Prompt }),
            OutputRefType = "trial_image",
            ErrorMessage = generation.ErrorMessage,
            StartedAt = startedAt,
            CompletedAt = now,
            CreatedAt = now
        };

        dbContext.AiJobs.Add(job);
        await dbContext.SaveChangesAsync(cancellationToken);

        if (!generation.Succeeded)
        {
            return Result<GenerateTrialImageResponse>.Failure(
                generation.ErrorMessage ?? "Image generation failed. Please try again.");
        }

        // Trial images are never persisted (no VisualAsset row), so there's no PublicId to store —
        // just return whatever URL the caller can render. Uploaded under a dedicated "trial" folder
        // since these are never linked from an entity and so can never be individually cleaned up.
        string imageUrl;
        if (mediaStorageService.IsConfigured)
        {
            var upload = await mediaStorageService.UploadImageAsync(
                generation.ImageBytes!, generation.ContentType ?? "image/jpeg", "trial-images", cancellationToken);
            imageUrl = upload.Url;
        }
        else
        {
            imageUrl = $"data:{generation.ContentType};base64,{Convert.ToBase64String(generation.ImageBytes!)}";
        }

        var remaining = TrialUsagePolicy.DailyLimit - (usedToday + 1);

        return Result<GenerateTrialImageResponse>.Success(
            new GenerateTrialImageResponse(imageUrl, Math.Max(0, remaining)));
    }
}
