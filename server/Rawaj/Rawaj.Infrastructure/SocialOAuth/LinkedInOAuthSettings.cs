namespace Rawaj.Infrastructure.SocialOAuth;

public class LinkedInOAuthSettings
{
    public const string SectionName = "SocialOAuth:LinkedIn";

    public string ClientId { get; set; } = string.Empty;
    public string ClientSecret { get; set; } = string.Empty;
    public List<string> Scopes { get; set; } = ["openid", "profile", "w_member_social"];
}
