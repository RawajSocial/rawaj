using MediatR;
using Rawaj.Application.Common.Interfaces;
using Rawaj.Application.Common.Models;
using Rawaj.Application.Features.Tenants.Common;

namespace Rawaj.Application.Features.Tenants.GetBrandProfileById;

public class GetBrandProfileByIdQueryHandler(IBrandProfileService brandProfileService, ICurrentUserService currentUserService)
    : IRequestHandler<GetBrandProfileByIdQuery, Result<TenantBrandProfileResponse>>
{
    public async Task<Result<TenantBrandProfileResponse>> Handle(GetBrandProfileByIdQuery request, CancellationToken cancellationToken)
    {
        if (currentUserService.UserId is not { } userId)
        {
            return Result<TenantBrandProfileResponse>.Failure("Not authenticated.");
        }

        var authorization = await brandProfileService.GetAuthorizedBrandProfileAsync(userId, request.BrandProfileId, cancellationToken);

        return authorization.Outcome switch
        {
            BrandImageOutcome.NotFound => Result<TenantBrandProfileResponse>.Failure("Brand profile not found."),
            BrandImageOutcome.Forbidden => Result<TenantBrandProfileResponse>.Failure("You do not have permission to view this brand profile."),
            _ => Result<TenantBrandProfileResponse>.Success(TenantBrandProfileResponse.FromEntity(authorization.BrandProfile!))
        };
    }
}
