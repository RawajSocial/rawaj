using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace Rawaj.Application.Common.Services;

/// <summary>
/// Keeps only a subset of a JSON object's top-level fields before it's pasted into a downstream
/// prompt. Several stages in the strategy pipeline (see <c>StrategyPrompt</c>) used to paste an entire
/// upstream artifact into two or three separate calls verbatim, paying full input-token price for
/// fields that specific call never reads — e.g. StrategyBlueprint doesn't need CampaignAnalysis's
/// risks/opportunities to design a posting cadence. This trims to what each call actually consumes.
///
/// <para>Falls back to the original string unchanged if the input isn't valid JSON (an already-broken
/// artifact, or something unexpected) — trimming is a cost optimization, not something that should
/// ever turn a working prompt into a broken one.</para>
/// </summary>
public static class JsonFieldSelector
{
    // Every artifact this trims is Arabic narrative text. System.Text.Json's default encoder escapes
    // non-ASCII characters as \uXXXX, which would silently turn every kept Arabic field into 6-byte
    // escape sequences per character — the opposite of what a token-trimming helper should do, and
    // exactly the kind of thing that only shows up once real (non-English-test) data hits it.
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
    };

    public static string? KeepFields(string? json, params string[] fields)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return json;
        }

        try
        {
            if (JsonNode.Parse(json) is not JsonObject node)
            {
                return json;
            }

            var trimmed = new JsonObject();

            foreach (var field in fields)
            {
                if (node.TryGetPropertyValue(field, out var value) && value is not null)
                {
                    trimmed[field] = value.DeepClone();
                }
            }

            return trimmed.Count > 0 ? trimmed.ToJsonString(SerializerOptions) : json;
        }
        catch (JsonException)
        {
            return json;
        }
    }
}
