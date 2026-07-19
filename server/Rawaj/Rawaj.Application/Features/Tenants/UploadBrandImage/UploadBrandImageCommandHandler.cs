using MediatR;
using Microsoft.Extensions.Logging;
using Rawaj.Application.Common.Interfaces;
using Rawaj.Application.Common.Models;
using Rawaj.Application.Features.Tenants.Common;

namespace Rawaj.Application.Features.Tenants.UploadBrandImage;

public class UploadBrandImageCommandHandler(
    IBrandProfileService brandProfileService,
    IStorageService storageService,
    ICurrentUserService currentUserService,
    ILogger<UploadBrandImageCommandHandler> logger)
    : IRequestHandler<UploadBrandImageCommand, Result<TenantBrandProfileResponse>>
{
    public async Task<Result<TenantBrandProfileResponse>> Handle(UploadBrandImageCommand request, CancellationToken cancellationToken)
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
        var oldImageUrl = brandProfile.ImageUrl;

        var newImageUrl = await storageService.UploadAsync(
            request.Content, request.FileName, request.ContentType, $"brand-images/{brandProfile.TenantId}", cancellationToken);

        var updated = await brandProfileService.SetBrandImageAsync(request.BrandProfileId, newImageUrl, cancellationToken);

        if (!string.IsNullOrEmpty(oldImageUrl))
        {
            try
            {
                await storageService.DeleteAsync(oldImageUrl, cancellationToken);
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Failed to delete previous brand image {ImageUrl} for brand profile {BrandProfileId}",
                    oldImageUrl, request.BrandProfileId);
            }
        }

        return Result<TenantBrandProfileResponse>.Success(TenantBrandProfileResponse.FromEntity(updated));
    }
}
