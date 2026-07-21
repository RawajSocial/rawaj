using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Rawaj.Application.Features.TeamMembers.AcceptInvite;
using Rawaj.Application.Features.TeamMembers.AddTeamMember;
using Rawaj.Application.Features.TeamMembers.DeclineInvite;
using Rawaj.Application.Features.TeamMembers.GetMyPendingInvites;
using Rawaj.Application.Features.TeamMembers.GetTeamMembers;
using Rawaj.Application.Features.TeamMembers.RemoveTeamMember;
using Rawaj.Application.Features.TeamMembers.UpdateTeamMember;
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

    [HttpGet("pending-invites")]
    public async Task<IActionResult> GetMyPendingInvites(CancellationToken cancellationToken)
    {
        var result = await sender.Send(new GetMyPendingInvitesQuery(), cancellationToken);

        return result.Succeeded
            ? Ok(ApiResponse<List<PendingInviteSummary>>.Success(result.Data!))
            : BadRequest(ApiResponse<List<PendingInviteSummary>>.Fail(result.ErrorMessage!));
    }

    [HttpPost("{tenantMemberId:guid}/accept")]
    public async Task<IActionResult> AcceptInvite(Guid tenantMemberId, CancellationToken cancellationToken)
    {
        var result = await sender.Send(new AcceptInviteCommand(tenantMemberId), cancellationToken);

        return result.Succeeded
            ? Ok(ApiResponse<bool>.Success(result.Data))
            : BadRequest(ApiResponse<bool>.Fail(result.ErrorMessage!));
    }

    [HttpPost("{tenantMemberId:guid}/decline")]
    public async Task<IActionResult> DeclineInvite(Guid tenantMemberId, CancellationToken cancellationToken)
    {
        var result = await sender.Send(new DeclineInviteCommand(tenantMemberId), cancellationToken);

        return result.Succeeded
            ? Ok(ApiResponse<bool>.Success(result.Data))
            : BadRequest(ApiResponse<bool>.Fail(result.ErrorMessage!));
    }

    [HttpPut("{tenantMemberId:guid}")]
    public async Task<IActionResult> Update(
        Guid tenantMemberId, [FromBody] UpdateTeamMemberRequest request, CancellationToken cancellationToken)
    {
        var result = await sender.Send(
            new UpdateTeamMemberCommand(tenantMemberId, request.Role, request.BrandProfileIds), cancellationToken);

        return result.Succeeded
            ? Ok(ApiResponse<UpdateTeamMemberResponse>.Success(result.Data!))
            : BadRequest(ApiResponse<UpdateTeamMemberResponse>.Fail(result.ErrorMessage!));
    }

    [HttpDelete("{tenantMemberId:guid}")]
    public async Task<IActionResult> Remove(Guid tenantMemberId, CancellationToken cancellationToken)
    {
        var result = await sender.Send(new RemoveTeamMemberCommand(tenantMemberId), cancellationToken);

        return result.Succeeded
            ? Ok(ApiResponse<bool>.Success(result.Data))
            : BadRequest(ApiResponse<bool>.Fail(result.ErrorMessage!));
    }

    public record UpdateTeamMemberRequest(Domain.Enums.TenantMemberRole Role, List<Guid> BrandProfileIds);
}
