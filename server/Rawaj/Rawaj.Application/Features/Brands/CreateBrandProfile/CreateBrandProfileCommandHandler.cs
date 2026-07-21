using MediatR;
using Microsoft.EntityFrameworkCore;
using Rawaj.Application.Common.Interfaces;
using Rawaj.Application.Common.Models;
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
            select plan.MaxBrands
        ).FirstAsync(cancellationToken);

        var existingBrandCount = await dbContext.TenantBrandProfiles
            .CountAsync(b => b.TenantId == tenantId, cancellationToken);

        if (existingBrandCount >= maxBrands)
        {
            return Result<CreateBrandProfileResponse>.Failure(
                $"Your subscription plan allows a maximum of {maxBrands} brand profile(s). Upgrade to add more.");
        }

        var now = DateTime.UtcNow;

        var brandProfile = new TenantBrandProfile
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            Name = request.Name,
            Description = request.Description,
            BrandVoice = request.BrandVoice,
            Status = BrandProfileStatus.Active,
            BrandInfo = new BrandInfo
            {
                Tagline = request.Tagline,
                Industry = request.Industry,
                TargetAudience = request.TargetAudience,
                Colors = request.Colors ?? [],
                LogoUrl = request.LogoUrl,
                WebsiteUrl = request.WebsiteUrl,
                SupportedLanguages = request.SupportedLanguages ?? [],
                Keywords = request.Keywords ?? [],
                IsDefault = existingBrandCount == 0
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
