using MediatR;
using Microsoft.EntityFrameworkCore;
using Rawaj.Application.Common.Interfaces;
using Rawaj.Application.Common.Models;
using Rawaj.Domain.Enums;

namespace Rawaj.Application.Features.TeamMembers.DeclineInvite;

public class DeclineInviteCommandHandler(IApplicationDbContext dbContext, ICurrentUserService currentUserService)
    : IRequestHandler<DeclineInviteCommand, Result<bool>>
{
    public async Task<Result<bool>> Handle(DeclineInviteCommand request, CancellationToken cancellationToken)
    {
        var userId = currentUserService.UserId
            ?? throw new InvalidOperationException("An authenticated user is required.");

        var tenantMember = await dbContext.TenantMembers
            .FirstOrDefaultAsync(m => m.Id == request.TenantMemberId && m.UserId == userId, cancellationToken);

        if (tenantMember is null)
        {
            return Result<bool>.Failure("Invitation not found.");
        }

        if (tenantMember.InvitationStatus != InvitationStatus.Pending)
        {
            return Result<bool>.Failure("This invitation is no longer pending.");
        }

        tenantMember.InvitationStatus = InvitationStatus.Declined;

        await dbContext.SaveChangesAsync(cancellationToken);

        return Result<bool>.Success(true);
    }
}
