using System.Text.Json;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Rawaj.Application.Common.Interfaces;
using Rawaj.Application.Common.Models;
using Rawaj.Application.Common.Policies;
using Rawaj.Application.Features.Content.Common;
using Rawaj.Domain.Entities.AiOperations;
using Rawaj.Domain.Enums;

namespace Rawaj.Application.Features.Brands.GenerateOnboardingQuestions;

public class GenerateOnboardingQuestionsCommandHandler(
    IApplicationDbContext dbContext,
    ICurrentUserService currentUserService,
    ICurrentTenantContext currentTenantContext,
    IAiTextGenerationService textGenerationService)
    : IRequestHandler<GenerateOnboardingQuestionsCommand, Result<GenerateOnboardingQuestionsResponse>>
{
    public async Task<Result<GenerateOnboardingQuestionsResponse>> Handle(
        GenerateOnboardingQuestionsCommand request, CancellationToken cancellationToken)
    {
        var tenantId = currentTenantContext.TenantId!.Value;
        var userId = currentUserService.UserId!.Value;

        var brand = await dbContext.TenantBrandProfiles
            .FirstOrDefaultAsync(b => b.Id == request.BrandProfileId && b.TenantId == tenantId, cancellationToken);
        if (brand is null)
        {
            return Result<GenerateOnboardingQuestionsResponse>.Failure("Brand profile not found.");
        }

        var creditsUsage = await AiCreditsPolicy.GetUsageAsync(dbContext, tenantId, cancellationToken);
        if (!creditsUsage.HasCreditsRemaining)
        {
            return Result<GenerateOnboardingQuestionsResponse>.Failure(
                $"Your subscription plan allows {creditsUsage.MaxCreditsMonthly} AI credits per month. Upgrade for more.");
        }

        var prompt = ContentPromptBuilder.BuildOnboardingQuestionsPrompt(brand, request.OnboardingContextJson);
        var generation = await textGenerationService.GenerateTextAsync(prompt, cancellationToken);

        var now = DateTime.UtcNow;

        var job = new AiJob
        {
            Id = Guid.NewGuid(),
            BrandProfileId = brand.Id,
            TriggeredBy = userId,
            JobType = AiJobType.PlanGeneration,
            Status = generation.Succeeded ? AiJobStatus.Completed : AiJobStatus.Failed,
            InputParams = JsonSerializer.Serialize(new { prompt }),
            OutputRefType = "onboarding_questions",
            Tokens = generation.TokensUsed,
            ErrorMessage = generation.ErrorMessage,
            StartedAt = now,
            CompletedAt = now,
            CreatedAt = now
        };
        dbContext.AiJobs.Add(job);

        await dbContext.SaveChangesAsync(cancellationToken);

        if (!generation.Succeeded)
        {
            return Result<GenerateOnboardingQuestionsResponse>.Failure(
                generation.ErrorMessage ?? "Could not generate onboarding questions. Please try again.");
        }

        return Result<GenerateOnboardingQuestionsResponse>.Success(new GenerateOnboardingQuestionsResponse(generation.Text!));
    }
}
