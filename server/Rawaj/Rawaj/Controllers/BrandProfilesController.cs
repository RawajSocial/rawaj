using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Rawaj.Application.Features.Brands.CreateBrandProfile;
using Rawaj.Application.Features.Brands.GetBrandProfiles;
using Rawaj.Common;

namespace Rawaj.Controllers;

[ApiController]
[Authorize]
[Route("api/v1/brand-profiles")]
public class BrandProfilesController(ISender sender) : ControllerBase
{
    [HttpPost]
    public async Task<IActionResult> Create(CreateBrandProfileCommand command, CancellationToken cancellationToken)
    {
        var result = await sender.Send(command, cancellationToken);

        return result.Succeeded
            ? Ok(ApiResponse<CreateBrandProfileResponse>.Success(result.Data!))
            : BadRequest(ApiResponse<CreateBrandProfileResponse>.Fail(result.ErrorMessage!));
    }

    [HttpGet]
    public async Task<IActionResult> GetAll(CancellationToken cancellationToken)
    {
        var result = await sender.Send(new GetBrandProfilesQuery(), cancellationToken);

        return result.Succeeded
            ? Ok(ApiResponse<List<BrandProfileSummary>>.Success(result.Data!))
            : BadRequest(ApiResponse<List<BrandProfileSummary>>.Fail(result.ErrorMessage!));
    }
}
