namespace Rawaj.Infrastructure.Ai;

public class HuggingFaceSettings
{
    public const string SectionName = "HuggingFace";

    public List<string> ApiKeys { get; set; } = [];
    public string ImageModel { get; set; } = "black-forest-labs/FLUX.1-schnell";
    public string BaseUrl { get; set; } = "https://router.huggingface.co/hf-inference/models/";
}
