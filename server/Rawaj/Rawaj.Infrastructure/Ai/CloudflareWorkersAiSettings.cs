namespace Rawaj.Infrastructure.Ai;

public class CloudflareWorkersAiSettings
{
    public const string SectionName = "CloudflareWorkersAi";

    public string AccountId { get; set; } = string.Empty;
    public string ApiToken { get; set; } = string.Empty;
    public string ImageModel { get; set; } = "@cf/black-forest-labs/flux-2-klein-4b";
    public string BaseUrl { get; set; } = "https://api.cloudflare.com/client/v4/accounts";
}
