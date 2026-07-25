using Microsoft.EntityFrameworkCore;
using Rawaj.Application.Common.Interfaces;
using Rawaj.Domain.Entities.Billing;
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
    public static async Task<int> GetBalanceAsync(IApplicationDbContext dbContext, Guid tenantId, Guid userId, TenantMemberRole role, CancellationToken cancellationToken)
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
    /// Per-tenant serialization for <see cref="TrySpendAsync"/>'s read-check-write, so two
    /// concurrent requests in this process (e.g. the same paid action fired from two browser tabs)
    /// can't both read the same pre-deduction balance and both pass the check. Scoped per process:
    /// this does not serialize across multiple server instances behind a load balancer - a true
    /// multi-instance fix would need a DB-level atomic update (e.g. EF Core's ExecuteUpdate), which
    /// the in-memory provider used by the test suite can't execute, so this is the safe middle
    /// ground for now.
    /// </summary>
    private static readonly System.Collections.Concurrent.ConcurrentDictionary<Guid, SemaphoreSlim> SpendLocks = new();

    private static SemaphoreSlim LockFor(Guid tenantId) => SpendLocks.GetOrAdd(tenantId, _ => new SemaphoreSlim(1, 1));

    /// <summary>
    /// Debits <paramref name="cost"/> from the correct balance for a spend (e.g. one AI generation).
    /// Holds a per-tenant lock across the read-check-write AND the resulting
    /// <c>SaveChangesAsync</c> (a second concurrent caller must not be able to read the balance
    /// again until the first caller's deduction has actually landed in the database) - so unlike
    /// most of this codebase's mutations, this one commits immediately rather than waiting for the
    /// caller's own later <c>SaveChangesAsync</c>. Returns false (no mutation made) when the
    /// balance is insufficient.
    /// </summary>
    public static async Task<bool> TrySpendAsync(
        IApplicationDbContext dbContext, Guid tenantId, Guid userId, TenantMemberRole role, int cost, CancellationToken cancellationToken,
        string reason = "spend", Guid? referenceId = null, string? referenceType = null)
    {
        var gate = LockFor(tenantId);
        await gate.WaitAsync(cancellationToken);
        try
        {
            Guid? spendingMemberId = null;

            if (role.HasAtLeast(TenantMemberRole.Admin))
            {
                var tenant = await dbContext.Tenants.FirstAsync(t => t.Id == tenantId, cancellationToken);
                if (tenant.CoinBalance < cost)
                {
                    return false;
                }

                tenant.CoinBalance -= cost;
            }
            else
            {
                var member = await dbContext.TenantMembers
                    .FirstAsync(m => m.TenantId == tenantId && m.UserId == userId, cancellationToken);

                if (member.AllocatedCoins - member.SpentCoins < cost)
                {
                    return false;
                }

                member.SpentCoins += cost;
                spendingMemberId = member.Id;
            }

            dbContext.CoinLedgerEntries.Add(new CoinLedgerEntry
            {
                Id = Guid.NewGuid(),
                TenantId = tenantId,
                TenantMemberId = spendingMemberId,
                Amount = -cost,
                Reason = reason,
                ReferenceId = referenceId,
                ReferenceType = referenceType,
                CreatedAt = DateTime.UtcNow
            });

            await dbContext.SaveChangesAsync(cancellationToken);
            return true;
        }
        finally
        {
            gate.Release();
        }
    }

    /// <summary>
    /// The single place the "not enough coins" sentence is produced. The frontend's
    /// coin-error.util.ts parses this exact shape to offer a "buy coins" link - change the
    /// format here and there together.
    /// </summary>
    public static string InsufficientCoinsMessage(int required, int balance, string action) => $"You need {required} coins to {action}, but only have {balance}.";

    /// <summary>Same idea as <see cref="InsufficientCoinsMessage"/>, for the scheduled-posts plan cap
    /// (checked identically by SchedulePost and ScheduleCampaignPosts).</summary>
    public static string ScheduledPostCapMessage(int max) => $"Your subscription plan allows a maximum of {max} scheduled post(s). Upgrade for more.";
}
