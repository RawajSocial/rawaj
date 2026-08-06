namespace Rawaj.Application.Common.Models;

/// <summary>
/// Per-call generation settings. Every property is optional and every default reproduces the
/// behaviour callers had before this type existed, so an existing call site that passes nothing
/// behaves exactly as it did.
/// </summary>
/// <param name="TaskName">Selects a per-task model from configuration, falling back to the default
/// model when the task has no entry. Today one model writes both the cheap onboarding questions and
/// the 12,000-coin strategy; this is how that stops being true.</param>
/// <param name="JsonMode">Ask the provider to guarantee syntactically valid JSON. Worth requesting
/// wherever the response is parsed, but not a substitute for the parser: it constrains syntax, not
/// the shape we asked for, and a provider that ignores the flag fails silently.</param>
/// <param name="Temperature">Null leaves the provider default untouched.</param>
/// <param name="Seed">Makes a generation reproducible for the same prompt and model, which is what
/// allows a regression in prompt quality to be investigated rather than guessed at.</param>
public sealed record AiTextGenerationOptions(
    string? TaskName = null,
    bool JsonMode = false,
    double? Temperature = null,
    int? Seed = null)
{
    public static readonly AiTextGenerationOptions Default = new();
}
