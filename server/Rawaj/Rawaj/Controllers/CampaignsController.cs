using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Rawaj.Application.Common.Models;
using Rawaj.Application.Features.Campaigns.ApproveCampaignPlan;
using Rawaj.Application.Features.Campaigns.ArchiveCampaign;
using Rawaj.Application.Features.Campaigns.CreateCampaign;
using Rawaj.Application.Features.Campaigns.DeleteCampaign;
using Rawaj.Application.Features.Campaigns.GenerateBusinessDiagnosis;
using Rawaj.Application.Features.Campaigns.GenerateCampaignContent;
using Rawaj.Application.Features.Campaigns.GenerateMarketingPlan;
using Rawaj.Application.Features.Campaigns.GetCampaign;
using Rawaj.Application.Features.Campaigns.GetCampaignDeleteSummary;
using Rawaj.Application.Features.Campaigns.GetCampaigns;
using Rawaj.Application.Features.Campaigns.RefineCampaignPlan;
using Rawaj.Application.Features.Campaigns.ResearchCampaignCompetitors;
using Rawaj.Application.Features.Campaigns.ScheduleCampaignPosts;
using Rawaj.Application.Features.Campaigns.UnarchiveCampaign;
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
        [FromQuery] Guid? brandProfileId, [FromQuery] int page = 1, [FromQuery] int pageSize = 20,
        [FromQuery] bool includeArchived = false, CancellationToken cancellationToken = default)
    {
        var result = await sender.Send(new GetCampaignsQuery(brandProfileId, page, pageSize, includeArchived), cancellationToken);

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
            new UpdateCampaignCommand(
                campaignId, request.Name, request.Status, request.StartDate, request.EndDate, request.BudgetAmount,
                request.Objective, request.TargetPlatforms, request.BudgetCurrency, request.BriefJson,
                request.MarkOnboardingCompleted),
            cancellationToken);

        return result.Succeeded
            ? Ok(ApiResponse<GetCampaignResponse>.Success(result.Data!))
            : BadRequest(ApiResponse<GetCampaignResponse>.Fail(result.ErrorMessage!));
    }

    [HttpGet("{campaignId:guid}/delete-summary")]
    public async Task<IActionResult> GetDeleteSummary(Guid campaignId, CancellationToken cancellationToken)
    {
        var result = await sender.Send(new GetCampaignDeleteSummaryQuery(campaignId), cancellationToken);

        return result.Succeeded
            ? Ok(ApiResponse<CampaignDeleteSummaryResponse>.Success(result.Data!))
            : NotFound(ApiResponse<CampaignDeleteSummaryResponse>.Fail(result.ErrorMessage!));
    }

    [HttpDelete("{campaignId:guid}")]
    public async Task<IActionResult> Delete(Guid campaignId, CancellationToken cancellationToken)
    {
        var result = await sender.Send(new DeleteCampaignCommand(campaignId), cancellationToken);

        return result.Succeeded
            ? Ok(ApiResponse<DeleteCampaignResponse>.Success(result.Data!))
            : BadRequest(ApiResponse<DeleteCampaignResponse>.Fail(result.ErrorMessage!));
    }

    [HttpPost("{campaignId:guid}/archive")]
    public async Task<IActionResult> Archive(Guid campaignId, CancellationToken cancellationToken)
    {
        var result = await sender.Send(new ArchiveCampaignCommand(campaignId), cancellationToken);

        return result.Succeeded
            ? Ok(ApiResponse<bool>.Success(result.Data!))
            : BadRequest(ApiResponse<bool>.Fail(result.ErrorMessage!));
    }

    [HttpPost("{campaignId:guid}/unarchive")]
    public async Task<IActionResult> Unarchive(Guid campaignId, CancellationToken cancellationToken)
    {
        var result = await sender.Send(new UnarchiveCampaignCommand(campaignId), cancellationToken);

        return result.Succeeded
            ? Ok(ApiResponse<UnarchiveCampaignResponse>.Success(result.Data!))
            : BadRequest(ApiResponse<UnarchiveCampaignResponse>.Fail(result.ErrorMessage!));
    }

    [EnableRateLimiting("ai-generation")]
    [HttpPost("{campaignId:guid}/research-competitors")]
    public async Task<IActionResult> ResearchCompetitors(Guid campaignId, CancellationToken cancellationToken)
    {
        var result = await sender.Send(new ResearchCampaignCompetitorsCommand(campaignId), cancellationToken);

        return result.Succeeded
            ? Ok(ApiResponse<ResearchCampaignCompetitorsResponse>.Success(result.Data!))
            : BadRequest(ApiResponse<ResearchCampaignCompetitorsResponse>.Fail(result.ErrorMessage!));
    }

    [EnableRateLimiting("ai-generation")]
    [HttpPost("{campaignId:guid}/diagnose-business")]
    public async Task<IActionResult> DiagnoseBusiness(Guid campaignId, CancellationToken cancellationToken)
    {
        var result = await sender.Send(new GenerateBusinessDiagnosisCommand(campaignId), cancellationToken);

        return result.Succeeded
            ? Ok(ApiResponse<GenerateBusinessDiagnosisResponse>.Success(result.Data!))
            : BadRequest(ApiResponse<GenerateBusinessDiagnosisResponse>.Fail(result.ErrorMessage!));
    }

    [EnableRateLimiting("ai-generation")]
    [HttpPost("{campaignId:guid}/generate-plan")]
    public async Task<IActionResult> GeneratePlan(Guid campaignId, CancellationToken cancellationToken)
    {
        var result = await sender.Send(new GenerateMarketingPlanCommand(campaignId), cancellationToken);

        return result.Succeeded
            ? Ok(ApiResponse<GenerateMarketingPlanResponse>.Success(result.Data!))
            : BadRequest(ApiResponse<GenerateMarketingPlanResponse>.Fail(result.ErrorMessage!));
    }

    [EnableRateLimiting("ai-generation")]
    [HttpPost("{campaignId:guid}/refine-plan")]
    public async Task<IActionResult> RefinePlan(Guid campaignId, [FromBody] RefineCampaignPlanRequest request, CancellationToken cancellationToken)
    {
        var result = await sender.Send(new RefineCampaignPlanCommand(campaignId, request.Feedback), cancellationToken);

        return result.Succeeded
            ? Ok(ApiResponse<RefineCampaignPlanResponse>.Success(result.Data!))
            : BadRequest(ApiResponse<RefineCampaignPlanResponse>.Fail(result.ErrorMessage!));
    }

    [HttpPost("{campaignId:guid}/approve-plan")]
    public async Task<IActionResult> ApprovePlan(Guid campaignId, CancellationToken cancellationToken)
    {
        var result = await sender.Send(new ApproveCampaignPlanCommand(campaignId), cancellationToken);

        return result.Succeeded
            ? Ok(ApiResponse<ApproveCampaignPlanResponse>.Success(result.Data!))
            : BadRequest(ApiResponse<ApproveCampaignPlanResponse>.Fail(result.ErrorMessage!));
    }

    [HttpPost("{campaignId:guid}/schedule-posts")]
    public async Task<IActionResult> SchedulePosts(Guid campaignId, [FromBody] ScheduleCampaignPostsRequest? request, CancellationToken cancellationToken)
    {
        var result = await sender.Send(new ScheduleCampaignPostsCommand(campaignId, request?.PublishPastDueNow ?? false), cancellationToken);

        return result.Succeeded
            ? Ok(ApiResponse<ScheduleCampaignPostsResponse>.Success(result.Data!))
            : BadRequest(ApiResponse<ScheduleCampaignPostsResponse>.Fail(result.ErrorMessage!));
    }

    [EnableRateLimiting("ai-generation")]
    [HttpPost("{campaignId:guid}/generate-content")]
    public async Task<IActionResult> GenerateContent(Guid campaignId, [FromBody] GenerateCampaignContentRequest request, CancellationToken cancellationToken)
    {
        var result = await sender.Send(
            new GenerateCampaignContentCommand(
                campaignId, request.PostCount, request.Language, request.IncludeImages, request.TemplateStyle),
            cancellationToken);

        return result.Succeeded
            ? Ok(ApiResponse<GenerateCampaignContentResponse>.Success(result.Data!))
            : BadRequest(ApiResponse<GenerateCampaignContentResponse>.Fail(result.ErrorMessage!));
    }

    public record GenerateCampaignContentRequest(int PostCount, Language Language, bool IncludeImages = true, ContentTemplateStyle TemplateStyle = ContentTemplateStyle.Auto);

    public record ScheduleCampaignPostsRequest(bool PublishPastDueNow = false);

    public record RefineCampaignPlanRequest(string Feedback);

    public record UpdateCampaignRequest(
        string? Name, CampaignStatus? Status, DateOnly? StartDate, DateOnly? EndDate, decimal? BudgetAmount,
        string? Objective = null, List<string>? TargetPlatforms = null, string? BudgetCurrency = null,
        string? BriefJson = null, bool MarkOnboardingCompleted = false);
}
