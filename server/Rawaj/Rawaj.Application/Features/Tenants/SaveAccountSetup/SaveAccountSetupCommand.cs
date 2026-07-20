using MediatR;
using Rawaj.Application.Common.Models;
using Rawaj.Application.Features.Tenants.Common;

namespace Rawaj.Application.Features.Tenants.SaveAccountSetup;

public record SaveAccountSetupCommand(
    Guid BrandProfileId,
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
    string? HearAboutUs) : IRequest<Result<TenantAccountSetupResponse>>;
