using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Rawaj.Application.Features.TeamMembers.AcceptInvitationAndRegister;
using Rawaj.Application.Features.TeamMembers.AcceptInvite;
using Rawaj.Application.Features.TeamMembers.AddTeamMember;
using Rawaj.Application.Features.TeamMembers.AllocateCoins;
using Rawaj.Application.Features.TeamMembers.DeclineInvite;
using Rawaj.Application.Features.TeamMembers.GetInvitationDetails;
using Rawaj.Application.Features.TeamMembers.GetMyPendingInvites;
using Rawaj.Application.Features.TeamMembers.GetPendingInvitations;
using Rawaj.Application.Features.TeamMembers.GetTeamActivity;
using Rawaj.Application.Features.TeamMembers.GetTeamMembers;
using Rawaj.Application.Features.TeamMembers.RemoveTeamMember;
using Rawaj.Application.Features.TeamMembers.RevokeInvitation;
using Rawaj.Application.Features.TeamMembers.UpdateTeamMember;
using Rawaj.Common;

namespace Rawaj.Controllers;

[ApiController]
[Route("api/v1/team-members")]
public class TeamMembersController(ISender sender) : ControllerBase
{
    [Authorize]
    [HttpPost]
    public async Task<IActionResult> Add(AddTeamMemberCommand command, CancellationToken cancellationToken)
    {
        var result = await sender.Send(command, cancellationToken);

        return result.Succeeded
            ? Ok(ApiResponse<AddTeamMemberResponse>.Success(result.Data!))
            : BadRequest(ApiResponse<AddTeamMemberResponse>.Fail(result.ErrorMessage!));
    }

    [Authorize]
    [HttpGet]
    public async Task<IActionResult> GetAll(CancellationToken cancellationToken)
    {
        var result = await sender.Send(new GetTeamMembersQuery(), cancellationToken);

        return result.Succeeded
            ? Ok(ApiResponse<List<TeamMemberSummary>>.Success(result.Data!))
            : BadRequest(ApiResponse<List<TeamMemberSummary>>.Fail(result.ErrorMessage!));
    }

    [Authorize]
    [HttpGet("pending-invites")]
    public async Task<IActionResult> GetMyPendingInvites(CancellationToken cancellationToken)
    {
        var result = await sender.Send(new GetMyPendingInvitesQuery(), cancellationToken);

        return result.Succeeded
            ? Ok(ApiResponse<List<PendingInviteSummary>>.Success(result.Data!))
            : BadRequest(ApiResponse<List<PendingInviteSummary>>.Fail(result.ErrorMessage!));
    }

    /// <summary>Pending invites to emails with no Rawaj account yet — the tenant-admin's view.</summary>
    [Authorize]
    [HttpGet("invitations")]
    public async Task<IActionResult> GetPendingInvitations(CancellationToken cancellationToken)
    {
        var result = await sender.Send(new GetPendingInvitationsQuery(), cancellationToken);

        return result.Succeeded
            ? Ok(ApiResponse<List<PendingInvitationSummary>>.Success(result.Data!))
            : BadRequest(ApiResponse<List<PendingInvitationSummary>>.Fail(result.ErrorMessage!));
    }

    /// <summary>Anonymous lookup for the `/invite?token=...` page — works for both invite kinds.</summary>
    [AllowAnonymous]
    [HttpGet("invitations/{token}")]
    public async Task<IActionResult> GetInvitationDetails(string token, CancellationToken cancellationToken)
    {
        var result = await sender.Send(new GetInvitationDetailsQuery(token), cancellationToken);

        return result.Succeeded
            ? Ok(ApiResponse<InvitationDetailsResponse>.Success(result.Data!))
            : NotFound(ApiResponse<InvitationDetailsResponse>.Fail(result.ErrorMessage!));
    }

    public record AcceptInvitationAndRegisterRequest(
        string Username, string Password, string FullName, Domain.Enums.Language PreferredLanguage);

