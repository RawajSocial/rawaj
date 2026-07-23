using MediatR;
using Rawaj.Application.Common.Interfaces;
using Rawaj.Application.Common.Models;
using Rawaj.Domain.Enums;

namespace Rawaj.Application.Features.TeamMembers.AllocateCoins;

/// <summary>
/// Sets a member's coin wallet to an absolute amount (not a delta) so the UI can read/write the
/// same number the owner sees. The handler escrows the difference out of/back into the tenant's
/// pool (<see cref="Rawaj.Domain.Entities.Tenants.Tenant.CoinBalance"/>).
/// </summary>
public record AllocateCoinsCommand(Guid TenantMemberId, int NewAllocation)
    : IRequest<Result<AllocateCoinsResponse>>, IRequireTenantRole
{
    public TenantMemberRole MinimumRole => TenantMemberRole.Admin;
}
