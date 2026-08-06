namespace Rawaj.Application.Common.Models;

public class AiTextGenerationResult
{
    public bool Succeeded { get; }
    public string? Text { get; }
    public int? TokensUsed { get; }
    public string? ErrorMessage { get; }

    /// <summary>The model that actually served the call. Recorded on the AiJob row so "which model
    /// produced this" is answerable once more than one is in use — and so <c>AiJob.Cost</c> can
    /// finally be computed from a per-model rate table instead of staying permanently null.</summary>
    public string? Model { get; }

    /// <summary>Wall-clock duration of the provider call, including any key-rotation retries, since
    /// that is the latency the user actually waited.</summary>
    public int? LatencyMs { get; }

    private AiTextGenerationResult(
        bool succeeded, string? text, int? tokensUsed, string? errorMessage, string? model, int? latencyMs)
    {
        Succeeded = succeeded;
        Text = text;
        TokensUsed = tokensUsed;
        ErrorMessage = errorMessage;
        Model = model;
        LatencyMs = latencyMs;
    }

    public static AiTextGenerationResult Success(
        string text, int? tokensUsed, string? model = null, int? latencyMs = null) =>
        new(true, text, tokensUsed, null, model, latencyMs);

    public static AiTextGenerationResult Failure(
        string errorMessage, string? model = null, int? latencyMs = null) =>
        new(false, null, null, errorMessage, model, latencyMs);
}