    /// <summary>Register-and-join in one call, for an invite sent to an email with no account yet.</summary>
    [AllowAnonymous]
    [EnableRateLimiting("auth")]
    [HttpPost("invitations/{token}/accept")]
    public async Task<IActionResult> AcceptInvitationAndRegister(
        string token, AcceptInvitationAndRegisterRequest request, CancellationToken cancellationToken)
    {
        var result = await sender.Send(
            new AcceptInvitationAndRegisterCommand(token, request.Username, request.Password, request.FullName, request.PreferredLanguage),
            cancellationToken);

        return result.Succeeded
            ? Ok(ApiResponse<AcceptInvitationAndRegisterResponse>.Success(result.Data!))
            : BadRequest(ApiResponse<AcceptInvitationAndRegisterResponse>.Fail(result.ErrorMessage!));
    }

    [Authorize]
    [HttpDelete("invitations/{invitationId:guid}")]
    public async Task<IActionResult> RevokeInvitation(Guid invitationId, CancellationToken cancellationToken)
    {
        var result = await sender.Send(new RevokeInvitationCommand(invitationId), cancellationToken);

        return result.Succeeded
            ? Ok(ApiResponse<bool>.Success(result.Data))
            : BadRequest(ApiResponse<bool>.Fail(result.ErrorMessage!));
    }

    [Authorize]
    [HttpPost("{tenantMemberId:guid}/accept")]
    public async Task<IActionResult> AcceptInvite(Guid tenantMemberId, CancellationToken cancellationToken)
    {
        var result = await sender.Send(new AcceptInviteCommand(tenantMemberId), cancellationToken);

        return result.Succeeded
            ? Ok(ApiResponse<bool>.Success(result.Data))
            : BadRequest(ApiResponse<bool>.Fail(result.ErrorMessage!));
    }

    [Authorize]
    [HttpPost("{tenantMemberId:guid}/decline")]
    public async Task<IActionResult> DeclineInvite(Guid tenantMemberId, CancellationToken cancellationToken)
    {
        var result = await sender.Send(new DeclineInviteCommand(tenantMemberId), cancellationToken);

        return result.Succeeded
            ? Ok(ApiResponse<bool>.Success(result.Data))
            : BadRequest(ApiResponse<bool>.Fail(result.ErrorMessage!));
    }

    [Authorize]
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

    [Authorize]
    [HttpPost("{tenantMemberId:guid}/coins")]
    public async Task<IActionResult> AllocateCoins(
        Guid tenantMemberId, [FromBody] AllocateCoinsRequest request, CancellationToken cancellationToken)
    {
        var result = await sender.Send(new AllocateCoinsCommand(tenantMemberId, request.NewAllocation), cancellationToken);

        return result.Succeeded
            ? Ok(ApiResponse<AllocateCoinsResponse>.Success(result.Data!))
            : BadRequest(ApiResponse<AllocateCoinsResponse>.Fail(result.ErrorMessage!));
    }

    public record AllocateCoinsRequest(int NewAllocation);

    [Authorize]
    [HttpDelete("{tenantMemberId:guid}")]
    public async Task<IActionResult> Remove(Guid tenantMemberId, CancellationToken cancellationToken)
    {
        var result = await sender.Send(new RemoveTeamMemberCommand(tenantMemberId), cancellationToken);

        return result.Succeeded
            ? Ok(ApiResponse<bool>.Success(result.Data))
            : BadRequest(ApiResponse<bool>.Fail(result.ErrorMessage!));
    }

    [Authorize]
    [HttpGet("activity")]
    public async Task<IActionResult> GetActivity(
        [FromQuery] int page, [FromQuery] int pageSize, [FromQuery] Guid? userId, CancellationToken cancellationToken)
    {
        var result = await sender.Send(
            new GetTeamActivityQuery(page == 0 ? 1 : page, pageSize == 0 ? 20 : pageSize, userId), cancellationToken);

        return result.Succeeded
            ? Ok(ApiResponse<TeamActivityPageResponse>.Success(result.Data!))
            : BadRequest(ApiResponse<TeamActivityPageResponse>.Fail(result.ErrorMessage!));
    }

    public record UpdateTeamMemberRequest(Domain.Enums.TenantMemberRole Role, List<Guid> BrandProfileIds);
}
