namespace Rawaj.Application.Features.Tenants.UpdateTenantProfile;

public record UpdateTenantProfileResponse(
    Guid TenantId,
    bool IsActivated,
    int CoinBalance,
    bool ActivationRewardGranted);
