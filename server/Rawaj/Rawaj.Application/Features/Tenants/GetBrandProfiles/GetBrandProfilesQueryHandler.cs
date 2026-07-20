using MediatR;
using Rawaj.Application.Common.Interfaces;
using Rawaj.Application.Common.Models;
using Rawaj.Application.Features.Tenants.Common;

namespace Rawaj.Application.Features.Tenants.GetBrandProfiles;

public class GetBrandProfilesQueryHandler(IBrandProfileService brandProfileService, ICurrentUserService currentUserService)
    : IRequestHandler<GetBrandProfilesQuery, Result<List<TenantBrandProfileResponse>>>
{
    public async Task<Result<List<TenantBrandProfileResponse>>> Handle(GetBrandProfilesQuery request, CancellationToken cancellationToken)
    {
        if (currentUserService.UserId is not { } userId)
        {
            return Result<List<TenantBrandProfileResponse>>.Failure("Not authenticated.");
        }

        var brandProfiles = await brandProfileService.GetAccessibleBrandProfilesAsync(userId, cancellationToken);

        return Result<List<TenantBrandProfileResponse>>.Success(
            brandProfiles.Select(TenantBrandProfileResponse.FromEntity).ToList());
    }
}
