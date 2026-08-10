using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Rawaj.Application.Common.Policies;
using Rawaj.Application.Features.Billing.ChangeSubscriptionPlan;
using Rawaj.Application.Features.Billing.GetAiCreditsUsage;
using Rawaj.Application.Features.Billing.GetBillingHistory;
using Rawaj.Application.Features.Billing.GetCampaignsUsage;
using Rawaj.Application.Features.Billing.GetCoinPricing;
using Rawaj.Application.Features.Billing.GetPublicCoinPricing;
using Rawaj.Application.Features.Billing.GetSubscription;
using Rawaj.Application.Features.Billing.GetSubscriptionPlans;
using Rawaj.Application.Features.Billing.Common;
using Rawaj.Application.Features.Billing.PurchaseAddOn;
using Rawaj.Application.Features.Billing.PurchaseCoins;
using Rawaj.Application.Features.Billing.Webhooks;
using Rawaj.Common;

namespace Rawaj.Controllers;

[ApiController]
[Authorize]
[Route("api/v1/subscriptions")]
public class SubscriptionsController(ISender sender) : ControllerBase
{
    /// <summary>Anonymous — the landing page and public pricing page list plan tiers before login.</summary>
    [AllowAnonymous]
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

    [HttpGet("ai-credits")]
    public async Task<IActionResult> GetAiCreditsUsage(CancellationToken cancellationToken)
    {
        var result = await sender.Send(new GetAiCreditsUsageQuery(), cancellationToken);

        return result.Succeeded
            ? Ok(ApiResponse<AiCreditsUsage>.Success(result.Data!))
            : BadRequest(ApiResponse<AiCreditsUsage>.Fail(result.ErrorMessage!));
    }

    [HttpGet("campaigns-usage")]
    public async Task<IActionResult> GetCampaignsUsage(CancellationToken cancellationToken)
    {
        var result = await sender.Send(new GetCampaignsUsageQuery(), cancellationToken);

        return result.Succeeded
            ? Ok(ApiResponse<CampaignsUsage>.Success(result.Data!))
            : BadRequest(ApiResponse<CampaignsUsage>.Fail(result.ErrorMessage!));
    }

    [HttpPost("change-plan")]
    public async Task<IActionResult> ChangePlan(ChangeSubscriptionPlanCommand command, CancellationToken cancellationToken)
    {
        var result = await sender.Send(command, cancellationToken);

        return result.Succeeded
            ? Ok(ApiResponse<ChangeSubscriptionPlanResponse>.Success(result.Data!))
            : BadRequest(ApiResponse<ChangeSubscriptionPlanResponse>.Fail(result.ErrorMessage!));
    }

    [HttpGet("coin-pricing")]
    public async Task<IActionResult> GetCoinPricing(CancellationToken cancellationToken)
    {
        var result = await sender.Send(new GetCoinPricingQuery(), cancellationToken);

        return result.Succeeded
            ? Ok(ApiResponse<GetCoinPricingResponse>.Success(result.Data!))
            : BadRequest(ApiResponse<GetCoinPricingResponse>.Fail(result.ErrorMessage!));
    }

    /// <summary>Anonymous — base coin costs/packages/add-on prices with no per-tenant discount or
    /// free-trial state, for the landing page and public pricing page.</summary>
    [AllowAnonymous]
    [HttpGet("public-coin-pricing")]
    public async Task<IActionResult> GetPublicCoinPricing(CancellationToken cancellationToken)
    {
        var result = await sender.Send(new GetPublicCoinPricingQuery(), cancellationToken);

        return result.Succeeded
            ? Ok(ApiResponse<GetPublicCoinPricingResponse>.Success(result.Data!))
            : BadRequest(ApiResponse<GetPublicCoinPricingResponse>.Fail(result.ErrorMessage!));
    }

    /// <summary>Starts a real Stripe Checkout payment and returns its URL — the browser must be
    /// redirected there; coins are granted only once the webhook confirms payment.</summary>
    [HttpPost("purchase-coins")]
    public async Task<IActionResult> PurchaseCoins(CreatePurchaseCoinsCheckoutSessionCommand command, CancellationToken cancellationToken)
    {
        var result = await sender.Send(command, cancellationToken);

        return result.Succeeded
            ? Ok(ApiResponse<CreateCheckoutSessionResponse>.Success(result.Data!))
            : BadRequest(ApiResponse<CreateCheckoutSessionResponse>.Fail(result.ErrorMessage!));
    }

    /// <summary>Starts a real Stripe Checkout payment and returns its URL — the browser must be
    /// redirected there; the add-on is granted only once the webhook confirms payment.</summary>
    [HttpPost("purchase-add-on")]
    public async Task<IActionResult> PurchaseAddOn(CreatePurchaseAddOnCheckoutSessionCommand command, CancellationToken cancellationToken)
    {
        var result = await sender.Send(command, cancellationToken);

        return result.Succeeded
            ? Ok(ApiResponse<CreateCheckoutSessionResponse>.Success(result.Data!))
            : BadRequest(ApiResponse<CreateCheckoutSessionResponse>.Fail(result.ErrorMessage!));
    }

    /// <summary>The "verify on return" fallback used when no Stripe webhook is configured — call
    /// right after the browser returns from Checkout with the <c>session_id</c> Stripe appended to
    /// the success URL. Returns <c>true</c> once fulfilled, <c>false</c> if not confirmed yet (safe
    /// to retry).</summary>
    [HttpPost("checkout/verify")]
    public async Task<IActionResult> VerifyCheckoutSession(VerifyCheckoutSessionCommand command, CancellationToken cancellationToken)
    {
        var result = await sender.Send(command, cancellationToken);

        return result.Succeeded
            ? Ok(ApiResponse<bool>.Success(result.Data))
            : BadRequest(ApiResponse<bool>.Fail(result.ErrorMessage!));
    }

    [HttpGet("history")]
    public async Task<IActionResult> GetHistory([FromQuery] int page, [FromQuery] int pageSize, CancellationToken cancellationToken)
    {
        var result = await sender.Send(
            new GetBillingHistoryQuery(page == 0 ? 1 : page, pageSize == 0 ? 20 : pageSize), cancellationToken);

        return result.Succeeded
            ? Ok(ApiResponse<GetBillingHistoryResponse>.Success(result.Data!))
            : BadRequest(ApiResponse<GetBillingHistoryResponse>.Fail(result.ErrorMessage!));
    }
}
