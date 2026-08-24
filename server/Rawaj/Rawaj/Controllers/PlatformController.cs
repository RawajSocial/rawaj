using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Rawaj.Application.Features.Platform.GetFeatureFlags;
using Rawaj.Common;

namespace Rawaj.Controllers;

[ApiController]
[Route("api/v1/platform")]
public class PlatformController(ISender sender) : ControllerBase
{
    [AllowAnonymous]
    [HttpGet("feature-flags")]
    public async Task<IActionResult> GetFeatureFlags(CancellationToken cancellationToken)
    {
        var result = await sender.Send(new GetFeatureFlagsQuery(), cancellationToken);

        return result.Succeeded
            ? Ok(ApiResponse<FeatureFlagsResponse>.Success(result.Data!))
            : BadRequest(ApiResponse<FeatureFlagsResponse>.Fail(result.ErrorMessage!));
    }
}
