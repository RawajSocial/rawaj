using MediatR;
using Rawaj.Application.Common.Interfaces;
using Rawaj.Application.Common.Models;
using Rawaj.Application.Features.Tenants.Common;
using Rawaj.Domain.ValueObjects;

namespace Rawaj.Application.Features.Tenants.CreateBrandProfile;

public class CreateBrandProfileCommandHandler(
    IBrandProfileService brandProfileService,
    ICurrentUserService currentUserService)
    : IRequestHandler<CreateBrandProfileCommand, Result<TenantBrandProfileResponse>>
{
    public async Task<Result<TenantBrandProfileResponse>> Handle(CreateBrandProfileCommand request, CancellationToken cancellationToken)
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

        var result = await brandProfileService.CreateFirstBrandProfileAsync(
            userId, request.Name, request.Description, request.BrandVoice, brandInfo, cancellationToken);

        return result.Outcome switch
        {
            CreateBrandProfileOutcome.TenantNotFound =>
                Result<TenantBrandProfileResponse>.Failure("No tenant found for the current user."),
            CreateBrandProfileOutcome.LimitReached =>
                Result<TenantBrandProfileResponse>.Failure("Your subscription plan does not allow any more brand profiles."),
            _ => Result<TenantBrandProfileResponse>.Success(TenantBrandProfileResponse.FromEntity(result.BrandProfile!)),
        };
    }
}
