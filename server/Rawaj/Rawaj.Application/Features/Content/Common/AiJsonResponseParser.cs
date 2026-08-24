using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Unicode;

namespace Rawaj.Application.Features.Content.Common;

/// <summary>
/// Pulls the JSON payload out of a text model's response.
/// <para>
/// Every prompt in <see cref="ContentPromptBuilder"/> that wants structured output ends with
/// "Respond with ONLY a valid JSON object (no markdown fences, no commentary)". Models comply
/// most of the time and not always — Groq in particular frequently wraps the object in a
/// <c>```json</c> fence, and sometimes prefixes it with a line of explanation. The response is
/// then still perfectly good JSON *inside* a wrapper that <see cref="JsonDocument.Parse(string)"/>
/// chokes on.
/// </para>
/// <para>
/// This mattered more than it sounds: the campaign handlers used to assign the raw response
/// straight into columns named <c>AiPlanJson</c>/<c>DiagnosisJson</c>, so a fenced response was
/// persisted as-is. The backend's own "does a plan exist" check only tests for non-empty text, so
/// it passed — but every consumer that actually tried to parse it (the whole strategy review UI)
/// silently got nothing, and the user had paid for a strategy they could never see.
/// </para>
/// </summary>
public static class AiJsonResponseParser
{
    /// <summary>
    /// Writes non-ASCII characters as themselves instead of <c>\uXXXX</c> escapes.
    ///
    /// <para>This is a token-cost fix, not a cosmetic one. Groq returns Arabic escaped, and an
    /// artifact stored that way is pasted verbatim into the next stage's prompt — so every Arabic
    /// character costs six ASCII characters instead of one, and <c>ت</c> tokenizes into several
    /// tokens where <c>ت</c> is one or two. Measured on a real stored Strategy artifact: 17,250
    /// characters escaped versus 3,658 unescaped, with whitespace accounting for 32 of the
    /// difference. That 4.7x inflation is what pushed a single ContentPlan request past Groq's
    /// entire per-minute token budget, which no amount of retrying can get under.</para>
    ///
    /// <para>The content is byte-for-byte the same JSON either way, so nothing downstream can tell
    /// the difference except the token bill.</para>
    /// </summary>
    private static readonly JsonSerializerOptions CompactUnicode = new()
    {
        Encoder = JavaScriptEncoder.Create(UnicodeRanges.All),
    };

    /// <summary>
    /// Rewrites already-stored JSON into the unescaped form described on <see cref="CompactUnicode"/>.
    /// Used on the way *out* of the artifact store as well as on the way in, so artifacts written
    /// before this existed stop inflating prompts without needing their rows rewritten.
    /// Returns the input unchanged if it isn't parseable — callers hold text a model produced, and
    /// silently dropping it here would be worse than passing it along as-is.
    /// </summary>
    public static string? Normalize(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return json;
        }

        try
        {
            using var document = JsonDocument.Parse(json);
            return JsonSerializer.Serialize(document.RootElement, CompactUnicode);
        }
        catch (JsonException)
        {
            return json;
        }
    }

    /// <summary>
    /// Returns the JSON object/array found in <paramref name="raw"/>, or <c>null</c> when there
    /// isn't one that parses. Callers should treat <c>null</c> as a failed generation rather than
    /// storing the raw text — a column named <c>*Json</c> should hold JSON.
    /// </summary>
    public static string? ExtractJsonPayload(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
        {
            return null;
        }

        var text = raw.Trim();

        // Fast path: already clean JSON.
        if (TryValidate(text, out var validated))
        {
            return validated;
        }

        // Slice from the first opening brace/bracket to its matching close. This handles both a
        // markdown fence (```json { ... } ```) and stray commentary on either side, without
        // needing to recognise every wrapper format a model might invent.
        var start = FirstIndexOfAny(text, '{', '[');
        if (start < 0)
        {
            return null;
        }

        var open = text[start];
        var close = open == '{' ? '}' : ']';
        var end = text.LastIndexOf(close);
        if (end <= start)
        {
            return null;
        }

        var candidate = text[start..(end + 1)];
        return TryValidate(candidate, out var extracted) ? extracted : null;
    }

    private static int FirstIndexOfAny(string text, char a, char b)
    {
        var indexA = text.IndexOf(a);
        var indexB = text.IndexOf(b);
        if (indexA < 0) return indexB;
        if (indexB < 0) return indexA;
        return Math.Min(indexA, indexB);
    }

    private static bool TryValidate(string candidate, out string? json)
    {
        json = null;
        if (candidate.Length == 0 || (candidate[0] != '{' && candidate[0] != '['))
        {
            return false;
        }

        try
        {
            using var document = JsonDocument.Parse(candidate);
            // Re-serialized rather than returned verbatim, so the escaped Arabic Groq hands back
            // never reaches storage in the first place — see CompactUnicode.
            json = JsonSerializer.Serialize(document.RootElement, CompactUnicode);
            return true;
        }
        catch (JsonException)
        {
            return false;
        }
    }
}
