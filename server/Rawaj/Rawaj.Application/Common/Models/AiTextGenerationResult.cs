namespace Rawaj.Application.Common.Models;

public class AiTextGenerationResult
{
    public bool Succeeded { get; }
    public string? Text { get; }
    public int? TokensUsed { get; }
    public string? ErrorMessage { get; }

    private AiTextGenerationResult(bool succeeded, string? text, int? tokensUsed, string? errorMessage)
    {
        Succeeded = succeeded;
        Text = text;
        TokensUsed = tokensUsed;
        ErrorMessage = errorMessage;
    }

    public static AiTextGenerationResult Success(string text, int? tokensUsed) => new(true, text, tokensUsed, null);

    public static AiTextGenerationResult Failure(string errorMessage) => new(false, null, null, errorMessage);
}
