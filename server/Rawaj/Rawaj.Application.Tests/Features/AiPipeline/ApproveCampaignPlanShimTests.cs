using Microsoft.EntityFrameworkCore;
using NSubstitute;
using Rawaj.Application.Common.Interfaces;
using Rawaj.Application.Features.AiPipeline.Services;
using Rawaj.Application.Features.Campaigns.ApproveCampaignPlan;
using Rawaj.Domain.Entities.AiOperations;
using Rawaj.Domain.Entities.Billing;
using Rawaj.Domain.Entities.Campaigns;
using Rawaj.Domain.Entities.Tenants;
using Rawaj.Domain.Enums;
using Rawaj.Persistence;
using Xunit;

namespace Rawaj.Application.Tests.Features.AiPipeline;

/// <summary>
/// C19's second follow-up item: approve-plan clearing a pipeline run's HumanApproval stage when one
/// exists, instead of only ever stamping the legacy column and leaving that stage stuck
/// AwaitingApproval forever.
/// </summary>
public class ApproveCampaignPlanShimTests
{
    [Fact]
    public async Task Approve_DelegatesToThePipelineService_WhenARunIsAwaitingApproval()
    {
        var world = await SeedAsync();
        world.Campaign.AiPlanJson = "{}";
        var run = new AiPipelineRun
        {
            Id = Guid.NewGuid(), TenantId = world.Tenant.Id, BrandProfileId = world.Brand.Id, CampaignId = world.Campaign.Id,
            TriggeredBy = world.UserId, Status = AiPipelineRunStatus.AwaitingApproval, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow
        };
        world.DbContext.AiPipelineRuns.Add(run);
        world.Campaign.CurrentPipelineRunId = run.Id;
        world.DbContext.AiPipelineStages.Add(new AiPipelineStage
        {
            Id = Guid.NewGuid(), RunId = run.Id, Kind = AiPipelineStageKind.HumanApproval,
            Status = AiPipelineStageStatus.AwaitingApproval, MaxAttempts = 1, CreatedAt = DateTime.UtcNow
        });
        await world.DbContext.SaveChangesAsync(CancellationToken.None);

        var approvalService = Substitute.For<IPipelineApprovalService>();
        approvalService.ApproveAsync(run, world.Campaign, Arg.Any<CancellationToken>())
            .Returns(callInfo =>
            {
                world.Campaign.PlanApprovedAt = DateTime.UtcNow;
                world.Campaign.Status = CampaignStatus.Active;
                return Rawaj.Application.Common.Models.Result<AiArtifact>.Success(new AiArtifact
                {
                    Id = Guid.NewGuid(), TenantId = world.Tenant.Id, BrandProfileId = world.Brand.Id, CampaignId = world.Campaign.Id,
                    Kind = AiArtifactKind.Strategy, Version = 1, IsCurrent = true, ContentJson = "{}", SchemaVersion = 1, CreatedAt = DateTime.UtcNow
                });
            });

        var handler = new ApproveCampaignPlanCommandHandler(
            world.DbContext, world.CurrentUserService(), world.CurrentTenantContext(), approvalService);

        var result = await handler.Handle(new ApproveCampaignPlanCommand(world.Campaign.Id), CancellationToken.None);

        Assert.True(result.Succeeded);
        await approvalService.Received(1).ApproveAsync(run, world.Campaign, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Approve_FallsBackToStampingTheColumnDirectly_WhenThereIsNoPipelineRun()
    {
        var world = await SeedAsync();
        world.Campaign.AiPlanJson = "{}"; // e.g. a legacy campaign generated before C19

        var approvalService = Substitute.For<IPipelineApprovalService>();
        var handler = new ApproveCampaignPlanCommandHandler(
            world.DbContext, world.CurrentUserService(), world.CurrentTenantContext(), approvalService);

        var result = await handler.Handle(new ApproveCampaignPlanCommand(world.Campaign.Id), CancellationToken.None);

        Assert.True(result.Succeeded);
        Assert.NotNull(world.Campaign.PlanApprovedAt);
        Assert.Equal(CampaignStatus.Active, world.Campaign.Status);
        await approvalService.DidNotReceive().ApproveAsync(
            Arg.Any<AiPipelineRun>(), Arg.Any<MarketingCampaign>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Approve_Fails_WhenNoStrategyExistsYet()
    {
        var world = await SeedAsync();
        var approvalService = Substitute.For<IPipelineApprovalService>();
        var handler = new ApproveCampaignPlanCommandHandler(
            world.DbContext, world.CurrentUserService(), world.CurrentTenantContext(), approvalService);

        var result = await handler.Handle(new ApproveCampaignPlanCommand(world.Campaign.Id), CancellationToken.None);

        Assert.False(result.Succeeded);
        Assert.Contains("Generate a strategy", result.ErrorMessage);
    }

    private sealed record World(AppDbContext DbContext, Tenant Tenant, TenantBrandProfile Brand, MarketingCampaign Campaign, Guid UserId)
    {
        public ICurrentUserService CurrentUserService()
        {
            var service = Substitute.For<ICurrentUserService>();
            service.UserId.Returns(UserId);
            return service;
        }

        public ICurrentTenantContext CurrentTenantContext()
        {
            var context = Substitute.For<ICurrentTenantContext>();
            context.TenantId.Returns(Tenant.Id);
            context.Role.Returns(TenantMemberRole.Owner);
            return context;
        }
    }

    private static async Task<World> SeedAsync()
    {
        var dbContext = TestDbContextFactory.Create();
        var now = DateTime.UtcNow;
        var planId = Guid.NewGuid();
        var subscriptionId = Guid.NewGuid();

        dbContext.SubscriptionPlans.Add(new SubscriptionPlan
        {
            Id = planId, Name = "Free", Cost = 0, Currency = "USD",
            MaxCampaignsMonthly = 100, IsActive = true, CreatedAt = now
        });

        dbContext.Subscriptions.Add(new Subscription
        {
            Id = subscriptionId, SubscriptionPlanId = planId, Status = SubscriptionStatus.Active,
            CurrentPeriodStart = now, CurrentPeriodEnd = now.AddMonths(1), CreatedAt = now
        });

        var userId = Guid.NewGuid();
        var tenant = new Tenant
        {
            Id = Guid.NewGuid(), Name = "Test Tenant", Subdomain = Guid.NewGuid().ToString("N"),
            TenantType = TenantType.Business, OwnerUserId = userId, SubscriptionId = subscriptionId,
            CoinBalance = 100_000, IsActive = true, IsActivated = true, CreatedAt = now, UpdatedAt = now
        };
        dbContext.Tenants.Add(tenant);

        var brand = new TenantBrandProfile
        {
            Id = Guid.NewGuid(), TenantId = tenant.Id, Name = "Qahwa",
            Status = BrandProfileStatus.Active, CreatedAt = now, UpdatedAt = now
        };
        dbContext.TenantBrandProfiles.Add(brand);

        var campaign = new MarketingCampaign
        {
            Id = Guid.NewGuid(), BrandProfileId = brand.Id, CreatedBy = Guid.NewGuid(),
            Name = "Summer Push", TargetPlatforms = ["Instagram"],
            Status = CampaignStatus.Draft, CreatedAt = now, UpdatedAt = now
        };
        dbContext.MarketingCampaigns.Add(campaign);

        await dbContext.SaveChangesAsync(CancellationToken.None);

        return new World(dbContext, tenant, brand, campaign, userId);
    }
}
