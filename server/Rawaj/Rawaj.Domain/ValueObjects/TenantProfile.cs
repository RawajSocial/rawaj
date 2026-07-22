namespace Rawaj.Domain.ValueObjects;

public class TenantProfile
{
    public string? Phone { get; set; }
    public string? Industry { get; set; }
    public string? Country { get; set; }
    public string? City { get; set; }
    public string? Website { get; set; }
    public string? AgencySize { get; set; }
    public List<string> ServicesOffered { get; set; } = [];
}
