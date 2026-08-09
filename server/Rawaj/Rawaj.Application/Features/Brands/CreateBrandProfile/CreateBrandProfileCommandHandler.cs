using MediatR;
using Microsoft.EntityFrameworkCore;
using Rawaj.Application.Common.Interfaces;
using Rawaj.Application.Common.Models;
using Rawaj.Application.Common.Validation;
using Rawaj.Domain.Entities.Tenants;
using Rawaj.Domain.Enums;
using Rawaj.Domain.ValueObjects;

namespace Rawaj.Application.Features.Brands.CreateBrandProfile;

public class CreateBrandProfileCommandHandler(IApplicationDbContext dbContext, ICurrentTenantContext currentTenantContext)
    : IRequestHandler<CreateBrandProfileCommand, Result<CreateBrandProfileResponse>>
{
    public async Task<Result<CreateBrandProfileResponse>> Handle(CreateBrandProfileCommand request, CancellationToken cancellationToken)
    {
        var tenantId = currentTenantContext.TenantId!.Value;

        var maxBrands = await (
            from tenant in dbContext.Tenants
            join subscription in dbContext.Subscriptions on tenant.SubscriptionId equals subscription.Id
            join plan in dbContext.SubscriptionPlans on subscription.SubscriptionPlanId equals plan.Id
            where tenant.Id == tenantId
            select plan.MaxBrands + tenant.ExtraBrandsPurchased
        ).FirstAsync(cancellationToken);

        var existingBrandCount = await dbContext.TenantBrandProfiles
            .CountAsync(b => b.TenantId == tenantId, cancellationToken);

        if (existingBrandCount >= maxBrands)
        {
            return Result<CreateBrandProfileResponse>.Failure("AGENCY_UPGRADE_REQUIRED");
        }

        var now = DateTime.UtcNow;

        var brandProfile = new TenantBrandProfile
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            Name = request.Name,
            Description = request.Description,
            Status = BrandProfileStatus.Active,
            BrandInfo = new BrandInfo
            {
                Tagline = request.Tagline,
                Industry = request.Industry,
                TargetAudience = request.TargetAudience,
                Colors = request.Colors ?? [],
                LogoUrl = request.LogoUrl,
                WebsiteUrl = UrlNormalizer.EnsureScheme(request.WebsiteUrl),
                SupportedLanguages = request.SupportedLanguages ?? [],
                Keywords = request.Keywords ?? [],
                Location = request.Location,
                IsDefault = existingBrandCount == 0,
                Tones = request.Tones ?? [],
                Instagram = request.Instagram,
                BusinessAge = request.BusinessAge,
                BusinessEstablishDate = request.BusinessEstablishDate,
                Stage = request.Stage,
                UniqueValue = request.UniqueValue,
                PricePositioning = request.PricePositioning,
                StorePresence = request.StorePresence,
                ExistingPlatforms = request.ExistingPlatforms ?? [],
                AdmiredBrand1 = request.AdmiredBrand1,
                AdmiredBrand2 = request.AdmiredBrand2,
                AdmiredBrand3 = request.AdmiredBrand3
            },
            CreatedAt = now,
            UpdatedAt = now
        };

        dbContext.TenantBrandProfiles.Add(brandProfile);
        await dbContext.SaveChangesAsync(cancellationToken);

        return Result<CreateBrandProfileResponse>.Success(
            new CreateBrandProfileResponse(
                brandProfile.Id,
                brandProfile.TenantId,
                brandProfile.Name,
                brandProfile.Status,
                brandProfile.BrandInfo.IsDefault));
    }
}
