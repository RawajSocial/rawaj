using System.Text.Json;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Rawaj.Application.Common.Interfaces;
using Rawaj.Application.Common.Models;
using Rawaj.Application.Common.Policies;
using Rawaj.Application.Features.AiPipeline.Prompts;
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
        // Not JsonMode: true - Groq's json_object response format can only ever produce a JSON
        // object, never a top-level array, and this prompt's shape is an array of question objects.
        // Validation instead relies entirely on AiJsonResponseParser + ArabicContentPolicy below.
        var promptOptions = AiTextGenerationOptions.Default;
        var startedAt = DateTime.UtcNow;
        var generation = await textGenerationService.GenerateTextAsync(prompt, cancellationToken, promptOptions);
        var questionsJson = generation.Succeeded ? AiJsonResponseParser.ExtractJsonPayload(generation.Text) : null;

        // Unlike every other AI-generated response in this app, this one used to reach the browser
        // with no shape or language validation at all - the model's raw text became the chat
        // bubbles the user reads. One retry, same shape as AiPipelinePolicy.ShouldRepairPrompt: a
        // model that ignores JSON shape or the Arabic instruction twice in a row won't be argued
        // into it on a third paid call.
        if (generation.Succeeded && (questionsJson is null || !ArabicContentPolicy.HasSufficientArabicContent(questionsJson)))
        {
            var repairInstruction = questionsJson is null
                ? PromptFragments.RepairInstruction
                : PromptFragments.LanguageRepairInstruction;

            generation = await textGenerationService.GenerateTextAsync(
                prompt + " " + repairInstruction, cancellationToken, promptOptions);
            questionsJson = generation.Succeeded ? AiJsonResponseParser.ExtractJsonPayload(generation.Text) : null;

            if (questionsJson is not null && !ArabicContentPolicy.HasSufficientArabicContent(questionsJson))
            {
                questionsJson = null;
            }
        }

        var now = DateTime.UtcNow;
        var succeeded = generation.Succeeded && questionsJson is not null;

        var job = new AiJob
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            BrandProfileId = brand.Id,
            TriggeredBy = userId,
            JobType = AiJobType.PlanGeneration,
            Status = succeeded ? AiJobStatus.Completed : AiJobStatus.Failed,
            InputParams = JsonSerializer.Serialize(new { prompt }),
            OutputRefType = "onboarding_questions",
            Tokens = generation.TokensUsed,
            ErrorMessage = generation.ErrorMessage,
            StartedAt = startedAt,
            CompletedAt = now,
            CreatedAt = now
        };
        dbContext.AiJobs.Add(job);

        if (!succeeded)
        {
            await dbContext.SaveChangesAsync(cancellationToken);
            return Result<GenerateOnboardingQuestionsResponse>.Failure(
                generation.ErrorMessage ?? "Could not generate onboarding questions. Please try again.");
        }

        await CoinPolicy.TrySpendAsync(dbContext, tenantId, userId, role, coinCost, cancellationToken, reason: "onboarding_questions");

        await dbContext.SaveChangesAsync(cancellationToken);

        return Result<GenerateOnboardingQuestionsResponse>.Success(new GenerateOnboardingQuestionsResponse(questionsJson!));
    }
}
