using Rawaj.Domain.Entities.Tenants;
using Rawaj.Domain.ValueObjects;

namespace Rawaj.Application.Common.Models;

public record AccountSetupFields(
    string AccountType,
    string Country,
    string? City,
    string? Website,
    string? Facebook,
    string? Instagram,
    string? Youtube,
    string? TikTok,
    string? LinkedIn,
    string? X,
    string? Snapchat,
    string? AgencyName,
    string? AgencyPhone,
    string? AgencyCountryCode,
    string? AgencySize,
    string? ActiveClients,
    List<string>? PrimaryServices,
    string? BusinessName,
    string? BusinessPhone,
    string? BusinessCountryCode,
    string? Industry,
    string? BusinessSize,
    string? HearAboutUs);

public enum GetAccountSetupOutcome
{
    Success,
    NotFound,
    Forbidden
}

public record GetAccountSetupResult(GetAccountSetupOutcome Outcome, TenantAccountSetup? AccountSetup, BrandInfo? BrandInfo);

public enum SaveAccountSetupOutcome
{
    Saved,
    NotFound,
    Forbidden
}

public record SaveAccountSetupResult(SaveAccountSetupOutcome Outcome, TenantAccountSetup? AccountSetup, BrandInfo? BrandInfo);
