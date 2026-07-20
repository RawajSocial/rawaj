using MediatR;
using Rawaj.Application.Common.Interfaces;
using Rawaj.Application.Common.Models;

namespace Rawaj.Application.Features.Tenants.InviteMember;

public class InviteTenantMemberCommandHandler(ITenantProvisioningService tenantProvisioningService, ICurrentUserService currentUserService)
    : IRequestHandler<InviteTenantMemberCommand, Result<InviteTenantMemberResponse>>
{
    public async Task<Result<InviteTenantMemberResponse>> Handle(InviteTenantMemberCommand request, CancellationToken cancellationToken)
    {
        if (currentUserService.UserId is not { } inviterUserId)
        {
            return Result<InviteTenantMemberResponse>.Failure("You must be signed in to invite a member.");
        }

        var result = await tenantProvisioningService.InviteMemberAsync(request.TenantId, request.Email, request.Role, inviterUserId, cancellationToken);

        return result.Outcome switch
        {
            InviteMemberOutcome.AlreadyMember => Result<InviteTenantMemberResponse>.Failure("This user is already a member of the tenant."),
            InviteMemberOutcome.AlreadyInvited => Result<InviteTenantMemberResponse>.Failure("An invitation is already pending for this email."),
            _ => Result<InviteTenantMemberResponse>.Success(new InviteTenantMemberResponse(result.InvitationId, result.RequiresRegistration))
        };
    }
}
