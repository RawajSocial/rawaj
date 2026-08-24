using Rawaj.Application.Common.Policies;
using Rawaj.Domain.Entities.Billing;
using Rawaj.Domain.Entities.Tenants;
using Rawaj.Domain.Enums;
using Xunit;

namespace Rawaj.Application.Tests.Policies;

public class CoinPolicyTests
{
    private static async Task<(Guid TenantId, Guid OwnerUserId, Guid MemberUserId, Guid MemberTenantMemberId)> SeedAsync(
        Rawaj.Persistence.AppDbContext dbContext, int tenantCoinBalance, int allocatedCoins, int spentCoins)
    {
        var planId = Guid.NewGuid();
        var subscriptionId = Guid.NewGuid();
        var tenantId = Guid.NewGuid();
        var ownerUserId = Guid.NewGuid();
        var memberUserId = Guid.NewGuid();
        var memberTenantMemberId = Guid.NewGuid();
        var now = DateTime.UtcNow;

        dbContext.SubscriptionPlans.Add(new SubscriptionPlan
        {
            Id = planId, Name = "TestPlan", Cost = 10, Currency = "USD", IsActive = true, CreatedAt = now
        });
        dbContext.Subscriptions.Add(new Subscription
        {
            Id = subscriptionId, SubscriptionPlanId = planId, Status = SubscriptionStatus.Active,
            CurrentPeriodStart = now, CurrentPeriodEnd = now.AddMonths(1), CreatedAt = now
        });
        dbContext.Tenants.Add(new Tenant
        {
            Id = tenantId, Name = "Test Tenant", Subdomain = Guid.NewGuid().ToString("N"),
            TenantType = TenantType.Agency, OwnerUserId = ownerUserId, SubscriptionId = subscriptionId,
            IsActive = true, CoinBalance = tenantCoinBalance, CreatedAt = now, UpdatedAt = now
        });
        dbContext.TenantMembers.Add(new TenantMember
        {
            Id = memberTenantMemberId, TenantId = tenantId, UserId = memberUserId, Role = TenantMemberRole.Editor,
            InvitationStatus = InvitationStatus.Accepted, JoinedAt = now, AllocatedCoins = allocatedCoins,
            SpentCoins = spentCoins, CreatedAt = now
        });

        await dbContext.SaveChangesAsync(CancellationToken.None);

        return (tenantId, ownerUserId, memberUserId, memberTenantMemberId);
    }

    [Fact]
    public async Task GetBalanceAsync_ForOwnerRole_ReturnsTenantPool()
    {
        using var dbContext = TestDbContextFactory.Create();
        var (tenantId, ownerUserId, _, _) = await SeedAsync(dbContext, tenantCoinBalance: 500, allocatedCoins: 0, spentCoins: 0);

        var balance = await CoinPolicy.GetBalanceAsync(dbContext, tenantId, ownerUserId, TenantMemberRole.Owner, CancellationToken.None);

        Assert.Equal(500, balance);
    }

    [Fact]
    public async Task GetBalanceAsync_ForEditorRole_ReturnsAllocatedMinusSpent()
    {
        using var dbContext = TestDbContextFactory.Create();
        var (tenantId, _, memberUserId, _) = await SeedAsync(dbContext, tenantCoinBalance: 500, allocatedCoins: 200, spentCoins: 50);

        var balance = await CoinPolicy.GetBalanceAsync(dbContext, tenantId, memberUserId, TenantMemberRole.Editor, CancellationToken.None);

        Assert.Equal(150, balance);
    }

    [Fact]
    public async Task TrySpendAsync_OwnerRole_WithSufficientBalance_DebitsTenantPool()
    {
        using var dbContext = TestDbContextFactory.Create();
        var (tenantId, ownerUserId, _, _) = await SeedAsync(dbContext, tenantCoinBalance: 100, allocatedCoins: 0, spentCoins: 0);

        var succeeded = await CoinPolicy.TrySpendAsync(dbContext, tenantId, ownerUserId, TenantMemberRole.Owner, 30, CancellationToken.None);
        await dbContext.SaveChangesAsync(CancellationToken.None);

        Assert.True(succeeded);
        Assert.Equal(70, dbContext.Tenants.Single(t => t.Id == tenantId).CoinBalance);
    }

    [Fact]
    public async Task TrySpendAsync_OwnerRole_WithInsufficientBalance_FailsAndMutatesNothing()
    {
        using var dbContext = TestDbContextFactory.Create();
        var (tenantId, ownerUserId, _, _) = await SeedAsync(dbContext, tenantCoinBalance: 10, allocatedCoins: 0, spentCoins: 0);

        var succeeded = await CoinPolicy.TrySpendAsync(dbContext, tenantId, ownerUserId, TenantMemberRole.Owner, 30, CancellationToken.None);

        Assert.False(succeeded);
        Assert.Equal(10, dbContext.Tenants.Single(t => t.Id == tenantId).CoinBalance);
    }

    [Fact]
    public async Task TrySpendAsync_EditorRole_WithSufficientWallet_DebitsWalletNotTenantPool()
    {
        using var dbContext = TestDbContextFactory.Create();
        var (tenantId, _, memberUserId, memberId) = await SeedAsync(dbContext, tenantCoinBalance: 1000, allocatedCoins: 100, spentCoins: 0);

        var succeeded = await CoinPolicy.TrySpendAsync(dbContext, tenantId, memberUserId, TenantMemberRole.Editor, 40, CancellationToken.None);
        await dbContext.SaveChangesAsync(CancellationToken.None);

        Assert.True(succeeded);
        Assert.Equal(40, dbContext.TenantMembers.Single(m => m.Id == memberId).SpentCoins);
        Assert.Equal(1000, dbContext.Tenants.Single(t => t.Id == tenantId).CoinBalance);
    }

    [Fact]
    public async Task TrySpendAsync_EditorRole_WithInsufficientWallet_Fails()
    {
        using var dbContext = TestDbContextFactory.Create();
        var (tenantId, _, memberUserId, _) = await SeedAsync(dbContext, tenantCoinBalance: 1000, allocatedCoins: 20, spentCoins: 15);

        var succeeded = await CoinPolicy.TrySpendAsync(dbContext, tenantId, memberUserId, TenantMemberRole.Editor, 10, CancellationToken.None);

        Assert.False(succeeded);
    }

    [Fact]
    public void InsufficientCoinsMessage_ProducesTheExactShapeTheFrontendParses()
    {
        // coin-error.util.ts on the frontend regex-matches this sentence to offer a "buy coins"
        // link - changing the wording here must change it there too.
        var message = CoinPolicy.InsufficientCoinsMessage(required: 200, balance: 50, action: "generate content");

        Assert.Equal("You need 200 coins to generate content, but only have 50.", message);
    }

    [Fact]
    public void ScheduledPostCapMessage_ProducesTheExpectedSentence()
    {
        var message = CoinPolicy.ScheduledPostCapMessage(max: 10);

        Assert.Equal("Your subscription plan allows a maximum of 10 scheduled post(s). Upgrade for more.", message);
    }
}
