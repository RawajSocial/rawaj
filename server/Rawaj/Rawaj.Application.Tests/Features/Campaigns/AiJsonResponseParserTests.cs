using Rawaj.Application.Features.Content.Common;
using Xunit;

namespace Rawaj.Application.Tests.Features.Campaigns;

/// <summary>
/// The campaign AI handlers ask for bare JSON and assign the response into columns named
/// <c>AiPlanJson</c>/<c>DiagnosisJson</c>. Models don't always comply, and a fenced response used
/// to be stored verbatim — passing every "a plan exists" check while rendering as nothing in the
/// review UI, which left the user with a paid-for strategy they couldn't see or approve.
/// </summary>
public class AiJsonResponseParserTests
{
    [Fact]
    public void ExtractJsonPayload_ReturnsCleanJsonUnchanged()
    {
        const string json = "{\"executiveSummary\":\"hello\"}";

        Assert.Equal(json, AiJsonResponseParser.ExtractJsonPayload(json));
    }

    [Fact]
    public void ExtractJsonPayload_StripsMarkdownFences()
    {
        // The single most common real-world deviation from "no markdown fences".
        var raw = "```json\n{\"executiveSummary\":\"hello\"}\n```";

        Assert.Equal("{\"executiveSummary\":\"hello\"}", AiJsonResponseParser.ExtractJsonPayload(raw));
    }

    [Fact]
    public void ExtractJsonPayload_StripsBareFences()
    {
        var raw = "```\n{\"a\":1}\n```";

        Assert.Equal("{\"a\":1}", AiJsonResponseParser.ExtractJsonPayload(raw));
    }

    [Fact]
    public void ExtractJsonPayload_StripsSurroundingCommentary()
    {
        var raw = "Sure! Here is the strategy you asked for:\n{\"a\":1}\nLet me know if you'd like changes.";

        Assert.Equal("{\"a\":1}", AiJsonResponseParser.ExtractJsonPayload(raw));
    }

    [Fact]
    public void ExtractJsonPayload_HandlesArrayPayloads()
    {
        // BuildOnboardingQuestionsPrompt asks for a JSON *array*, not an object.
        var raw = "```json\n[{\"question\":\"q\"}]\n```";

        Assert.Equal("[{\"question\":\"q\"}]", AiJsonResponseParser.ExtractJsonPayload(raw));
    }

    [Fact]
    public void ExtractJsonPayload_PreservesNestedBraces()
    {
        const string json = "{\"campaignBlueprint\":{\"pillars\":[\"a\",\"b\"]},\"x\":{\"y\":{\"z\":1}}}";

        Assert.Equal(json, AiJsonResponseParser.ExtractJsonPayload("```json\n" + json + "\n```"));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("I'm sorry, I can't help with that.")]
    [InlineData("{\"unterminated\": ")]
    public void ExtractJsonPayload_ReturnsNullWhenThereIsNoUsableJson(string? raw)
    {
        // Null is the signal for "treat this as a failed generation" — callers must not persist
        // the raw text into a *Json column, and must not charge for it.
        Assert.Null(AiJsonResponseParser.ExtractJsonPayload(raw));
    }
}
