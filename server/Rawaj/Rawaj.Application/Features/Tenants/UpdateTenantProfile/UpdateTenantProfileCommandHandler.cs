using MediatR;
using Microsoft.EntityFrameworkCore;
using Rawaj.Application.Common.Interfaces;
using Rawaj.Application.Common.Models;
using Rawaj.Domain.ValueObjects;

namespace Rawaj.Application.Features.Tenants.UpdateTenantProfile;

public class UpdateTenantProfileCommandHandler(
    IApplicationDbContext dbContext, ICurrentTenantContext currentTenantContext)
    : IRequestHandler<UpdateTenantProfileCommand, Result<UpdateTenantProfileResponse>>
{
    private const int ActivationRewardCoins = 50;

    public async Task<Result<UpdateTenantProfileResponse>> Handle(
        UpdateTenantProfileCommand request, CancellationToken cancellationToken)
    {
        var tenantId = currentTenantContext.TenantId!.Value;

        var tenant = await dbContext.Tenants.FirstAsync(t => t.Id == tenantId, cancellationToken);

        var profile = tenant.TenantProfile ?? new TenantProfile();
        profile.Phone = request.Phone ?? profile.Phone;
        profile.Industry = request.Industry ?? profile.Industry;
        profile.Country = request.Country ?? profile.Country;
        profile.City = request.City ?? profile.City;
        profile.Website = request.Website ?? profile.Website;
        tenant.TenantProfile = profile;

        var isNowComplete = !string.IsNullOrWhiteSpace(profile.Phone)
            && !string.IsNullOrWhiteSpace(profile.Industry)
            && !string.IsNullOrWhiteSpace(profile.Country)
            && !string.IsNullOrWhiteSpace(profile.City);

        var rewardGranted = false;
        if (isNowComplete && !tenant.IsActivated)
        {
            tenant.IsActivated = true;
            tenant.CoinBalance += ActivationRewardCoins;
            rewardGranted = true;
        }

        tenant.UpdatedAt = DateTime.UtcNow;

        await dbContext.SaveChangesAsync(cancellationToken);

        return Result<UpdateTenantProfileResponse>.Success(
            new UpdateTenantProfileResponse(tenant.Id, tenant.IsActivated, tenant.CoinBalance, rewardGranted));
    }
}
