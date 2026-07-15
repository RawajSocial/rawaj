using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Rawaj.Application.Features.TeamMembers.AddTeamMember;
using Rawaj.Application.Features.TeamMembers.GetTeamMembers;
using Rawaj.Common;

namespace Rawaj.Controllers;

[ApiController]
[Authorize]
[Route("api/v1/team-members")]
public class TeamMembersController(ISender sender) : ControllerBase
{
    [HttpPost]
    public async Task<IActionResult> Add(AddTeamMemberCommand command, CancellationToken cancellationToken)
    {
        var result = await sender.Send(command, cancellationToken);

        return result.Succeeded
            ? Ok(ApiResponse<AddTeamMemberResponse>.Success(result.Data!))
            : BadRequest(ApiResponse<AddTeamMemberResponse>.Fail(result.ErrorMessage!));
    }

    [HttpGet]
    public async Task<IActionResult> GetAll(CancellationToken cancellationToken)
    {
        var result = await sender.Send(new GetTeamMembersQuery(), cancellationToken);

        return result.Succeeded
            ? Ok(ApiResponse<List<TeamMemberSummary>>.Success(result.Data!))
            : BadRequest(ApiResponse<List<TeamMemberSummary>>.Fail(result.ErrorMessage!));
    }
}
