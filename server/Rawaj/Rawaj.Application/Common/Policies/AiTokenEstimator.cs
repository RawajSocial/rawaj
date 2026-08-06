namespace Rawaj.Application.Common.Policies;

/// <summary>
/// A rough token count for a prompt, used only to reject a request that provably cannot fit before
/// spending a provider call on it.
///
/// <para>Deliberately a heuristic, not a tokenizer. Shipping the real llama tokenizer to answer
/// "is this obviously too big" would be a large dependency for a check whose only job is catching an
/// order-of-magnitude problem. The authoritative answer still comes from the provider — see
/// <see cref="AiPipelinePolicy.ClassifyProviderError"/>, which recognises the rejection and marks it
/// non-retryable. This just gets there without the round-trip.</para>
///
/// <para>The English chars-per-token rule of thumb is badly wrong for Arabic, which is most of what
/// this product sends: Arabic averages closer to one token per one-and-a-half characters, so a
/// single ratio would under-count a Arabic prompt by roughly half and miss exactly the case worth
/// catching. Non-ASCII is therefore weighted separately, and the estimate leans high on purpose —
/// over-estimating costs a clear error message, under-estimating costs a confusing provider one.</para>
/// </summary>
public static class AiTokenEstimator
{
    private const double AsciiCharsPerToken = 4.0;
    private const double NonAsciiCharsPerToken = 1.5;

    public static int Estimate(string? text)
    {
        if (string.IsNullOrEmpty(text))
        {
            return 0;
        }

        var asciiCount = 0;
        var nonAsciiCount = 0;

        foreach (var c in text)
        {
            if (c < 128)
            {
                asciiCount++;
            }
            else
            {
                nonAsciiCount++;
            }
        }

        return (int)Math.Ceiling(asciiCount / AsciiCharsPerToken + nonAsciiCount / NonAsciiCharsPerToken);
    }

    /// <summary>
    /// Whether a prompt plus the completion it reserves can fit the provider's per-request budget.
    /// The reserved completion counts: Groq bills <c>max_tokens</c> against the same allowance as the
    /// prompt, which is why a 11k-token prompt with a 4k completion reservation is rejected against a
    /// 12k limit even though the prompt alone would have fit.
    /// </summary>
    public static bool ExceedsBudget(string prompt, int maxCompletionTokens, int budget, out int estimated)
    {
        estimated = Estimate(prompt) + Math.Max(0, maxCompletionTokens);
        return budget > 0 && estimated > budget;
    }
}
