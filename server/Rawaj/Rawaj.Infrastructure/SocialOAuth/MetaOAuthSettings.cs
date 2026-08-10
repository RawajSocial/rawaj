namespace Rawaj.Infrastructure.SocialOAuth;

public class MetaOAuthSettings
{
    public const string SectionName = "SocialOAuth:Meta";

    public string ClientId { get; set; } = string.Empty;
    public string ClientSecret { get; set; } = string.Empty;
    public string ApiVersion { get; set; } = "v21.0";
    // pages_read_user_content is required by Meta to read a Page post's reactions/comments/shares
    // via field expansion (?fields=reactions.summary(...),comments.summary(...),shares) - without
    // it the Graph API returns error #10 ("This endpoint requires the 'pages_read_user_content'
    // permission...") and the whole engagement call fails, even though pages_read_engagement is
    // granted.
    public List<string> FacebookScopes { get; set; } =
        ["pages_show_list", "pages_manage_posts", "pages_read_engagement", "pages_read_user_content", "read_insights"];
    public List<string> InstagramScopes { get; set; } =
        ["pages_show_list", "instagram_basic", "instagram_content_publish", "pages_read_engagement"];
}
