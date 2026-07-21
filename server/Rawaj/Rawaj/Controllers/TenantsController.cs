using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Rawaj.Application.Features.Tenants.CreateTenant;
using Rawaj.Application.Features.Tenants.GetMyTenant;
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

    [HttpGet("me")]
    public async Task<IActionResult> GetMine(CancellationToken cancellationToken)
    {
        var result = await sender.Send(new GetMyTenantQuery(), cancellationToken);

        return result.Succeeded
            ? Ok(ApiResponse<GetMyTenantResponse>.Success(result.Data!))
            : NotFound(ApiResponse<GetMyTenantResponse>.Fail(result.ErrorMessage!));
    }
}
