using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Rawaj.Application.Common.Models;
using Rawaj.Application.Features.Campaigns.Common;
using Rawaj.Application.Features.Campaigns.CreateCampaignStep1;
using Rawaj.Application.Features.Campaigns.GetCampaignById;
using Rawaj.Application.Features.Campaigns.UpdateCampaignStep2;
using Rawaj.Application.Features.Campaigns.UpdateCampaignStep3;
using Rawaj.Application.Features.Campaigns.UpdateCampaignStep4;
using Rawaj.Application.Features.Campaigns.UpdateCampaignStep5;
using Rawaj.Application.Features.Campaigns.UpdateCampaignStep6;
using Rawaj.Application.Features.Campaigns.UpdateCampaignStep7;
using Rawaj.Common;

namespace Rawaj.Controllers;

[ApiController]
[Authorize]
[Route("api/v1/campaigns")]
public class CampaignsController(ISender sender) : ControllerBase
{
    [HttpPost("onboarding/step-1")]
    public async Task<IActionResult> Step1(CreateCampaignStep1Command command, CancellationToken cancellationToken) =>
        await Handle(command, cancellationToken);

    [HttpGet("{campaignId:guid}")]
    public async Task<IActionResult> Get(Guid campaignId, CancellationToken cancellationToken) =>
        await Handle(new GetCampaignByIdQuery(campaignId), cancellationToken);

    [HttpPut("{campaignId:guid}/onboarding/step-2")]
    public async Task<IActionResult> Step2(Guid campaignId, UpdateCampaignStep2Command command, CancellationToken cancellationToken) =>
        await Handle(command with { CampaignId = campaignId }, cancellationToken);

    [HttpPut("{campaignId:guid}/onboarding/step-3")]
    public async Task<IActionResult> Step3(Guid campaignId, UpdateCampaignStep3Command command, CancellationToken cancellationToken) =>
        await Handle(command with { CampaignId = campaignId }, cancellationToken);

    [HttpPut("{campaignId:guid}/onboarding/step-4")]
    public async Task<IActionResult> Step4(Guid campaignId, UpdateCampaignStep4Command command, CancellationToken cancellationToken) =>
        await Handle(command with { CampaignId = campaignId }, cancellationToken);

    [HttpPut("{campaignId:guid}/onboarding/step-5")]
    public async Task<IActionResult> Step5(Guid campaignId, UpdateCampaignStep5Command command, CancellationToken cancellationToken) =>
        await Handle(command with { CampaignId = campaignId }, cancellationToken);

    [HttpPut("{campaignId:guid}/onboarding/step-6")]
    public async Task<IActionResult> Step6(Guid campaignId, UpdateCampaignStep6Command command, CancellationToken cancellationToken) =>
        await Handle(command with { CampaignId = campaignId }, cancellationToken);

    [HttpPut("{campaignId:guid}/onboarding/step-7")]
    public async Task<IActionResult> Step7(Guid campaignId, UpdateCampaignStep7Command command, CancellationToken cancellationToken) =>
        await Handle(command with { CampaignId = campaignId }, cancellationToken);

    private async Task<IActionResult> Handle(IRequest<Result<CampaignResponse>> request, CancellationToken cancellationToken)
    {
        var result = await sender.Send(request, cancellationToken);

        return result.Succeeded
            ? Ok(ApiResponse<CampaignResponse>.Success(result.Data!))
            : BadRequest(ApiResponse<CampaignResponse>.Fail(result.ErrorMessage!));
    }
}
