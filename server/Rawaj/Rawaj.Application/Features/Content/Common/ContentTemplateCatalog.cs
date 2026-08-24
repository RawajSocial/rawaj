using Rawaj.Domain.Enums;

namespace Rawaj.Application.Features.Content.Common;

public static class ContentTemplateCatalog
{
    private static readonly Dictionary<ContentTemplateStyle, (string Structure, string ImageStyle)> Entries = new()
    {
        [ContentTemplateStyle.Auto] = (
            "Vary post structure naturally across common social content templates (product highlight, " +
            "promotional offer, educational tip, engagement question, behind-the-scenes, testimonial, " +
            "announcement) so the batch feels diverse rather than repetitive.",
            "Vary visual style naturally to match each post's purpose (product photography, bold promo " +
            "graphic, educational graphic, candid behind-the-scenes shot, etc.)."),
        [ContentTemplateStyle.ProductHighlight] = (
            "Lead with the product or service's core benefit, add one concrete supporting detail, and end " +
            "with a clear call to action.",
            "Clean, well-lit product-focused photography style with a minimal, uncluttered background."),
        [ContentTemplateStyle.PromotionalOffer] = (
            "Open with the offer or discount itself, create urgency or scarcity, and end with a direct " +
            "call to action to redeem it.",
            "Bold, high-contrast graphic design with large short text overlay and vibrant colors."),
        [ContentTemplateStyle.EducationalTip] = (
            "Frame the post as a single useful tip or fact relevant to the brand's industry, structured as " +
            "a hook followed by the tip and a short takeaway.",
            "Clear, friendly educational graphic style, icon-based or step-based layout."),
        [ContentTemplateStyle.EngagementQuestion] = (
            "Open with a genuine question relevant to the audience's interests or pain points to invite " +
            "comments, keep it short and conversational.",
            "Warm, approachable lifestyle-photography style that invites conversation."),
        [ContentTemplateStyle.BehindTheScenes] = (
            "Write in a candid, personal tone showing the human/process side of the brand, as if pulling " +
            "back the curtain on how something is made or done.",
            "Candid, unpolished behind-the-scenes photography style, natural lighting."),
        [ContentTemplateStyle.Testimonial] = (
            "Frame the post around a customer's positive experience or result, written to feel authentic " +
            "and specific rather than generic praise.",
            "Warm, authentic photography style suitable for a customer story or quote card."),
        [ContentTemplateStyle.Announcement] = (
            "Open by clearly stating the news itself, add why it matters to the audience, and close with " +
            "what happens next or how to act on it.",
            "Clean, professional announcement graphic style with clear typography hierarchy."),
    };

    public static string StructureGuidance(ContentTemplateStyle style) => Entries[style].Structure;

    public static string ImageStyleHint(ContentTemplateStyle style) => Entries[style].ImageStyle;
}
