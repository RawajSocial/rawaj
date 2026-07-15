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
    IAiImageGenerationService imageGenerationService)
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

        var generation = await imageGenerationService.GenerateImageAsync(request.Prompt, cancellationToken);

        var now = DateTime.UtcNow;
        var job = new AiJob
        {
            Id = Guid.NewGuid(),
            BrandProfileId = null,
            TriggeredBy = userId.Value,
            JobType = AiJobType.ImageGeneration,
            Status = generation.Succeeded ? AiJobStatus.Completed : AiJobStatus.Failed,
            InputParams = System.Text.Json.JsonSerializer.Serialize(new { prompt = request.Prompt }),
            OutputRefType = "trial_image",
            ErrorMessage = generation.ErrorMessage,
            StartedAt = now,
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

        var dataUrl = $"data:{generation.ContentType};base64,{Convert.ToBase64String(generation.ImageBytes!)}";
        var remaining = TrialUsagePolicy.DailyLimit - (usedToday + 1);

        return Result<GenerateTrialImageResponse>.Success(
            new GenerateTrialImageResponse(dataUrl, Math.Max(0, remaining)));
    }
}
