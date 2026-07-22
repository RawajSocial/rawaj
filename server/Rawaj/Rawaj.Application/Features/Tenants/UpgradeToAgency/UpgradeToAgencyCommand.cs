using MediatR;
using Rawaj.Application.Common.Interfaces;
using Rawaj.Application.Common.Models;
using Rawaj.Domain.Enums;

namespace Rawaj.Application.Features.Tenants.UpgradeToAgency;

public record UpgradeToAgencyCommand(
    string AgencySize,
    List<string> ServicesOffered,
    string? Phone,
    string? Industry,
    string? Country,
    string? City,
    string? Website) : IRequest<Result<UpgradeToAgencyResponse>>, IRequireTenantRole
{
    public TenantMemberRole MinimumRole => TenantMemberRole.Owner;
}
