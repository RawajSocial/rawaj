using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Rawaj.Application.Common.Models;
using Rawaj.Application.Features.Content.CreatePostFromVisualAsset;
using Rawaj.Application.Features.Content.DeleteVisualAsset;
using Rawaj.Application.Features.Content.GenerateVisualAsset;
using Rawaj.Application.Features.Content.GetVisualAssets;
using Rawaj.Application.Features.Content.ReviewVisualAsset;
using Rawaj.Common;
using Rawaj.Domain.Enums;

namespace Rawaj.Controllers;

[ApiController]
[Authorize]
[Route("api/v1/visual-assets")]
public class VisualAssetsController(ISender sender) : ControllerBase
{
    [EnableRateLimiting("ai-generation")]
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
        [FromQuery] Guid brandProfileId, [FromQuery] Guid? campaignId, [FromQuery] string? search = null,
        [FromQuery] int page = 1, [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var result = await sender.Send(new GetVisualAssetsQuery(brandProfileId, campaignId, search, page, pageSize), cancellationToken);

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

    [HttpPost("{visualAssetId:guid}/create-post")]
    public async Task<IActionResult> CreatePost(
        Guid visualAssetId, [FromBody] CreatePostFromVisualAssetRequest request, CancellationToken cancellationToken)
    {
        var result = await sender.Send(
            new CreatePostFromVisualAssetCommand(visualAssetId, request.Platform, request.Language), cancellationToken);

        return result.Succeeded
            ? Ok(ApiResponse<CreatePostFromVisualAssetResponse>.Success(result.Data!))
            : BadRequest(ApiResponse<CreatePostFromVisualAssetResponse>.Fail(result.ErrorMessage!));
    }

    [HttpDelete("{visualAssetId:guid}")]
    public async Task<IActionResult> Delete(Guid visualAssetId, CancellationToken cancellationToken)
    {
        var result = await sender.Send(new DeleteVisualAssetCommand(visualAssetId), cancellationToken);

        return result.Succeeded
            ? Ok(ApiResponse<bool>.Success(result.Data))
            : BadRequest(ApiResponse<bool>.Fail(result.ErrorMessage!));
    }

    public record ReviewVisualAssetRequest(bool Approve);

    public record CreatePostFromVisualAssetRequest(SocialPlatform Platform, Language Language);
}
