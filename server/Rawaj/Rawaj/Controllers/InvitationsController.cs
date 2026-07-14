using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Rawaj.Application.Common.Models;
using Rawaj.Application.Features.Tenants.AcceptInvitation;
using Rawaj.Application.Features.Tenants.DeclineInvitation;
using Rawaj.Application.Features.Tenants.GetInvitation;
using Rawaj.Common;

namespace Rawaj.Controllers;

[ApiController]
[Route("api/v1/invitations")]
public class InvitationsController(ISender sender) : ControllerBase
{
    [HttpGet("{invitationId:guid}")]
    [AllowAnonymous]
    public async Task<IActionResult> Get(Guid invitationId, CancellationToken cancellationToken)
    {
        var result = await sender.Send(new GetInvitationQuery(invitationId), cancellationToken);

        return result.Succeeded
            ? Ok(ApiResponse<InvitationDetailsDto>.Success(result.Data!))
            : NotFound(ApiResponse<InvitationDetailsDto>.Fail(result.ErrorMessage!));
    }

    [HttpPost("{invitationId:guid}/accept")]
    [Authorize]
    public async Task<IActionResult> Accept(Guid invitationId, CancellationToken cancellationToken)
    {
        var result = await sender.Send(new AcceptInvitationCommand(invitationId), cancellationToken);

        return result.Succeeded
            ? Ok(ApiResponse<string>.Success("Invitation accepted."))
            : BadRequest(ApiResponse<string>.Fail(result.ErrorMessage!));
    }

    [HttpPost("{invitationId:guid}/decline")]
    [Authorize]
    public async Task<IActionResult> Decline(Guid invitationId, CancellationToken cancellationToken)
    {
        var result = await sender.Send(new DeclineInvitationCommand(invitationId), cancellationToken);

        return result.Succeeded
            ? Ok(ApiResponse<string>.Success("Invitation declined."))
            : BadRequest(ApiResponse<string>.Fail(result.ErrorMessage!));
    }
}
