using System.Security.Cryptography;
using System.Text;

namespace Rawaj.Application.Common.Services;

/// <summary>
/// Computes a stage's <c>InputHash</c>: a fingerprint of everything that determines its output.
///
/// <para>It answers two questions. "Has this already been done?" — a completed stage whose recomputed
/// hash still matches is returned as-is, with no model call and no charge, which is what makes
/// re-invoking a single stage safe. And "is the cached brand analysis still valid?" — a mismatch
/// against the brand's current identity means it is stale.</para>
///
/// <para>The prompt template version is part of the input on purpose. Without it, shipping a changed
/// prompt would keep serving artifacts produced by a prompt that no longer exists.</para>
/// </summary>
public static class PipelineInputHash
{
    /// <summary>
    /// Bump when a prompt change should invalidate previously cached artifacts. This is a blunt,
    /// global lever by design: a per-stage version would be more precise and far easier to forget to
    /// bump, and the cost of over-invalidating is one recomputation.
    /// </summary>
    public const int PromptTemplateVersion = 1;

    public static string Compute(params string?[] parts) => Compute((IEnumerable<string?>)parts);

    public static string Compute(IEnumerable<string?> parts)
    {
        var builder = new StringBuilder();
        builder.Append("v").Append(PromptTemplateVersion);

        foreach (var part in parts)
        {
            // A separator that cannot appear in the parts themselves, so ("ab", "c") and ("a", "bc")
            // cannot collide into the same hash.
            builder.Append('').Append(part ?? string.Empty);
        }

        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(builder.ToString()));

        return Convert.ToHexStringLower(bytes);
    }
}
