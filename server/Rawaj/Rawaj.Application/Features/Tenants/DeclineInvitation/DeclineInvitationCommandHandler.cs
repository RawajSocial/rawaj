using MediatR;
using Rawaj.Application.Common.Interfaces;
using Rawaj.Application.Common.Models;

namespace Rawaj.Application.Features.Tenants.DeclineInvitation;

public class DeclineInvitationCommandHandler(
    ITenantProvisioningService tenantProvisioningService,
    ICurrentUserService currentUserService,
    IIdentityService identityService)
    : IRequestHandler<DeclineInvitationCommand, Result<Unit>>
{
    public async Task<Result<Unit>> Handle(DeclineInvitationCommand request, CancellationToken cancellationToken)
    {
        if (currentUserService.UserId is not { } userId)
        {
            return Result<Unit>.Failure("You must be signed in to respond to an invitation.");
        }

        var user = await identityService.FindByIdAsync(userId, cancellationToken);
        if (user is null)
        {
            return Result<Unit>.Failure("You must be signed in to respond to an invitation.");
        }

        var outcome = await tenantProvisioningService.DeclineInvitationAsync(request.InvitationId, userId, user.Email, cancellationToken);

        return outcome switch
        {
            InvitationActionOutcome.Success => Result<Unit>.Success(Unit.Value),
            InvitationActionOutcome.NotFound => Result<Unit>.Failure("Invitation not found."),
            InvitationActionOutcome.NotAuthorized => Result<Unit>.Failure("This invitation was not sent to your account."),
            InvitationActionOutcome.AlreadyResolved => Result<Unit>.Failure("This invitation has already been responded to."),
            _ => Result<Unit>.Failure("Unable to process this invitation.")
        };
    }
}
