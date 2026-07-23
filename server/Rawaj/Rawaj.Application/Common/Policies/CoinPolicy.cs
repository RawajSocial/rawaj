using Microsoft.EntityFrameworkCore;
using Rawaj.Application.Common.Interfaces;
using Rawaj.Domain.Entities.Tenants;
using Rawaj.Domain.Enums;

namespace Rawaj.Application.Common.Policies;

/// <summary>
/// There are two distinct coin balances per tenant: the tenant pool (<see cref="Tenant.CoinBalance"/>,
/// spent directly by the Owner/Admin) and each invited Editor/Viewer's own escrowed wallet
/// (<see cref="TenantMember.AllocatedCoins"/> minus <see cref="TenantMember.SpentCoins"/>, coins the
/// owner allocated out of their pool up front). An invited member must never see or spend the
/// owner's full pool — only their own wallet.
/// </summary>
public static class CoinPolicy
{
    public static async Task<int> GetBalanceAsync(
        IApplicationDbContext dbContext, Guid tenantId, Guid userId, TenantMemberRole role, CancellationToken cancellationToken)
    {
        if (role.HasAtLeast(TenantMemberRole.Admin))
        {
            return await dbContext.Tenants
                .Where(t => t.Id == tenantId)
                .Select(t => t.CoinBalance)
                .FirstAsync(cancellationToken);
        }

        var member = await dbContext.TenantMembers
            .Where(m => m.TenantId == tenantId && m.UserId == userId)
            .Select(m => new { m.AllocatedCoins, m.SpentCoins })
            .FirstAsync(cancellationToken);

        return member.AllocatedCoins - member.SpentCoins;
    }

    /// <summary>
    /// Debits <paramref name="cost"/> from the correct balance for a spend (e.g. one AI generation).
    /// Caller is responsible for calling <c>SaveChangesAsync</c> afterwards. Returns false (no
    /// mutation made) when the balance is insufficient.
    /// </summary>
    public static async Task<bool> TrySpendAsync(
        IApplicationDbContext dbContext, Guid tenantId, Guid userId, TenantMemberRole role, int cost, CancellationToken cancellationToken)
    {
        if (role.HasAtLeast(TenantMemberRole.Admin))
        {
            var tenant = await dbContext.Tenants.FirstAsync(t => t.Id == tenantId, cancellationToken);
            if (tenant.CoinBalance < cost)
            {
                return false;
            }

            tenant.CoinBalance -= cost;
            return true;
        }

        var member = await dbContext.TenantMembers
            .FirstAsync(m => m.TenantId == tenantId && m.UserId == userId, cancellationToken);

        if (member.AllocatedCoins - member.SpentCoins < cost)
        {
            return false;
        }

        member.SpentCoins += cost;
        return true;
    }
}
