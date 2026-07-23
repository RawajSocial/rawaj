using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Rawaj.Application.Common.Models;
using Rawaj.Application.Features.Content.GenerateVisualAsset;
using Rawaj.Application.Features.Content.GetVisualAssets;
using Rawaj.Application.Features.Content.ReviewVisualAsset;
using Rawaj.Common;

namespace Rawaj.Controllers;

[ApiController]
[Authorize]
[Route("api/v1/visual-assets")]
public class VisualAssetsController(ISender sender) : ControllerBase
{
    [HttpPost("generate")]
    public async Task<IActionResult> Generate(GenerateVisualAssetCommand command, CancellationToken cancellationToken)
    {
        var result = await sender.Send(command, cancellationToken);

        return result.Succeeded
            ? Ok(ApiResponse<GenerateVisualAssetResponse>.Success(result.Data!))
            : BadRequest(ApiResponse<GenerateVisualAssetResponse>.Fail(result.ErrorMessage!));
    }

    [HttpGet]
    public async Task<IActionResult> GetAll(
        [FromQuery] Guid brandProfileId, [FromQuery] Guid? campaignId, [FromQuery] int page = 1, [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var result = await sender.Send(new GetVisualAssetsQuery(brandProfileId, campaignId, page, pageSize), cancellationToken);

        return result.Succeeded
            ? Ok(ApiResponse<PagedResult<VisualAssetSummary>>.Success(result.Data!))
            : BadRequest(ApiResponse<PagedResult<VisualAssetSummary>>.Fail(result.ErrorMessage!));
    }

    [HttpPost("{visualAssetId:guid}/review")]
    public async Task<IActionResult> Review(Guid visualAssetId, [FromBody] ReviewVisualAssetRequest request, CancellationToken cancellationToken)
    {
        var result = await sender.Send(new ReviewVisualAssetCommand(visualAssetId, request.Approve), cancellationToken);

        return result.Succeeded
            ? Ok(ApiResponse<ReviewVisualAssetResponse>.Success(result.Data!))
            : BadRequest(ApiResponse<ReviewVisualAssetResponse>.Fail(result.ErrorMessage!));
    }

    public record ReviewVisualAssetRequest(bool Approve);
}
