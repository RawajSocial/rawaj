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

    public async Task<BrandImageResult> GetAuthorizedBrandProfileAsync(Guid userId, Guid brandProfileId, CancellationToken cancellationToken)
    {
        var brandProfile = await dbContext.TenantBrandProfiles
            .FirstOrDefaultAsync(b => b.Id == brandProfileId, cancellationToken);

        if (brandProfile is null)
        {
            return new BrandImageResult(BrandImageOutcome.NotFound, null);
        }

        var member = await dbContext.TenantMembers
            .FirstOrDefaultAsync(m => m.TenantId == brandProfile.TenantId && m.UserId == userId, cancellationToken);

        if (member is null || member.InvitationStatus != InvitationStatus.Accepted ||
            (member.Role != TenantMemberRole.Owner && member.Role != TenantMemberRole.Admin))
        {
            return new BrandImageResult(BrandImageOutcome.Forbidden, null);
        }

        var hasBrandAccess = await dbContext.TenantMemberBrandAccesses
            .AnyAsync(a => a.TenantMemberId == member.Id && a.BrandProfileId == brandProfileId, cancellationToken);

        if (!hasBrandAccess)
        {
            return new BrandImageResult(BrandImageOutcome.Forbidden, null);
        }

        return new BrandImageResult(BrandImageOutcome.Updated, brandProfile);
    }

    public async Task<TenantBrandProfile> SetBrandImageAsync(Guid brandProfileId, string? imageUrl, CancellationToken cancellationToken)
    {
        var brandProfile = await dbContext.TenantBrandProfiles.FirstAsync(b => b.Id == brandProfileId, cancellationToken);

        brandProfile.ImageUrl = imageUrl;
        brandProfile.UpdatedAt = DateTime.UtcNow;

        await dbContext.SaveChangesAsync(cancellationToken);

        return brandProfile;
    }

    public async Task<List<TenantBrandProfile>> GetAccessibleBrandProfilesAsync(Guid userId, CancellationToken cancellationToken)
    {
        var memberIds = await dbContext.TenantMembers
            .Where(m => m.UserId == userId && m.InvitationStatus == InvitationStatus.Accepted)
            .Select(m => m.Id)
            .ToListAsync(cancellationToken);

        var brandProfileIds = await dbContext.TenantMemberBrandAccesses
            .Where(a => memberIds.Contains(a.TenantMemberId))
            .Select(a => a.BrandProfileId)
            .Distinct()
            .ToListAsync(cancellationToken);

        return await dbContext.TenantBrandProfiles
            .Where(b => brandProfileIds.Contains(b.Id))
            .ToListAsync(cancellationToken);
    }

    public async Task<UpdateBrandProfileResult> UpdateBrandProfileAsync(
        Guid userId,
        Guid brandProfileId,
        string name,
        string? description,
        BrandVoice? brandVoice,
        BrandInfo? brandInfo,
        CancellationToken cancellationToken)
    {
        var authorization = await GetAuthorizedBrandProfileAsync(userId, brandProfileId, cancellationToken);

        if (authorization.Outcome is BrandImageOutcome.NotFound)
        {
            return new UpdateBrandProfileResult(UpdateBrandProfileOutcome.NotFound, null);
        }

        if (authorization.Outcome is BrandImageOutcome.Forbidden)
        {
            return new UpdateBrandProfileResult(UpdateBrandProfileOutcome.Forbidden, null);
        }

        var brandProfile = authorization.BrandProfile!;

        if (brandInfo is not null)
        {
            brandInfo.IsDefault = brandProfile.BrandInfo?.IsDefault ?? false;
        }

        brandProfile.Name = name;
        brandProfile.Description = description;
        brandProfile.BrandVoice = brandVoice;
        brandProfile.BrandInfo = brandInfo;
        brandProfile.UpdatedAt = DateTime.UtcNow;

        await dbContext.SaveChangesAsync(cancellationToken);

        return new UpdateBrandProfileResult(UpdateBrandProfileOutcome.Updated, brandProfile);
    }

    public async Task<ArchiveBrandProfileResult> ArchiveBrandProfileAsync(Guid userId, Guid brandProfileId, CancellationToken cancellationToken)
    {
        var authorization = await GetAuthorizedBrandProfileAsync(userId, brandProfileId, cancellationToken);

        if (authorization.Outcome is BrandImageOutcome.NotFound)
        {
            return new ArchiveBrandProfileResult(ArchiveBrandProfileOutcome.NotFound, null);
        }

        if (authorization.Outcome is BrandImageOutcome.Forbidden)
        {
            return new ArchiveBrandProfileResult(ArchiveBrandProfileOutcome.Forbidden, null);
        }

        var brandProfile = authorization.BrandProfile!;
        brandProfile.Status = BrandProfileStatus.Archived;
        brandProfile.UpdatedAt = DateTime.UtcNow;

        await dbContext.SaveChangesAsync(cancellationToken);

        return new ArchiveBrandProfileResult(ArchiveBrandProfileOutcome.Archived, brandProfile);
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
