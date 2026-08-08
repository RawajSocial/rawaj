using Rawaj.Application.Common.Policies;
using Rawaj.Application.Features.Content.Common;
using Rawaj.Domain.Enums;
using Xunit;

namespace Rawaj.Application.Tests.Common;

/// <summary>
/// Locks in the fix for the failure that made campaign content generation impossible on a Groq free
/// tier: Groq returns Arabic as <c>\uXXXX</c> escapes, and an artifact stored that way was pasted
/// verbatim into the next stage's prompt, inflating it roughly 4.7x. A single ContentPlan request
/// ended up larger than the entire per-minute token budget — which no retry can ever get under.
/// </summary>
public class AiPromptTokenBudgetTests
{
    private const string ArabicJson = "{\"executiveSummary\":\"تهدف حملة كولكشن الصيف لشركة عطور الشرق\"}";

    [Fact]
    public void ExtractJsonPayload_ReturnsArabicUnescaped_NotAsUnicodeEscapes()
    {
        var escaped = "{\"executiveSummary\":\"\\u062A\\u0647\\u062F\\u0641 \\u062D\\u0645\\u0644\\u0629\"}";

        var extracted = AiJsonResponseParser.ExtractJsonPayload(escaped);

        Assert.NotNull(extracted);
        Assert.DoesNotContain("\\u", extracted);
        Assert.Contains("تهدف حملة", extracted);
    }

    [Fact]
    public void ExtractJsonPayload_UnescapingIsWhatShrinksThePrompt()
    {
        // The measured shape of the real bug: same JSON, same information, a fraction of the tokens.
        var escaped = AsEscaped(ArabicJson);

        var extracted = AiJsonResponseParser.ExtractJsonPayload(escaped)!;

        Assert.True(escaped.Length > extracted.Length * 2,
            $"expected a large reduction, got {escaped.Length} -> {extracted.Length}");
        Assert.True(AiTokenEstimator.Estimate(extracted) < AiTokenEstimator.Estimate(escaped));
    }

    [Fact]
    public void Normalize_RewritesAlreadyStoredEscapedArtifacts()
    {
        // Artifacts written before the parser normalized still hold escapes; the artifact store runs
        // this on read so those rows stop inflating prompts without being rewritten in the database.
        var normalized = AiJsonResponseParser.Normalize(AsEscaped(ArabicJson));

        Assert.NotNull(normalized);
        Assert.DoesNotContain("\\u", normalized);
        Assert.Contains("عطور الشرق", normalized);
    }

    [Fact]
    public void Normalize_LeavesUnparseableTextAlone()
    {
        // Never silently drop a model's output: passing it through beats returning null to a caller
        // that is about to store or prompt with it.
        Assert.Equal("not json at all", AiJsonResponseParser.Normalize("not json at all"));
        Assert.Null(AiJsonResponseParser.Normalize(null));
    }

    [Fact]
    public void Estimate_CountsArabicHeavierThanAscii()
    {
        // A single chars-per-token ratio would under-count Arabic by about half and miss exactly the
        // prompts this product actually sends.
        Assert.True(AiTokenEstimator.Estimate(new string('ع', 100)) > AiTokenEstimator.Estimate(new string('a', 100)));
    }

    [Fact]
    public void ExceedsBudget_CountsTheReservedCompletion_NotJustThePrompt()
    {
        // Groq bills max_tokens against the same allowance as the prompt, which is why an 11k prompt
        // with a 4k reservation is rejected against a 12k limit though the prompt alone would fit.
        var prompt = new string('a', 4 * 9000);

        Assert.False(AiTokenEstimator.ExceedsBudget(prompt, 0, 12000, out _));
        Assert.True(AiTokenEstimator.ExceedsBudget(prompt, 4000, 12000, out var estimated));
        Assert.True(estimated > 12000);
    }

    [Fact]
    public void ExceedsBudget_IsDisabledWhenNoLimitConfigured()
    {
        // Guessing a limit for an account we can't inspect would reject requests a paid tier accepts.
        Assert.False(AiTokenEstimator.ExceedsBudget(new string('a', 400000), 4000, budget: 0, out _));
    }

    [Theory]
    [InlineData("Request too large for model `llama-3.3-70b-versatile` ... on tokens per minute (TPM): Limit 12000, Requested 15318")]
    [InlineData("Please reduce your message size and try again.")]
    [InlineData("This model's maximum context length is 8192 tokens.")]
    public void ClassifyProviderError_TreatsOversizedRequestsAsPromptTooLarge_NotQuota(string message)
    {
        // Groq sends this as a 429, so it reads like a rate limit. It isn't: waiting never helps and
        // every rotated key rejects it identically.
        Assert.Equal(AiFailureKind.PromptTooLarge, AiPipelinePolicy.ClassifyProviderError(message));
    }

    [Fact]
    public void ClassifyProviderError_StillTreatsRealQuotaExhaustionAsQuota()
    {
        Assert.Equal(AiFailureKind.Quota, AiPipelinePolicy.ClassifyProviderError("You exceeded your current quota."));
        Assert.Equal(AiFailureKind.Quota, AiPipelinePolicy.ClassifyProviderError("Rate limit reached for requests."));
    }

    [Fact]
    public void ResolveFailure_DoesNotRetryAnOversizedPrompt()
    {
        var stage = new Domain.Entities.AiOperations.AiPipelineStage
        {
            Kind = AiPipelineStageKind.ContentPlan,
            Attempts = 0,
        };

        var outcome = AiPipelinePolicy.ResolveFailure(stage, AiFailureKind.PromptTooLarge, DateTime.UtcNow);

        // The next attempt would send byte-for-byte the same prompt to the same limit.
        Assert.Equal(AiPipelineStageStatus.Failed, outcome.Status);
        Assert.Null(outcome.NextAttemptAt);
    }

    [Fact]
    public void ResolveFailure_StillRetriesATransientProviderError()
    {
        var stage = new Domain.Entities.AiOperations.AiPipelineStage
        {
            Kind = AiPipelineStageKind.ContentPlan,
            Attempts = 0,
        };

        var outcome = AiPipelinePolicy.ResolveFailure(stage, AiFailureKind.Provider, DateTime.UtcNow);

        Assert.Equal(AiPipelineStageStatus.Pending, outcome.Status);
        Assert.NotNull(outcome.NextAttemptAt);
    }

    /// <summary>Renders JSON the way Groq hands it back — every non-ASCII character as \uXXXX.</summary>
    private static string AsEscaped(string json)
    {
        var builder = new System.Text.StringBuilder();
        foreach (var c in json)
        {
            builder.Append(c < 128 ? c.ToString() : $"\\u{(int)c:x4}");
        }
        return builder.ToString();
    }
}
