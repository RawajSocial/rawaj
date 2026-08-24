using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Rawaj.Application.Common.Interfaces;
using Rawaj.Application.Features.Billing.Webhooks;

namespace Rawaj.Controllers;

/// <summary>
/// Receives Stripe's webhook callbacks — the only proof that a Checkout payment or subscription
/// renewal actually succeeded (see <c>HandleStripeWebhookEventCommandHandler</c>'s doc comment).
/// Called directly by Stripe's servers with no JWT, hence <see cref="AllowAnonymousAttribute"/> at
/// the class level rather than a per-action one.
/// </summary>
[ApiController]
[AllowAnonymous]
[Route("api/v1/webhooks/stripe")]
public class StripeWebhookController(ISender sender, IPaymentGatewayService paymentGateway, ILogger<StripeWebhookController> logger)
    : ControllerBase
{
    [HttpPost]
    public async Task<IActionResult> Handle(CancellationToken cancellationToken)
    {
        // Must read the raw body directly rather than bind a DTO parameter — model binding would
        // consume the stream before signature verification can run against the exact bytes Stripe
        // signed, and a re-serialized/re-parsed body would no longer match the signature.
        using var reader = new StreamReader(Request.Body);
        var payload = await reader.ReadToEndAsync(cancellationToken);
        var signature = Request.Headers["Stripe-Signature"].ToString();

        Application.Common.Models.PaymentWebhookEvent stripeEvent;
        try
        {
            stripeEvent = paymentGateway.ParseAndVerifyWebhookEvent(payload, signature);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Rejected a Stripe webhook delivery with an invalid signature.");
            return BadRequest();
        }

        var result = await sender.Send(new HandleStripeWebhookEventCommand(stripeEvent), cancellationToken);

        // 200 even for event types we don't act on, so Stripe stops retrying them. Only a genuine
        // processing failure should surface as non-200 and get retried.
        return result.Succeeded ? Ok() : StatusCode(StatusCodes.Status500InternalServerError);
    }
}
