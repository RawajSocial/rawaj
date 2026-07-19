using MediatR;
using Microsoft.Extensions.Logging;
using Rawaj.Application.Common.Interfaces;
using Rawaj.Application.Common.Models;
using Rawaj.Application.Features.Tenants.Common;

namespace Rawaj.Application.Features.Tenants.RemoveBrandImage;

public class RemoveBrandImageCommandHandler(
    IBrandProfileService brandProfileService,
    IStorageService storageService,
    ICurrentUserService currentUserService,
    ILogger<RemoveBrandImageCommandHandler> logger)
    : IRequestHandler<RemoveBrandImageCommand, Result<TenantBrandProfileResponse>>
{
    public async Task<Result<TenantBrandProfileResponse>> Handle(RemoveBrandImageCommand request, CancellationToken cancellationToken)
    {
        if (currentUserService.UserId is not { } userId)
        {
            return Result<TenantBrandProfileResponse>.Failure("Not authenticated.");
        }

        var authorization = await brandProfileService.GetAuthorizedBrandProfileAsync(userId, request.BrandProfileId, cancellationToken);

        if (authorization.Outcome is BrandImageOutcome.NotFound)
        {
            return Result<TenantBrandProfileResponse>.Failure("Brand profile not found.");
        }

        if (authorization.Outcome is BrandImageOutcome.Forbidden)
        {
            return Result<TenantBrandProfileResponse>.Failure("You do not have permission to manage this brand profile.");
        }

        var brandProfile = authorization.BrandProfile!;

        if (string.IsNullOrEmpty(brandProfile.ImageUrl))
        {
            return Result<TenantBrandProfileResponse>.Success(TenantBrandProfileResponse.FromEntity(brandProfile));
        }

        try
        {
            await storageService.DeleteAsync(brandProfile.ImageUrl, cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to delete brand image {ImageUrl} for brand profile {BrandProfileId}",
                brandProfile.ImageUrl, request.BrandProfileId);
        }

        var updated = await brandProfileService.SetBrandImageAsync(request.BrandProfileId, null, cancellationToken);

        return Result<TenantBrandProfileResponse>.Success(TenantBrandProfileResponse.FromEntity(updated));
    }
}
