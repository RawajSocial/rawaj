using MediatR;
using Microsoft.EntityFrameworkCore;
using Rawaj.Application.Common.Interfaces;
using Rawaj.Application.Common.Models;

namespace Rawaj.Application.Features.SocialAccounts.GetAuthorizationUrl;

public class GetAuthorizationUrlCommandHandler(
    IApplicationDbContext dbContext,
    ICurrentUserService currentUserService,
    ICurrentTenantContext currentTenantContext,
    IOAuthStateStore oAuthStateStore,
    IEnumerable<ISocialOAuthProvider> oAuthProviders)
    : IRequestHandler<GetAuthorizationUrlCommand, Result<GetAuthorizationUrlResponse>>
{
    public async Task<Result<GetAuthorizationUrlResponse>> Handle(
        GetAuthorizationUrlCommand request, CancellationToken cancellationToken)
    {
        var tenantId = currentTenantContext.TenantId!.Value;
        var userId = currentUserService.UserId!.Value;

        var brandProfileBelongsToTenant = await dbContext.TenantBrandProfiles
            .AnyAsync(b => b.Id == request.BrandProfileId && b.TenantId == tenantId, cancellationToken);
        if (!brandProfileBelongsToTenant)
        {
            return Result<GetAuthorizationUrlResponse>.Failure("Brand profile not found.");
        }

        var provider = oAuthProviders.FirstOrDefault(p => p.Platform == request.Platform);
        if (provider is null)
        {
            return Result<GetAuthorizationUrlResponse>.Failure(
                $"OAuth is not yet configured for {request.Platform}.");
        }

        var state = oAuthStateStore.Create(
            new OAuthStateContext(userId, tenantId, request.BrandProfileId, request.Platform, request.RedirectUri));

        var authorizationUrl = provider.BuildAuthorizationUrl(state, request.RedirectUri);

        return Result<GetAuthorizationUrlResponse>.Success(new GetAuthorizationUrlResponse(authorizationUrl));
    }
}
