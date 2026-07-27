using MediatR;
using Rawaj.Application.Common.Interfaces;
using Rawaj.Application.Common.Models;
using Rawaj.Application.Features.AiTrial.Common;
using Rawaj.Domain.Entities.AiOperations;
using Rawaj.Domain.Enums;

namespace Rawaj.Application.Features.AiTrial.GenerateTrialContent;

public class GenerateTrialContentCommandHandler(
    IApplicationDbContext dbContext,
    ICurrentUserService currentUserService,
    IAiTextGenerationService textGenerationService)
    : IRequestHandler<GenerateTrialContentCommand, Result<GenerateTrialContentResponse>>
{
    public async Task<Result<GenerateTrialContentResponse>> Handle(
        GenerateTrialContentCommand request, CancellationToken cancellationToken)
    {
        var userId = currentUserService.UserId;
        if (userId is null)
        {
            return Result<GenerateTrialContentResponse>.Failure("Unauthorized.");
        }

        var usedToday = await TrialUsagePolicy.CountTodayAsync(dbContext, userId.Value, cancellationToken);
        if (usedToday >= TrialUsagePolicy.DailyLimit)
        {
            return Result<GenerateTrialContentResponse>.Failure(
                $"You've reached today's free trial limit ({TrialUsagePolicy.DailyLimit}). Subscribe to keep generating content.");
        }

        var startedAt = DateTime.UtcNow;
        var generation = await textGenerationService.GenerateTextAsync(request.Prompt, cancellationToken);

        var now = DateTime.UtcNow;
        var job = new AiJob
        {
            Id = Guid.NewGuid(),
            BrandProfileId = null,
            TriggeredBy = userId.Value,
            JobType = AiJobType.ContentGeneration,
            Status = generation.Succeeded ? AiJobStatus.Completed : AiJobStatus.Failed,
            InputParams = System.Text.Json.JsonSerializer.Serialize(new { prompt = request.Prompt }),
            OutputRefType = "trial_content",
            Tokens = generation.TokensUsed,
            ErrorMessage = generation.ErrorMessage,
            StartedAt = startedAt,
            CompletedAt = now,
            CreatedAt = now
        };

        dbContext.AiJobs.Add(job);
        await dbContext.SaveChangesAsync(cancellationToken);

        if (!generation.Succeeded)
        {
            return Result<GenerateTrialContentResponse>.Failure(
                generation.ErrorMessage ?? "Content generation failed. Please try again.");
        }

        var remaining = TrialUsagePolicy.DailyLimit - (usedToday + 1);

        return Result<GenerateTrialContentResponse>.Success(
            new GenerateTrialContentResponse(generation.Text!, Math.Max(0, remaining)));
    }
}
