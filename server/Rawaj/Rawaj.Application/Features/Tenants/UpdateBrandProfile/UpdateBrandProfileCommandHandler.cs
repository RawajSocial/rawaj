using MediatR;
using Rawaj.Application.Common.Interfaces;
using Rawaj.Application.Common.Models;
using Rawaj.Application.Features.Tenants.Common;
using Rawaj.Domain.ValueObjects;

namespace Rawaj.Application.Features.Tenants.UpdateBrandProfile;

public class UpdateBrandProfileCommandHandler(IBrandProfileService brandProfileService, ICurrentUserService currentUserService)
    : IRequestHandler<UpdateBrandProfileCommand, Result<TenantBrandProfileResponse>>
{
    public async Task<Result<TenantBrandProfileResponse>> Handle(UpdateBrandProfileCommand request, CancellationToken cancellationToken)
    {
        if (currentUserService.UserId is not { } userId)
        {
            return Result<TenantBrandProfileResponse>.Failure("Not authenticated.");
        }

        var brandInfo = new BrandInfo
        {
            Tagline = request.Tagline,
            Industry = request.Industry,
            TargetAudience = request.TargetAudience,
            Colors = request.Colors ?? [],
            LogoUrl = request.LogoUrl,
            WebsiteUrl = request.WebsiteUrl,
            SupportedLanguages = request.SupportedLanguages ?? [],
            Keywords = request.Keywords ?? [],
        };

        var result = await brandProfileService.UpdateBrandProfileAsync(
            userId, request.BrandProfileId, request.Name, request.Description, request.BrandVoice, brandInfo, cancellationToken);

        return result.Outcome switch
        {
            UpdateBrandProfileOutcome.NotFound => Result<TenantBrandProfileResponse>.Failure("Brand profile not found."),
            UpdateBrandProfileOutcome.Forbidden => Result<TenantBrandProfileResponse>.Failure("You do not have permission to manage this brand profile."),
            _ => Result<TenantBrandProfileResponse>.Success(TenantBrandProfileResponse.FromEntity(result.BrandProfile!))
        };
    }
}
