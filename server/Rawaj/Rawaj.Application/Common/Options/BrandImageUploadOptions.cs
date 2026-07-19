namespace Rawaj.Application.Common.Options;

public class BrandImageUploadOptions
{
    public const string SectionName = "BrandImageUpload";

    public long MaxSizeBytes { get; set; } = 5 * 1024 * 1024;
}
