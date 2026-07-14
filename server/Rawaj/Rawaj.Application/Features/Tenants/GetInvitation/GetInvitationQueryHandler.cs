using MediatR;
using Rawaj.Application.Common.Interfaces;
using Rawaj.Application.Common.Models;

namespace Rawaj.Application.Features.Tenants.GetInvitation;

public class GetInvitationQueryHandler(ITenantProvisioningService tenantProvisioningService)
    : IRequestHandler<GetInvitationQuery, Result<InvitationDetailsDto>>
{
    public async Task<Result<InvitationDetailsDto>> Handle(GetInvitationQuery request, CancellationToken cancellationToken)
    {
        var invitation = await tenantProvisioningService.GetInvitationAsync(request.InvitationId, cancellationToken);

        return invitation is null
            ? Result<InvitationDetailsDto>.Failure("Invitation not found.")
            : Result<InvitationDetailsDto>.Success(invitation);
    }
}
