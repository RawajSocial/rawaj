using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Rawaj.Application.Common.Interfaces;
using Rawaj.Domain.Common;
using Rawaj.Domain.Entities.AiOperations;
using Rawaj.Domain.Entities.Auth;
using Rawaj.Domain.Entities.Billing;
using Rawaj.Domain.Entities.BrandIntelligence;
using Rawaj.Domain.Entities.Campaigns;
using Rawaj.Domain.Entities.Platform;
using Rawaj.Domain.Entities.SocialMedia;
using Rawaj.Domain.Entities.Tenants;
using Rawaj.Persistence.Identity;

namespace Rawaj.Persistence;

public class AppDbContext : IdentityUserContext<ApplicationUser, Guid>, IApplicationDbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    public DbSet<Tenant> Tenants => Set<Tenant>();
    public DbSet<AccountSetup> AccountSetups => Set<AccountSetup>();
    public DbSet<TenantBrandProfile> TenantBrandProfiles => Set<TenantBrandProfile>();
    public DbSet<TenantMember> TenantMembers => Set<TenantMember>();
    public DbSet<TenantMemberBrandAccess> TenantMemberBrandAccesses => Set<TenantMemberBrandAccess>();
    public DbSet<TenantInvitation> TenantInvitations => Set<TenantInvitation>();

    public DbSet<Competitor> Competitors => Set<Competitor>();
    public DbSet<RagDocument> RagDocuments => Set<RagDocument>();

    public DbSet<MarketingCampaign> MarketingCampaigns => Set<MarketingCampaign>();
    public DbSet<ContentItem> ContentItems => Set<ContentItem>();
    public DbSet<ContentRevision> ContentRevisions => Set<ContentRevision>();
    public DbSet<VisualAsset> VisualAssets => Set<VisualAsset>();

    public DbSet<SocialAccount> SocialAccounts => Set<SocialAccount>();
    public DbSet<ScheduledPost> ScheduledPosts => Set<ScheduledPost>();
    public DbSet<PostAnalytics> PostAnalytics => Set<PostAnalytics>();

    public DbSet<AiJob> AiJobs => Set<AiJob>();
    public DbSet<AiPipelineRun> AiPipelineRuns => Set<AiPipelineRun>();
    public DbSet<AiPipelineStage> AiPipelineStages => Set<AiPipelineStage>();
    public DbSet<AiArtifact> AiArtifacts => Set<AiArtifact>();

    public DbSet<SubscriptionPlan> SubscriptionPlans => Set<SubscriptionPlan>();
    public DbSet<Subscription> Subscriptions => Set<Subscription>();
    public DbSet<CoinPackage> CoinPackages => Set<CoinPackage>();
    public DbSet<BillingTransaction> BillingTransactions => Set<BillingTransaction>();
    public DbSet<CoinLedgerEntry> CoinLedgerEntries => Set<CoinLedgerEntry>();

    public DbSet<Notification> Notifications => Set<Notification>();
    public DbSet<OutboxMessage> OutboxMessages => Set<OutboxMessage>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();

    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();
    public DbSet<EmailOtpCode> EmailOtpCodes => Set<EmailOtpCode>();

    /// <summary>
    /// SQL Server's `rowversion` columns auto-generate their own value on every write and ignore
    /// whatever the app sets — this bump is a no-op there. The EF Core InMemory provider (used by
    /// the test suite) has no such auto-generation, so without this, every insert of a
    /// RowVersion-tracked entity fails the model's "required property" check and no update can
    /// ever produce a genuine concurrency conflict to test against. See IConcurrencyAware.
    /// </summary>
    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        foreach (var entry in ChangeTracker.Entries<IConcurrencyAware>())
        {
            if (entry.State is EntityState.Added or EntityState.Modified)
            {
                entry.Entity.RowVersion = Guid.NewGuid().ToByteArray();
            }
        }

        return base.SaveChangesAsync(cancellationToken);
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);

        modelBuilder.Entity<ApplicationUser>(b =>
        {
            b.ToTable("users");
            b.Property(u => u.FullName).HasMaxLength(150).IsRequired();
            b.Property(u => u.AvatarUrl).HasColumnType("nvarchar(max)");
            b.Property(u => u.PreferredLanguage).HasConversion<string>().HasMaxLength(5).IsRequired();
        });
    }
}
