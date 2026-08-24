using Microsoft.EntityFrameworkCore;
using Rawaj.Domain.Entities.AiOperations;
using Rawaj.Domain.Entities.Auth;
using Rawaj.Domain.Entities.Billing;
using Rawaj.Domain.Entities.BrandIntelligence;
using Rawaj.Domain.Entities.Campaigns;
using Rawaj.Domain.Entities.Platform;
using Rawaj.Domain.Entities.SocialMedia;
using Rawaj.Domain.Entities.Tenants;

namespace Rawaj.Application.Common.Interfaces;

public interface IApplicationDbContext
{
    DbSet<Tenant> Tenants { get; }
    DbSet<AccountSetup> AccountSetups { get; }
    DbSet<TenantMember> TenantMembers { get; }
    DbSet<TenantMemberBrandAccess> TenantMemberBrandAccesses { get; }
    DbSet<TenantInvitation> TenantInvitations { get; }
    DbSet<TenantBrandProfile> TenantBrandProfiles { get; }
    DbSet<SubscriptionPlan> SubscriptionPlans { get; }
    DbSet<Subscription> Subscriptions { get; }
    DbSet<CoinPackage> CoinPackages { get; }
    DbSet<BillingTransaction> BillingTransactions { get; }
    DbSet<CoinLedgerEntry> CoinLedgerEntries { get; }
    DbSet<PendingCheckoutSession> PendingCheckoutSessions { get; }
    DbSet<MarketingCampaign> MarketingCampaigns { get; }
    DbSet<ContentItem> ContentItems { get; }
    DbSet<ContentRevision> ContentRevisions { get; }
    DbSet<VisualAsset> VisualAssets { get; }
    DbSet<AiJob> AiJobs { get; }
    DbSet<AiPipelineRun> AiPipelineRuns { get; }
    DbSet<AiPipelineStage> AiPipelineStages { get; }
    DbSet<AiArtifact> AiArtifacts { get; }
    DbSet<Competitor> Competitors { get; }
    DbSet<RagDocument> RagDocuments { get; }
    DbSet<SocialAccount> SocialAccounts { get; }
    DbSet<ScheduledPost> ScheduledPosts { get; }
    DbSet<PostAnalytics> PostAnalytics { get; }
    DbSet<FollowerCountSnapshot> FollowerCountSnapshots { get; }
    DbSet<Notification> Notifications { get; }
    DbSet<OutboxMessage> OutboxMessages { get; }
    DbSet<AuditLog> AuditLogs { get; }
    DbSet<RefreshToken> RefreshTokens { get; }
    DbSet<EmailOtpCode> EmailOtpCodes { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken);
}
