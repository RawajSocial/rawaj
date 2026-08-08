using System.Text.Json;

namespace Rawaj.Application.Common.Policies;

/// <summary>
/// Guards against a model drifting into a non-Arabic language despite the prompt asking for Arabic
/// — JSON mode only constrains syntax, not the language of the string values inside it. Used where
/// nothing else validates the response before it reaches the user (see
/// <c>GenerateOnboardingQuestionsCommandHandler</c>); pipeline stages that already validate JSON
/// shape via <c>ArtifactSchema</c> don't need this on top.
/// </summary>
public static class ArabicContentPolicy
{
    private const double MinimumArabicLetterRatio = 0.5;

    /// <summary>Inclusive bounds of the Arabic Unicode block (U+0600-U+06FF).</summary>
    private const char ArabicBlockStart = '؀';
    private const char ArabicBlockEnd = 'ۿ';

    /// <summary>
    /// True when at least half of the letter characters across every string value in
    /// <paramref name="json"/> fall in the Arabic Unicode block. Digits, punctuation and JSON syntax
    /// are ignored so they can't dilute or pad the ratio. A JSON value with no letters at all (empty
    /// strings, an empty array) isn't evidence of the wrong language, so it passes. Returns false
    /// (fail closed) if the text isn't valid JSON.
    /// </summary>
    public static bool HasSufficientArabicContent(string json)
    {
        JsonDocument document;
        try
        {
            document = JsonDocument.Parse(json);
        }
        catch (JsonException)
        {
            return false;
        }

        using (document)
        {
            var arabicLetters = 0;
            var totalLetters = 0;
            CountLetters(document.RootElement, ref arabicLetters, ref totalLetters);

            return totalLetters == 0 || (double)arabicLetters / totalLetters >= MinimumArabicLetterRatio;
        }
    }

    private static void CountLetters(JsonElement element, ref int arabicLetters, ref int totalLetters)
    {
        switch (element.ValueKind)
        {
            case JsonValueKind.String:
                foreach (var c in element.GetString() ?? string.Empty)
                {
                    if (!char.IsLetter(c))
                    {
                        continue;
                    }

                    totalLetters++;
                    if (c >= ArabicBlockStart && c <= ArabicBlockEnd)
                    {
                        arabicLetters++;
                    }
                }
                break;

            case JsonValueKind.Object:
                foreach (var property in element.EnumerateObject())
                {
                    CountLetters(property.Value, ref arabicLetters, ref totalLetters);
                }
                break;

            case JsonValueKind.Array:
                foreach (var item in element.EnumerateArray())
                {
                    CountLetters(item, ref arabicLetters, ref totalLetters);
                }
                break;
        }
    }
}
