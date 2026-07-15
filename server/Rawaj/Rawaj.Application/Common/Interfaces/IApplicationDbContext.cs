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
    DbSet<TenantMember> TenantMembers { get; }
    DbSet<TenantBrandProfile> TenantBrandProfiles { get; }
    DbSet<SubscriptionPlan> SubscriptionPlans { get; }
    DbSet<Subscription> Subscriptions { get; }
    DbSet<MarketingCampaign> MarketingCampaigns { get; }
    DbSet<ContentItem> ContentItems { get; }
    DbSet<ContentRevision> ContentRevisions { get; }
    DbSet<VisualAsset> VisualAssets { get; }
    DbSet<AiJob> AiJobs { get; }
    DbSet<Competitor> Competitors { get; }
    DbSet<RagDocument> RagDocuments { get; }
    DbSet<SocialAccount> SocialAccounts { get; }
    DbSet<ScheduledPost> ScheduledPosts { get; }
    DbSet<PostAnalytics> PostAnalytics { get; }
    DbSet<Notification> Notifications { get; }
    DbSet<RefreshToken> RefreshTokens { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken);
}
