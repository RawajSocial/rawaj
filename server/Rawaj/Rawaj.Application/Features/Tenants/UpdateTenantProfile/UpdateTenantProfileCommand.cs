using MediatR;
using Rawaj.Application.Common.Interfaces;
using Rawaj.Application.Common.Models;
using Rawaj.Domain.Enums;

namespace Rawaj.Application.Features.Tenants.UpdateTenantProfile;

public record UpdateTenantProfileCommand(
    string? Phone,
    string? Industry,
    string? Country,
    string? City,
    string? Website) : IRequest<Result<UpdateTenantProfileResponse>>, IRequireTenantRole
{
    public TenantMemberRole MinimumRole => TenantMemberRole.Admin;
}
