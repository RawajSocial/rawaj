namespace Rawaj.Application.Features.TeamMembers.AllocateCoins;

public record AllocateCoinsResponse(Guid TenantMemberId, int AllocatedCoins, int SpentCoins, int TenantCoinBalance);
