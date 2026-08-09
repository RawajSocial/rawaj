using MediatR;
using Microsoft.EntityFrameworkCore;
using Rawaj.Application.Common.Interfaces;
using Rawaj.Application.Common.Models;
using Rawaj.Application.Common.Validation;
using Rawaj.Domain.ValueObjects;

namespace Rawaj.Application.Features.Brands.UpdateBrandProfile;

public class UpdateBrandProfileCommandHandler(IApplicationDbContext dbContext, ICurrentTenantContext currentTenantContext)
    : IRequestHandler<UpdateBrandProfileCommand, Result<UpdateBrandProfileResponse>>
{
    public async Task<Result<UpdateBrandProfileResponse>> Handle(UpdateBrandProfileCommand request, CancellationToken cancellationToken)
    {
        var tenantId = currentTenantContext.TenantId!.Value;

        var brandProfile = await dbContext.TenantBrandProfiles
            .FirstOrDefaultAsync(b => b.Id == request.BrandProfileId && b.TenantId == tenantId, cancellationToken);
        if (brandProfile is null)
        {
            return Result<UpdateBrandProfileResponse>.Failure("Brand profile not found.");
        }

        if (!string.IsNullOrWhiteSpace(request.Name))
        {
            brandProfile.Name = request.Name;
        }

        if (request.Description is not null)
        {
            brandProfile.Description = request.Description;
        }

        brandProfile.BrandInfo ??= new BrandInfo();

        if (request.Tones is not null) brandProfile.BrandInfo.Tones = request.Tones;
        if (request.Tagline is not null) brandProfile.BrandInfo.Tagline = request.Tagline;
        if (request.Industry is not null) brandProfile.BrandInfo.Industry = request.Industry;
        if (request.TargetAudience is not null) brandProfile.BrandInfo.TargetAudience = request.TargetAudience;
        if (request.Colors is not null) brandProfile.BrandInfo.Colors = request.Colors;
        if (request.LogoUrl is not null) brandProfile.BrandInfo.LogoUrl = request.LogoUrl;
        if (request.WebsiteUrl is not null) brandProfile.BrandInfo.WebsiteUrl = UrlNormalizer.EnsureScheme(request.WebsiteUrl);
        if (request.SupportedLanguages is not null) brandProfile.BrandInfo.SupportedLanguages = request.SupportedLanguages;
        if (request.Keywords is not null) brandProfile.BrandInfo.Keywords = request.Keywords;
        if (request.Location is not null) brandProfile.BrandInfo.Location = request.Location;
        if (request.Instagram is not null) brandProfile.BrandInfo.Instagram = request.Instagram;
        if (request.BusinessAge is not null) brandProfile.BrandInfo.BusinessAge = request.BusinessAge;
        if (request.BusinessEstablishDate.HasValue) brandProfile.BrandInfo.BusinessEstablishDate = request.BusinessEstablishDate;
        if (request.Stage is not null) brandProfile.BrandInfo.Stage = request.Stage;
        if (request.UniqueValue is not null) brandProfile.BrandInfo.UniqueValue = request.UniqueValue;
        if (request.PricePositioning is not null) brandProfile.BrandInfo.PricePositioning = request.PricePositioning;
        if (request.StorePresence is not null) brandProfile.BrandInfo.StorePresence = request.StorePresence;
        if (request.ExistingPlatforms is not null) brandProfile.BrandInfo.ExistingPlatforms = request.ExistingPlatforms;
        if (request.AdmiredBrand1 is not null) brandProfile.BrandInfo.AdmiredBrand1 = request.AdmiredBrand1;
        if (request.AdmiredBrand2 is not null) brandProfile.BrandInfo.AdmiredBrand2 = request.AdmiredBrand2;
        if (request.AdmiredBrand3 is not null) brandProfile.BrandInfo.AdmiredBrand3 = request.AdmiredBrand3;

        brandProfile.UpdatedAt = DateTime.UtcNow;

        await dbContext.SaveChangesAsync(cancellationToken);

        return Result<UpdateBrandProfileResponse>.Success(
            new UpdateBrandProfileResponse(
                brandProfile.Id,
                brandProfile.Name,
                brandProfile.Description,
                brandProfile.BrandInfo.Tones,
                brandProfile.Status,
                brandProfile.BrandInfo.IsDefault));
    }
}
