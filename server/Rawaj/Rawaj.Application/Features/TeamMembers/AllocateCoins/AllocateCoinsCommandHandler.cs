using MediatR;
using Microsoft.EntityFrameworkCore;
using Rawaj.Application.Common.Interfaces;
using Rawaj.Application.Common.Models;
using Rawaj.Application.Common.Policies;
using Rawaj.Domain.Enums;

namespace Rawaj.Application.Features.TeamMembers.AllocateCoins;

public class AllocateCoinsCommandHandler(
    IApplicationDbContext dbContext, ICurrentUserService currentUserService, ICurrentTenantContext currentTenantContext)
    : IRequestHandler<AllocateCoinsCommand, Result<AllocateCoinsResponse>>
{
    public async Task<Result<AllocateCoinsResponse>> Handle(AllocateCoinsCommand request, CancellationToken cancellationToken)
    {
        var tenantId = currentTenantContext.TenantId!.Value;

        var member = await dbContext.TenantMembers
            .FirstOrDefaultAsync(m => m.Id == request.TenantMemberId && m.TenantId == tenantId, cancellationToken);
        if (member is null)
        {
            return Result<AllocateCoinsResponse>.Failure("Team member not found.");
        }

        if (member.Role.HasAtLeast(TenantMemberRole.Admin))
        {
            return Result<AllocateCoinsResponse>.Failure("Owners and admins spend directly from the organization's balance and don't need a separate allocation.");
        }

        if (request.NewAllocation < member.SpentCoins)
        {
            return Result<AllocateCoinsResponse>.Failure(
                $"This member has already spent {member.SpentCoins} coins — the new allocation can't be lower than that.");
        }

        var tenant = await dbContext.Tenants.FirstAsync(t => t.Id == tenantId, cancellationToken);

        var previousAllocation = member.AllocatedCoins;
        var delta = request.NewAllocation - previousAllocation;
        if (delta > 0 && tenant.CoinBalance < delta)
        {
            return Result<AllocateCoinsResponse>.Failure("Your organization doesn't have enough coins for this allocation.");
        }

        tenant.CoinBalance -= delta;
        member.AllocatedCoins = request.NewAllocation;

        AuditLogger.Log(
            dbContext, tenantId, currentUserService.UserId, "team.coins_allocated",
            message: $"Allocated {request.NewAllocation} coins (was {previousAllocation}).",
            entityType: "tenant_member", entityId: member.Id);

        await dbContext.SaveChangesAsync(cancellationToken);

        return Result<AllocateCoinsResponse>.Success(
            new AllocateCoinsResponse(member.Id, member.AllocatedCoins, member.SpentCoins, tenant.CoinBalance));
    }
}
