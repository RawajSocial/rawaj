using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Rawaj.Application.Common.Interfaces;
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
    public DbSet<TenantBrandProfile> TenantBrandProfiles => Set<TenantBrandProfile>();
    public DbSet<TenantMember> TenantMembers => Set<TenantMember>();
    public DbSet<TenantMemberBrandAccess> TenantMemberBrandAccesses => Set<TenantMemberBrandAccess>();

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

    public DbSet<SubscriptionPlan> SubscriptionPlans => Set<SubscriptionPlan>();
    public DbSet<Subscription> Subscriptions => Set<Subscription>();

    public DbSet<Notification> Notifications => Set<Notification>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();

    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();

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
