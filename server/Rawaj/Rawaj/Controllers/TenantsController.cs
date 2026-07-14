using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Rawaj.Application.Features.Tenants.InviteMember;
using Rawaj.Common;

namespace Rawaj.Controllers;

[ApiController]
[Authorize]
[Route("api/v1/tenants")]
public class TenantsController(ISender sender) : ControllerBase
{
    [HttpPost("invitations")]
    public async Task<IActionResult> InviteMember(InviteTenantMemberCommand command, CancellationToken cancellationToken)
    {
        var result = await sender.Send(command, cancellationToken);

        return result.Succeeded
            ? Ok(ApiResponse<string>.Success("Invitation sent."))
            : BadRequest(ApiResponse<string>.Fail(result.ErrorMessage!));
    }
}
