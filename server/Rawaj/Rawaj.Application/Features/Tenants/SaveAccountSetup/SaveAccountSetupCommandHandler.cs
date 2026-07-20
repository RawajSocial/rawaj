using MediatR;
using Rawaj.Application.Common.Interfaces;
using Rawaj.Application.Common.Models;
using Rawaj.Application.Features.Tenants.Common;

namespace Rawaj.Application.Features.Tenants.SaveAccountSetup;

public class SaveAccountSetupCommandHandler(IAccountSetupService accountSetupService, ICurrentUserService currentUserService)
    : IRequestHandler<SaveAccountSetupCommand, Result<TenantAccountSetupResponse>>
{
    public async Task<Result<TenantAccountSetupResponse>> Handle(SaveAccountSetupCommand request, CancellationToken cancellationToken)
    {
        if (currentUserService.UserId is not { } userId)
        {
            return Result<TenantAccountSetupResponse>.Failure("Not authenticated.");
        }

        var fields = new AccountSetupFields(
            request.AccountType, request.Country, request.City, request.Website,
            request.Facebook, request.Instagram, request.Youtube, request.TikTok, request.LinkedIn, request.X, request.Snapchat,
            request.AgencyName, request.AgencyPhone, request.AgencyCountryCode, request.AgencySize, request.ActiveClients, request.PrimaryServices,
            request.BusinessName, request.BusinessPhone, request.BusinessCountryCode, request.Industry, request.BusinessSize, request.HearAboutUs);

        var result = await accountSetupService.SaveAsync(userId, request.BrandProfileId, fields, cancellationToken);

        return result.Outcome switch
        {
            SaveAccountSetupOutcome.NotFound => Result<TenantAccountSetupResponse>.Failure("Brand profile not found."),
            SaveAccountSetupOutcome.Forbidden => Result<TenantAccountSetupResponse>.Failure("You do not have permission to manage this brand profile."),
            _ => Result<TenantAccountSetupResponse>.Success(TenantAccountSetupResponse.FromEntity(result.AccountSetup!, result.BrandInfo))
        };
    }
}
