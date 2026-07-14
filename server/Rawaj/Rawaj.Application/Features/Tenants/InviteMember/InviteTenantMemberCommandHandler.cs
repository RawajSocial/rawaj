using MediatR;
using Rawaj.Application.Common.Interfaces;
using Rawaj.Application.Common.Models;

namespace Rawaj.Application.Features.Tenants.InviteMember;

public class InviteTenantMemberCommandHandler(ITenantProvisioningService tenantProvisioningService, ICurrentUserService currentUserService)
    : IRequestHandler<InviteTenantMemberCommand, Result<Unit>>
{
    public async Task<Result<Unit>> Handle(InviteTenantMemberCommand request, CancellationToken cancellationToken)
    {
        if (currentUserService.UserId is not { } inviterUserId)
        {
            return Result<Unit>.Failure("You must be signed in to invite a member.");
        }

        await tenantProvisioningService.InviteMemberAsync(request.TenantId, request.Email, request.Role, inviterUserId, cancellationToken);

        return Result<Unit>.Success(Unit.Value);
    }
}
