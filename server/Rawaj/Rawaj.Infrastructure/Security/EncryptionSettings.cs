namespace Rawaj.Infrastructure.Security;

public class EncryptionSettings
{
    public const string SectionName = "Encryption";

    public string Key { get; set; } = null!;
}
