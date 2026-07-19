namespace Rawaj.Infrastructure.Storage;

public class StorageSettings
{
    public const string SectionName = "Storage";

    public string BasePath { get; set; } = "wwwroot/uploads";
    public string PublicBaseUrl { get; set; } = null!;
}
