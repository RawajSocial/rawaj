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
    IAiTextGenerationService textGenerationService,
    ICoinCostProvider coinCostProvider)
    : IRequestHandler<GenerateOnboardingQuestionsCommand, Result<GenerateOnboardingQuestionsResponse>>
{
    public async Task<Result<GenerateOnboardingQuestionsResponse>> Handle(
        GenerateOnboardingQuestionsCommand request, CancellationToken cancellationToken)
    {
        var tenantId = currentTenantContext.TenantId!.Value;
        var userId = currentUserService.UserId!.Value;
        var role = currentTenantContext.Role!.Value;

        var brand = await dbContext.TenantBrandProfiles
            .FirstOrDefaultAsync(b => b.Id == request.BrandProfileId && b.TenantId == tenantId, cancellationToken);
        if (brand is null)
        {
            return Result<GenerateOnboardingQuestionsResponse>.Failure("Brand profile not found.");
        }

        // This is the pricing sheet's AI Reasoning Conversation feature (the wizard's step-7 chat) -
        // previously wired up but never actually charged.
        var coinCost = await CoinPricingPolicy.GetDiscountedCostAsync(dbContext, tenantId, coinCostProvider.ReasoningConversation, cancellationToken);
        var coinBalance = await CoinPolicy.GetBalanceAsync(dbContext, tenantId, userId, role, cancellationToken);
        if (coinBalance < coinCost)
        {
            return Result<GenerateOnboardingQuestionsResponse>.Failure(
                CoinPolicy.InsufficientCoinsMessage(coinCost, coinBalance, "continue this conversation"));
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

        if (!generation.Succeeded)
        {
            await dbContext.SaveChangesAsync(cancellationToken);
            return Result<GenerateOnboardingQuestionsResponse>.Failure(
                generation.ErrorMessage ?? "Could not generate onboarding questions. Please try again.");
        }

        await CoinPolicy.TrySpendAsync(dbContext, tenantId, userId, role, coinCost, cancellationToken);

        await dbContext.SaveChangesAsync(cancellationToken);

        return Result<GenerateOnboardingQuestionsResponse>.Success(new GenerateOnboardingQuestionsResponse(generation.Text!));
    }
}
