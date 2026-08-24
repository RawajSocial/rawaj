using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Rawaj.Application.Common.Models;
using Rawaj.Application.Features.Content.DeleteContentItem;
using Rawaj.Application.Features.Content.GenerateContentItem;
using Rawaj.Application.Features.Content.GetContentItem;
using Rawaj.Application.Features.Content.GetContentItems;
using Rawaj.Application.Features.Content.GetContentRevisions;
using Rawaj.Application.Features.Content.RegenerateContentItem;
using Rawaj.Application.Features.Content.ReviewContentItem;
using Rawaj.Common;

namespace Rawaj.Controllers;

[ApiController]
[Authorize]
[Route("api/v1/content-items")]
public class ContentController(ISender sender) : ControllerBase
{
    [EnableRateLimiting("ai-generation")]
    [HttpPost("generate")]
    public async Task<IActionResult> Generate(GenerateContentItemCommand command, CancellationToken cancellationToken)
    {
        var result = await sender.Send(command, cancellationToken);

        return result.Succeeded
            ? Ok(ApiResponse<GenerateContentItemResponse>.Success(result.Data!))
            : BadRequest(ApiResponse<GenerateContentItemResponse>.Fail(result.ErrorMessage!));
    }

    [HttpGet]
    public async Task<IActionResult> GetAll(
        [FromQuery] Guid brandProfileId, [FromQuery] Guid? campaignId, [FromQuery] int page = 1, [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var result = await sender.Send(new GetContentItemsQuery(brandProfileId, campaignId, page, pageSize), cancellationToken);

        return result.Succeeded
            ? Ok(ApiResponse<PagedResult<ContentItemSummary>>.Success(result.Data!))
            : BadRequest(ApiResponse<PagedResult<ContentItemSummary>>.Fail(result.ErrorMessage!));
    }

    [HttpGet("{contentItemId:guid}")]
    public async Task<IActionResult> GetById(Guid contentItemId, CancellationToken cancellationToken)
    {
        var result = await sender.Send(new GetContentItemQuery(contentItemId), cancellationToken);

        return result.Succeeded
            ? Ok(ApiResponse<GetContentItemResponse>.Success(result.Data!))
            : NotFound(ApiResponse<GetContentItemResponse>.Fail(result.ErrorMessage!));
    }

    [HttpPost("{contentItemId:guid}/review")]
    public async Task<IActionResult> Review(Guid contentItemId, [FromBody] ReviewContentItemRequest request, CancellationToken cancellationToken)
    {
        var result = await sender.Send(new ReviewContentItemCommand(contentItemId, request.Approve), cancellationToken);

        return result.Succeeded
            ? Ok(ApiResponse<ReviewContentItemResponse>.Success(result.Data!))
            : BadRequest(ApiResponse<ReviewContentItemResponse>.Fail(result.ErrorMessage!));
    }

    [EnableRateLimiting("ai-generation")]
    [HttpPost("{contentItemId:guid}/regenerate")]
    public async Task<IActionResult> Regenerate(Guid contentItemId, [FromBody] RegenerateContentItemRequest request, CancellationToken cancellationToken)
    {
        var result = await sender.Send(new RegenerateContentItemCommand(contentItemId, request.Feedback), cancellationToken);

        return result.Succeeded
            ? Ok(ApiResponse<RegenerateContentItemResponse>.Success(result.Data!))
            : BadRequest(ApiResponse<RegenerateContentItemResponse>.Fail(result.ErrorMessage!));
    }

    [HttpDelete("{contentItemId:guid}")]
    public async Task<IActionResult> Delete(Guid contentItemId, CancellationToken cancellationToken)
    {
        var result = await sender.Send(new DeleteContentItemCommand(contentItemId), cancellationToken);

        return result.Succeeded
            ? Ok(ApiResponse<bool>.Success(result.Data))
            : BadRequest(ApiResponse<bool>.Fail(result.ErrorMessage!));
    }

    [HttpGet("{contentItemId:guid}/revisions")]
    public async Task<IActionResult> GetRevisions(Guid contentItemId, CancellationToken cancellationToken)
    {
        var result = await sender.Send(new GetContentRevisionsQuery(contentItemId), cancellationToken);

        return result.Succeeded
            ? Ok(ApiResponse<List<ContentRevisionSummary>>.Success(result.Data!))
            : BadRequest(ApiResponse<List<ContentRevisionSummary>>.Fail(result.ErrorMessage!));
    }

    public record ReviewContentItemRequest(bool Approve);

    public record RegenerateContentItemRequest(string Feedback);
}
