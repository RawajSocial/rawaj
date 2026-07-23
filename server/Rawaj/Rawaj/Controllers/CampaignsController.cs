using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Rawaj.Application.Common.Models;
using Rawaj.Application.Features.Campaigns.ArchiveCampaign;
using Rawaj.Application.Features.Campaigns.CreateCampaign;
using Rawaj.Application.Features.Campaigns.GenerateCampaignContent;
using Rawaj.Application.Features.Campaigns.GenerateMarketingPlan;
using Rawaj.Application.Features.Campaigns.GetCampaign;
using Rawaj.Application.Features.Campaigns.GetCampaigns;
using Rawaj.Application.Features.Campaigns.UpdateCampaign;
using Rawaj.Common;
using Rawaj.Domain.Enums;

namespace Rawaj.Controllers;

[ApiController]
[Authorize]
[Route("api/v1/campaigns")]
public class CampaignsController(ISender sender) : ControllerBase
{
    [HttpPost]
    public async Task<IActionResult> Create(CreateCampaignCommand command, CancellationToken cancellationToken)
    {
        var result = await sender.Send(command, cancellationToken);

        return result.Succeeded
            ? Ok(ApiResponse<CreateCampaignResponse>.Success(result.Data!))
            : BadRequest(ApiResponse<CreateCampaignResponse>.Fail(result.ErrorMessage!));
    }

    [HttpGet]
    public async Task<IActionResult> GetAll(
        [FromQuery] Guid? brandProfileId, [FromQuery] int page = 1, [FromQuery] int pageSize = 20, CancellationToken cancellationToken = default)
    {
        var result = await sender.Send(new GetCampaignsQuery(brandProfileId, page, pageSize), cancellationToken);

        return result.Succeeded
            ? Ok(ApiResponse<PagedResult<CampaignSummary>>.Success(result.Data!))
            : BadRequest(ApiResponse<PagedResult<CampaignSummary>>.Fail(result.ErrorMessage!));
    }

    [HttpGet("{campaignId:guid}")]
    public async Task<IActionResult> GetById(Guid campaignId, CancellationToken cancellationToken)
    {
        var result = await sender.Send(new GetCampaignQuery(campaignId), cancellationToken);

        return result.Succeeded
            ? Ok(ApiResponse<GetCampaignResponse>.Success(result.Data!))
            : NotFound(ApiResponse<GetCampaignResponse>.Fail(result.ErrorMessage!));
    }

    [HttpPut("{campaignId:guid}")]
    public async Task<IActionResult> Update(Guid campaignId, [FromBody] UpdateCampaignRequest request, CancellationToken cancellationToken)
    {
        var result = await sender.Send(
            new UpdateCampaignCommand(campaignId, request.Name, request.Status, request.StartDate, request.EndDate, request.BudgetAmount),
            cancellationToken);

        return result.Succeeded
            ? Ok(ApiResponse<GetCampaignResponse>.Success(result.Data!))
            : BadRequest(ApiResponse<GetCampaignResponse>.Fail(result.ErrorMessage!));
    }

    [HttpPost("{campaignId:guid}/archive")]
    public async Task<IActionResult> Archive(Guid campaignId, CancellationToken cancellationToken)
    {
        var result = await sender.Send(new ArchiveCampaignCommand(campaignId), cancellationToken);

        return result.Succeeded
            ? Ok(ApiResponse<bool>.Success(result.Data!))
            : BadRequest(ApiResponse<bool>.Fail(result.ErrorMessage!));
    }

    [HttpPost("{campaignId:guid}/generate-plan")]
    public async Task<IActionResult> GeneratePlan(Guid campaignId, CancellationToken cancellationToken)
    {
        var result = await sender.Send(new GenerateMarketingPlanCommand(campaignId), cancellationToken);

        return result.Succeeded
            ? Ok(ApiResponse<GenerateMarketingPlanResponse>.Success(result.Data!))
            : BadRequest(ApiResponse<GenerateMarketingPlanResponse>.Fail(result.ErrorMessage!));
    }

    [HttpPost("{campaignId:guid}/generate-content")]
    public async Task<IActionResult> GenerateContent(
        Guid campaignId, [FromBody] GenerateCampaignContentRequest request, CancellationToken cancellationToken)
    {
        var result = await sender.Send(
            new GenerateCampaignContentCommand(
                campaignId, request.PostCount, request.Language, request.IncludeImages, request.TemplateStyle),
            cancellationToken);

        return result.Succeeded
            ? Ok(ApiResponse<GenerateCampaignContentResponse>.Success(result.Data!))
            : BadRequest(ApiResponse<GenerateCampaignContentResponse>.Fail(result.ErrorMessage!));
    }

    public record GenerateCampaignContentRequest(
        int PostCount, Language Language, bool IncludeImages = true, ContentTemplateStyle TemplateStyle = ContentTemplateStyle.Auto);

    public record UpdateCampaignRequest(
        string? Name, CampaignStatus? Status, DateOnly? StartDate, DateOnly? EndDate, decimal? BudgetAmount);
}
