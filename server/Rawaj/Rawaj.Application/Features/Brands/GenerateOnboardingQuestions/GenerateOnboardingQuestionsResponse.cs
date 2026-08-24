namespace Rawaj.Application.Features.Brands.GenerateOnboardingQuestions;

/// <summary>
/// QuestionsJson is the model's raw JSON array response (not parsed server-side - see command
/// doc comment): [{"question":"...","suggestions":["...","...","..."]}, ...].
/// </summary>
public record GenerateOnboardingQuestionsResponse(string QuestionsJson);
