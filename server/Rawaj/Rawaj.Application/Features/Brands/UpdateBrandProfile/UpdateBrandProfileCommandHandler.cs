using MediatR;
using Microsoft.EntityFrameworkCore;
using Rawaj.Application.Common.Interfaces;
using Rawaj.Application.Common.Models;
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

        if (request.BrandVoice.HasValue)
        {
            brandProfile.BrandVoice = request.BrandVoice;
        }

        brandProfile.BrandInfo ??= new BrandInfo();

        if (request.Tagline is not null) brandProfile.BrandInfo.Tagline = request.Tagline;
        if (request.Industry is not null) brandProfile.BrandInfo.Industry = request.Industry;
        if (request.TargetAudience is not null) brandProfile.BrandInfo.TargetAudience = request.TargetAudience;
        if (request.Colors is not null) brandProfile.BrandInfo.Colors = request.Colors;
        if (request.LogoUrl is not null) brandProfile.BrandInfo.LogoUrl = request.LogoUrl;
        if (request.WebsiteUrl is not null) brandProfile.BrandInfo.WebsiteUrl = request.WebsiteUrl;
        if (request.SupportedLanguages is not null) brandProfile.BrandInfo.SupportedLanguages = request.SupportedLanguages;
        if (request.Keywords is not null) brandProfile.BrandInfo.Keywords = request.Keywords;

        brandProfile.UpdatedAt = DateTime.UtcNow;

        await dbContext.SaveChangesAsync(cancellationToken);

        return Result<UpdateBrandProfileResponse>.Success(
            new UpdateBrandProfileResponse(
                brandProfile.Id,
                brandProfile.Name,
                brandProfile.Description,
                brandProfile.BrandVoice,
                brandProfile.Status,
                brandProfile.BrandInfo.IsDefault));
    }
}
