using MediatR;
using Rawaj.Application.Common.Interfaces;
using Rawaj.Application.Common.Models;
using Rawaj.Application.Features.Tenants.Common;

namespace Rawaj.Application.Features.Tenants.GetAccountSetup;

public class GetAccountSetupQueryHandler(IAccountSetupService accountSetupService, ICurrentUserService currentUserService)
    : IRequestHandler<GetAccountSetupQuery, Result<TenantAccountSetupResponse?>>
{
    public async Task<Result<TenantAccountSetupResponse?>> Handle(GetAccountSetupQuery request, CancellationToken cancellationToken)
    {
        if (currentUserService.UserId is not { } userId)
        {
            return Result<TenantAccountSetupResponse?>.Failure("Not authenticated.");
        }

        var result = await accountSetupService.GetAsync(userId, request.BrandProfileId, cancellationToken);

        return result.Outcome switch
        {
            GetAccountSetupOutcome.NotFound => Result<TenantAccountSetupResponse?>.Failure("Brand profile not found."),
            GetAccountSetupOutcome.Forbidden => Result<TenantAccountSetupResponse?>.Failure("You do not have permission to view this brand profile."),
            _ => Result<TenantAccountSetupResponse?>.Success(
                result.AccountSetup is null ? null : TenantAccountSetupResponse.FromEntity(result.AccountSetup, result.BrandInfo))
        };
    }
}
