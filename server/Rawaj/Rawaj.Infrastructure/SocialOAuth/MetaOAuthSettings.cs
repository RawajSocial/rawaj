namespace Rawaj.Infrastructure.SocialOAuth;

public class MetaOAuthSettings
{
    public const string SectionName = "SocialOAuth:Meta";

    public string ClientId { get; set; } = string.Empty;
    public string ClientSecret { get; set; } = string.Empty;
    public string ApiVersion { get; set; } = "v21.0";
    public List<string> FacebookScopes { get; set; } = ["pages_show_list", "pages_manage_posts", "pages_read_engagement"];
    public List<string> InstagramScopes { get; set; } =
        ["pages_show_list", "instagram_basic", "instagram_content_publish", "pages_read_engagement"];
}
