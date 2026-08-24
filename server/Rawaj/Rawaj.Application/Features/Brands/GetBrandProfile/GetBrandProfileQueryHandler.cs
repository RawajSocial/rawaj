using MediatR;
using Microsoft.EntityFrameworkCore;
using Rawaj.Application.Common.Interfaces;
using Rawaj.Application.Common.Models;

namespace Rawaj.Application.Features.Brands.GetBrandProfile;

public class GetBrandProfileQueryHandler(IApplicationDbContext dbContext, ICurrentTenantContext currentTenantContext)
    : IRequestHandler<GetBrandProfileQuery, Result<GetBrandProfileResponse>>
{
    public async Task<Result<GetBrandProfileResponse>> Handle(GetBrandProfileQuery request, CancellationToken cancellationToken)
    {
        var tenantId = currentTenantContext.TenantId!.Value;

        var brand = await dbContext.TenantBrandProfiles
            .FirstOrDefaultAsync(b => b.Id == request.BrandProfileId && b.TenantId == tenantId, cancellationToken);
        if (brand is null)
        {
            return Result<GetBrandProfileResponse>.Failure("Brand profile not found.");
        }

        return Result<GetBrandProfileResponse>.Success(new GetBrandProfileResponse(
            brand.Id,
            brand.Name,
            brand.Description,
            brand.BrandInfo?.Tones ?? [],
            brand.Status,
            brand.BrandInfo?.IsDefault ?? false,
            brand.BrandInfo?.Tagline,
            brand.BrandInfo?.Industry,
            brand.BrandInfo?.TargetAudience,
            brand.BrandInfo?.Colors ?? [],
            brand.BrandInfo?.LogoUrl,
            brand.BrandInfo?.WebsiteUrl,
            brand.BrandInfo?.SupportedLanguages ?? [],
            brand.BrandInfo?.Keywords ?? [],
            brand.BrandInfo?.Location,
            brand.BrandInfo?.Instagram,
            brand.BrandInfo?.BusinessAge,
            brand.BrandInfo?.BusinessEstablishDate,
            brand.BrandInfo?.Stage,
            brand.BrandInfo?.UniqueValue,
            brand.BrandInfo?.PricePositioning,
            brand.BrandInfo?.StorePresence,
            brand.BrandInfo?.ExistingPlatforms ?? [],
            brand.BrandInfo?.AdmiredBrand1,
            brand.BrandInfo?.AdmiredBrand2,
            brand.BrandInfo?.AdmiredBrand3));
    }
}
