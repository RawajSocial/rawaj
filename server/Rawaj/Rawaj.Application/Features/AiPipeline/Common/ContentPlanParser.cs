using System.Text.Json;
using Rawaj.Application.Features.Content.Common;
using Rawaj.Domain.Enums;

namespace Rawaj.Application.Features.AiPipeline.Common;

/// <summary>One post as the model described it, before it becomes a <c>ContentItem</c> row.</summary>
public sealed record GeneratedPostDraft(
    SocialPlatform Platform, ContentType ContentType, string Content, List<string> Hashtags, string? Cta,
    DateTime SuggestedPostAt, string? ImagePrompt);

/// <summary>
/// Turns a <c>ContentPlan</c> artifact's <c>posts</c> array into drafts ready to become
/// <c>ContentItem</c> rows. Shared by the <c>ContentPlan</c> stage executor and
/// <c>GenerateCampaignContentCommandHandler</c> (the handler the pipeline is replacing) so both read
/// exactly the same tolerance rules rather than two copies that could drift.
///
/// <para>Per-item tolerant: a malformed individual post is skipped rather than failing the whole
/// batch — one bad entry in a ten-post response should not discard the nine good ones the tenant
/// already paid for.</para>
/// </summary>
public static class ContentPlanParser
{
    public static List<GeneratedPostDraft> Parse(string rawJson, IReadOnlyList<SocialPlatform> allowedPlatforms, DateTime baseDate)
    {
        var drafts = new List<GeneratedPostDraft>();

        // A model that fences its JSON would otherwise fail every post in the batch after the text
        // call had already been made and paid for.
        var payload = AiJsonResponseParser.ExtractJsonPayload(rawJson);
        if (payload is null)
        {
            return drafts;
        }

        JsonDocument document;
        try
        {
            document = JsonDocument.Parse(payload);
        }
        catch (JsonException)
        {
            return drafts;
        }

        using (document)
        {
            if (!document.RootElement.TryGetProperty("posts", out var postsElement) || postsElement.ValueKind != JsonValueKind.Array)
            {
                return drafts;
            }

            foreach (var post in postsElement.EnumerateArray())
            {
                try
                {
                    if (!post.TryGetProperty("platform", out var platformProp) ||
                        !Enum.TryParse<SocialPlatform>(platformProp.GetString(), true, out var platform) ||
                        !allowedPlatforms.Contains(platform))
                    {
                        continue;
                    }

                    if (!post.TryGetProperty("content", out var contentProp) || string.IsNullOrWhiteSpace(contentProp.GetString()))
                    {
                        continue;
                    }

                    var contentType = ContentType.Post;
                    if (post.TryGetProperty("contentType", out var contentTypeProp))
                    {
                        Enum.TryParse(contentTypeProp.GetString(), true, out contentType);
                    }

                    var dayOffset = 0;
                    if (post.TryGetProperty("dayOffset", out var dayOffsetProp) && dayOffsetProp.TryGetInt32(out var parsedDayOffset))
                    {
                        dayOffset = Math.Clamp(parsedDayOffset, 0, 90);
                    }

                    var hour = 12;
                    if (post.TryGetProperty("hour", out var hourProp) && hourProp.TryGetInt32(out var parsedHour))
                    {
                        hour = Math.Clamp(parsedHour, 0, 23);
                    }

                    var hashtags = new List<string>();
                    if (post.TryGetProperty("hashtags", out var hashtagsProp) && hashtagsProp.ValueKind == JsonValueKind.Array)
                    {
                        hashtags = hashtagsProp.EnumerateArray()
                            .Select(h => h.GetString())
                            .Where(h => !string.IsNullOrWhiteSpace(h))
                            .Select(h => h!)
                            .ToList();
                    }

                    string? cta = null;
                    if (post.TryGetProperty("cta", out var ctaProp) && ctaProp.ValueKind == JsonValueKind.String)
                    {
                        cta = ctaProp.GetString();
                    }

                    // Optional: an older/uncooperative model response without it still yields a
                    // usable post, it just falls back to the post copy for the image.
                    string? imagePrompt = null;
                    if (post.TryGetProperty("imagePrompt", out var imagePromptProp) && imagePromptProp.ValueKind == JsonValueKind.String)
                    {
                        var value = imagePromptProp.GetString();
                        imagePrompt = string.IsNullOrWhiteSpace(value) ? null : value;
                    }

                    drafts.Add(new GeneratedPostDraft(
                        platform, contentType, contentProp.GetString()!, hashtags, cta,
                        baseDate.AddDays(dayOffset).AddHours(hour), imagePrompt));
                }
                catch (JsonException)
                {
                    // Skip malformed individual entries rather than discarding the whole batch.
                }
            }
        }

        return drafts;
    }
}
