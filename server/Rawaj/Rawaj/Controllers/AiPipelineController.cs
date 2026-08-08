using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Rawaj.Application.Common.Models;
using Rawaj.Application.Features.AiPipeline.ApproveStrategy;
using Rawaj.Application.Features.AiPipeline.CancelRun;
using Rawaj.Application.Features.AiPipeline.GetArtifact;
using Rawaj.Application.Features.AiPipeline.GetRunStatus;
using Rawaj.Application.Features.AiPipeline.RefineStrategy;
using Rawaj.Application.Features.AiPipeline.ResumeRun;
using Rawaj.Application.Features.AiPipeline.RunStage;
using Rawaj.Application.Features.AiPipeline.StartRun;
using Rawaj.Common;
using Rawaj.Domain.Enums;

namespace Rawaj.Controllers;

/// <summary>
/// The pipeline's API surface. Deliberately thin — every handler already enforces its own tenant and
/// brand-access checks (<c>IRequireTenantRole</c>/<c>IRequireResolvedBrandAccess</c>), so this class
/// does nothing but translate HTTP into a MediatR request and a <see cref="Result{T}"/> into an
/// <see cref="ApiResponse{T}"/>.
/// </summary>
[ApiController]
[Authorize]
[Route("api/v1/ai-pipeline")]
public class AiPipelineController(ISender sender) : ControllerBase
{
    /// <summary>Returns 202: the run and its stage rows exist by the time this responds, but nothing
    /// has necessarily executed yet — the worker advances it on its next poll.</summary>
    [EnableRateLimiting("ai-generation")]
    [HttpPost("campaigns/{campaignId:guid}/start")]
    public async Task<IActionResult> Start(Guid campaignId, CancellationToken cancellationToken)
    {
        var result = await sender.Send(new StartRunCommand(campaignId), cancellationToken);

        return result.Succeeded
            ? Accepted(ApiResponse<StartRunResponse>.Success(result.Data!))
            : BadRequest(ApiResponse<StartRunResponse>.Fail(result.ErrorMessage!));
    }

    [HttpGet("runs/{runId:guid}")]
    public async Task<IActionResult> GetStatus(Guid runId, CancellationToken cancellationToken)
    {
        var result = await sender.Send(new GetRunStatusQuery(runId), cancellationToken);

        return result.Succeeded
            ? Ok(ApiResponse<GetRunStatusResponse>.Success(result.Data!))
            : NotFound(ApiResponse<GetRunStatusResponse>.Fail(result.ErrorMessage!));
    }

    [HttpGet("campaigns/{campaignId:guid}/artifacts/{kind}")]
    public async Task<IActionResult> GetArtifact(Guid campaignId, AiArtifactKind kind, CancellationToken cancellationToken)
    {
        var result = await sender.Send(new GetArtifactQuery(campaignId, kind), cancellationToken);

        return result.Succeeded
            ? Ok(ApiResponse<GetArtifactResponse>.Success(result.Data!))
            : NotFound(ApiResponse<GetArtifactResponse>.Fail(result.ErrorMessage!));
    }

    [EnableRateLimiting("ai-generation")]
    [HttpPost("runs/{runId:guid}/stages/{kind}/run")]
    public async Task<IActionResult> RunStage(Guid runId, AiPipelineStageKind kind, CancellationToken cancellationToken)
    {
        var result = await sender.Send(new RunStageCommand(runId, kind), cancellationToken);

        return result.Succeeded
            ? Ok(ApiResponse<RunStageResponse>.Success(result.Data!))
            : BadRequest(ApiResponse<RunStageResponse>.Fail(result.ErrorMessage!));
    }

    [EnableRateLimiting("ai-generation")]
    [HttpPost("runs/{runId:guid}/resume")]
    public async Task<IActionResult> Resume(Guid runId, CancellationToken cancellationToken)
    {
        var result = await sender.Send(new ResumeRunCommand(runId), cancellationToken);

        return result.Succeeded
            ? Ok(ApiResponse<ResumeRunResponse>.Success(result.Data!))
            : BadRequest(ApiResponse<ResumeRunResponse>.Fail(result.ErrorMessage!));
    }

    [HttpPost("runs/{runId:guid}/cancel")]
    public async Task<IActionResult> Cancel(Guid runId, CancellationToken cancellationToken)
    {
        var result = await sender.Send(new CancelRunCommand(runId), cancellationToken);

        return result.Succeeded
            ? Ok(ApiResponse<CancelRunResponse>.Success(result.Data!))
            : BadRequest(ApiResponse<CancelRunResponse>.Fail(result.ErrorMessage!));
    }

    [EnableRateLimiting("ai-generation")]
    [HttpPost("runs/{runId:guid}/approve")]
    public async Task<IActionResult> ApproveStrategy(Guid runId, CancellationToken cancellationToken)
    {
        var result = await sender.Send(new ApproveStrategyCommand(runId), cancellationToken);

        return result.Succeeded
            ? Ok(ApiResponse<ApproveStrategyResponse>.Success(result.Data!))
            : BadRequest(ApiResponse<ApproveStrategyResponse>.Fail(result.ErrorMessage!));
    }

    [EnableRateLimiting("ai-generation")]
    [HttpPost("campaigns/{campaignId:guid}/refine")]
    public async Task<IActionResult> RefineStrategy(Guid campaignId, [FromBody] RefineStrategyRequest request, CancellationToken cancellationToken)
    {
        var result = await sender.Send(new RefineStrategyCommand(campaignId, request.Feedback), cancellationToken);

        return result.Succeeded
            ? Ok(ApiResponse<RefineStrategyResponse>.Success(result.Data!))
            : BadRequest(ApiResponse<RefineStrategyResponse>.Fail(result.ErrorMessage!));
    }

    public record RefineStrategyRequest(string Feedback);
}
