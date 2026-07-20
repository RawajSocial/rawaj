namespace Rawaj.Domain.ValueObjects;

public class AgencyDetails
{
    public string? AgencyName { get; set; }
    public string? AgencyPhone { get; set; }
    public string? AgencyCountryCode { get; set; }
    public string? AgencySize { get; set; }
    public string? ActiveClients { get; set; }
    public List<string> PrimaryServices { get; set; } = [];
}
