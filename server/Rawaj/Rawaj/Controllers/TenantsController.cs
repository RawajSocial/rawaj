using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Rawaj.Application.Features.Tenants.CreateTenant;
using Rawaj.Application.Features.Tenants.GetMyMemberships;
using Rawaj.Application.Features.Tenants.GetMyTenant;
using Rawaj.Application.Features.Tenants.GetTenantUsageSummary;
using Rawaj.Application.Features.Tenants.UpdateTenantProfile;
using Rawaj.Application.Features.Tenants.UpgradeToAgency;
using Rawaj.Common;

namespace Rawaj.Controllers;

[ApiController]
[Authorize]
[Route("api/v1/tenants")]
public class TenantsController(ISender sender) : ControllerBase
{
    [HttpPost]
    public async Task<IActionResult> Create(CreateTenantCommand command, CancellationToken cancellationToken)
    {
        var result = await sender.Send(command, cancellationToken);

        return result.Succeeded
            ? Ok(ApiResponse<CreateTenantResponse>.Success(result.Data!))
            : BadRequest(ApiResponse<CreateTenantResponse>.Fail(result.ErrorMessage!));
    }

    [HttpGet("memberships")]
    public async Task<IActionResult> GetMemberships(CancellationToken cancellationToken)
    {
        var result = await sender.Send(new GetMyMembershipsQuery(), cancellationToken);

        return result.Succeeded
            ? Ok(ApiResponse<List<MembershipSummary>>.Success(result.Data!))
            : BadRequest(ApiResponse<List<MembershipSummary>>.Fail(result.ErrorMessage!));
    }

    [HttpGet("me")]
    public async Task<IActionResult> GetMine(CancellationToken cancellationToken)
    {
        var result = await sender.Send(new GetMyTenantQuery(), cancellationToken);

        return result.Succeeded
            ? Ok(ApiResponse<GetMyTenantResponse>.Success(result.Data!))
            : NotFound(ApiResponse<GetMyTenantResponse>.Fail(result.ErrorMessage!));
    }

    [HttpPut("me/profile")]
    public async Task<IActionResult> UpdateProfile(UpdateTenantProfileCommand command, CancellationToken cancellationToken)
    {
        var result = await sender.Send(command, cancellationToken);

        return result.Succeeded
            ? Ok(ApiResponse<UpdateTenantProfileResponse>.Success(result.Data!))
            : BadRequest(ApiResponse<UpdateTenantProfileResponse>.Fail(result.ErrorMessage!));
    }

    [HttpGet("me/usage-summary")]
    public async Task<IActionResult> GetUsageSummary(CancellationToken cancellationToken)
    {
        var result = await sender.Send(new GetTenantUsageSummaryQuery(), cancellationToken);

        return result.Succeeded
            ? Ok(ApiResponse<TenantUsageSummaryResponse>.Success(result.Data!))
            : BadRequest(ApiResponse<TenantUsageSummaryResponse>.Fail(result.ErrorMessage!));
    }

    [HttpPost("me/upgrade-to-agency")]
    public async Task<IActionResult> UpgradeToAgency(UpgradeToAgencyCommand command, CancellationToken cancellationToken)
    {
        var result = await sender.Send(command, cancellationToken);

        return result.Succeeded
            ? Ok(ApiResponse<UpgradeToAgencyResponse>.Success(result.Data!))
            : BadRequest(ApiResponse<UpgradeToAgencyResponse>.Fail(result.ErrorMessage!));
    }
}
