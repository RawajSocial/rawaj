using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Rawaj.Application.Common.Models;
using Rawaj.Application.Features.Admin.GetPlatformStats;
using Rawaj.Application.Features.Admin.GetTenants;
using Rawaj.Application.Features.Admin.GetUsers;
using Rawaj.Application.Features.Admin.SetUserActive;
using Rawaj.Common;

namespace Rawaj.Controllers;

/// <summary>
/// Platform-level operations for Rawaj's own staff (support/ops), distinct from tenant-scoped
/// RBAC (TenantMemberRole) which governs what a tenant's own users can do within their tenant.
/// Gated by the platform_admin JWT claim rather than the TenantAuthorizationBehavior pipeline,
/// since these endpoints act across tenants and have no single tenant to scope to.
/// </summary>
[ApiController]
[Authorize(Policy = "PlatformAdmin")]
[Route("api/v1/admin")]
public class AdminController(ISender sender) : ControllerBase
{
    [HttpGet("stats")]
    public async Task<IActionResult> GetStats(CancellationToken cancellationToken)
    {
        var result = await sender.Send(new GetPlatformStatsQuery(), cancellationToken);

        return result.Succeeded
            ? Ok(ApiResponse<PlatformStatsResponse>.Success(result.Data!))
            : BadRequest(ApiResponse<PlatformStatsResponse>.Fail(result.ErrorMessage!));
    }

    [HttpGet("tenants")]
    public async Task<IActionResult> GetTenants([FromQuery] int page = 1, [FromQuery] int pageSize = 20, CancellationToken cancellationToken = default)
    {
        var result = await sender.Send(new GetTenantsQuery(page, pageSize), cancellationToken);

        return result.Succeeded
            ? Ok(ApiResponse<PagedResult<AdminTenantSummary>>.Success(result.Data!))
            : BadRequest(ApiResponse<PagedResult<AdminTenantSummary>>.Fail(result.ErrorMessage!));
    }

    [HttpGet("users")]
    public async Task<IActionResult> GetUsers([FromQuery] int page = 1, [FromQuery] int pageSize = 20, CancellationToken cancellationToken = default)
    {
        var result = await sender.Send(new GetUsersQuery(page, pageSize), cancellationToken);

        return result.Succeeded
            ? Ok(ApiResponse<PagedResult<AdminUserSummary>>.Success(result.Data!))
            : BadRequest(ApiResponse<PagedResult<AdminUserSummary>>.Fail(result.ErrorMessage!));
    }

    [HttpPost("users/{userId:guid}/active")]
    public async Task<IActionResult> SetUserActive(Guid userId, [FromBody] SetUserActiveRequest request, CancellationToken cancellationToken)
    {
        var result = await sender.Send(new SetUserActiveCommand(userId, request.IsActive), cancellationToken);

        return result.Succeeded
            ? Ok(ApiResponse<bool>.Success(result.Data))
            : BadRequest(ApiResponse<bool>.Fail(result.ErrorMessage!));
    }

    public record SetUserActiveRequest(bool IsActive);
}
