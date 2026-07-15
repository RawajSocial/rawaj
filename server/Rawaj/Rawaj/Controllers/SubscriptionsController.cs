using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Rawaj.Application.Features.Billing.ChangeSubscriptionPlan;
using Rawaj.Application.Features.Billing.GetSubscription;
using Rawaj.Application.Features.Billing.GetSubscriptionPlans;
using Rawaj.Common;

namespace Rawaj.Controllers;

[ApiController]
[Authorize]
[Route("api/v1/subscriptions")]
public class SubscriptionsController(ISender sender) : ControllerBase
{
    [HttpGet("plans")]
    public async Task<IActionResult> GetPlans(CancellationToken cancellationToken)
    {
        var result = await sender.Send(new GetSubscriptionPlansQuery(), cancellationToken);

        return result.Succeeded
            ? Ok(ApiResponse<List<SubscriptionPlanSummary>>.Success(result.Data!))
            : BadRequest(ApiResponse<List<SubscriptionPlanSummary>>.Fail(result.ErrorMessage!));
    }

    [HttpGet("me")]
    public async Task<IActionResult> GetMine(CancellationToken cancellationToken)
    {
        var result = await sender.Send(new GetSubscriptionQuery(), cancellationToken);

        return result.Succeeded
            ? Ok(ApiResponse<GetSubscriptionResponse>.Success(result.Data!))
            : BadRequest(ApiResponse<GetSubscriptionResponse>.Fail(result.ErrorMessage!));
    }

    [HttpPost("change-plan")]
    public async Task<IActionResult> ChangePlan(ChangeSubscriptionPlanCommand command, CancellationToken cancellationToken)
    {
        var result = await sender.Send(command, cancellationToken);

        return result.Succeeded
            ? Ok(ApiResponse<ChangeSubscriptionPlanResponse>.Success(result.Data!))
            : BadRequest(ApiResponse<ChangeSubscriptionPlanResponse>.Fail(result.ErrorMessage!));
    }
}
