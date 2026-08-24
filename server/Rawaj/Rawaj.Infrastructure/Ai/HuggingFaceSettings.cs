namespace Rawaj.Infrastructure.Ai;

public class HuggingFaceSettings
{
    public const string SectionName = "HuggingFace";

    public List<string> ApiKeys { get; set; } = [];
    public string ImageModel { get; set; } = "black-forest-labs/FLUX.1-schnell";

    /// <summary>
    /// HuggingFace retired the old per-model "hf-inference" direct route for this model; image
    /// generation now goes through one of several third-party inference providers behind HF's
    /// router. nscale was the one confirmed working (returns b64 image data directly, no second
    /// download hop needed) - see HuggingFaceImageGenerationService for the request/response shape.
    /// </summary>
    public string BaseUrl { get; set; } = "https://router.huggingface.co/nscale/v1/images/generations";
}
