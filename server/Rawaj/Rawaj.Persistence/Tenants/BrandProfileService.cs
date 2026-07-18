using Microsoft.EntityFrameworkCore;
using Rawaj.Application.Common.Interfaces;
using Rawaj.Application.Common.Models;
using Rawaj.Domain.Entities.Tenants;
using Rawaj.Domain.Enums;
using Rawaj.Domain.ValueObjects;

namespace Rawaj.Persistence.Tenants;

public class BrandProfileService(AppDbContext dbContext) : IBrandProfileService
{
    public async Task<CreateBrandProfileResult> CreateFirstBrandProfileAsync(
        Guid ownerUserId, string name, string? description, BrandVoice? brandVoice, BrandInfo? brandInfo, CancellationToken cancellationToken)
    {
        var tenant = await dbContext.Tenants.FirstOrDefaultAsync(t => t.OwnerUserId == ownerUserId, cancellationToken);
        if (tenant is null)
        {
            return new CreateBrandProfileResult(CreateBrandProfileOutcome.TenantNotFound, null);
        }

        var ownerMember = await dbContext.TenantMembers
            .FirstAsync(m => m.TenantId == tenant.Id && m.UserId == ownerUserId, cancellationToken);

        var maxBrands = await dbContext.Subscriptions
            .Where(s => s.Id == tenant.SubscriptionId)
            .Select(s => s.SubscriptionPlan.MaxBrands)
            .FirstAsync(cancellationToken);

        var existingProfiles = await dbContext.TenantBrandProfiles
            .Where(b => b.TenantId == tenant.Id)
            .ToListAsync(cancellationToken);

        if (existingProfiles.Count >= maxBrands)
        {
            if (existingProfiles.Count == 1)
            {
                var soleProfile = existingProfiles[0];
                await EnsureOwnerHasBrandAccessAsync(ownerMember.Id, soleProfile.Id, cancellationToken);
                return new CreateBrandProfileResult(CreateBrandProfileOutcome.AlreadyExisted, soleProfile);
            }

            return new CreateBrandProfileResult(CreateBrandProfileOutcome.LimitReached, null);
        }

        var now = DateTime.UtcNow;

        if (brandInfo is not null)
        {
            brandInfo.IsDefault = existingProfiles.Count == 0;
        }

        var brandProfile = new TenantBrandProfile
        {
            Id = Guid.NewGuid(),
            TenantId = tenant.Id,
            Name = name,
            Description = description,
            BrandVoice = brandVoice,
            Status = BrandProfileStatus.Active,
            BrandInfo = brandInfo,
            CreatedAt = now,
            UpdatedAt = now,
        };

        var brandAccess = new TenantMemberBrandAccess
        {
            Id = Guid.NewGuid(),
            TenantMemberId = ownerMember.Id,
            BrandProfileId = brandProfile.Id,
            CreatedAt = now,
        };

        dbContext.TenantBrandProfiles.Add(brandProfile);
        dbContext.TenantMemberBrandAccesses.Add(brandAccess);
        await dbContext.SaveChangesAsync(cancellationToken);

        return new CreateBrandProfileResult(CreateBrandProfileOutcome.Created, brandProfile);
    }

    private async Task EnsureOwnerHasBrandAccessAsync(Guid ownerMemberId, Guid brandProfileId, CancellationToken cancellationToken)
    {
        var alreadyGranted = await dbContext.TenantMemberBrandAccesses
            .AnyAsync(a => a.TenantMemberId == ownerMemberId && a.BrandProfileId == brandProfileId, cancellationToken);

        if (alreadyGranted)
        {
            return;
        }

        dbContext.TenantMemberBrandAccesses.Add(new TenantMemberBrandAccess
        {
            Id = Guid.NewGuid(),
            TenantMemberId = ownerMemberId,
            BrandProfileId = brandProfileId,
            CreatedAt = DateTime.UtcNow,
        });
        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
