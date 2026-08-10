using MediatR;
using Rawaj.Application.Common.Interfaces;
using Rawaj.Application.Common.Models;

namespace Rawaj.Application.Features.Billing.Webhooks;

public class VerifyCheckoutSessionCommandHandler(
    ICurrentTenantContext currentTenantContext, IPaymentGatewayService paymentGateway, ISender sender)
    : IRequestHandler<VerifyCheckoutSessionCommand, Result<bool>>
{
    public async Task<Result<bool>> Handle(VerifyCheckoutSessionCommand request, CancellationToken cancellationToken)
    {
        var evt = await paymentGateway.RetrieveCheckoutSessionAsync(request.SessionId, cancellationToken);
        if (evt is null)
        {
            // Not confirmed yet (still processing, or the session belongs to an abandoned/cancelled
            // checkout) — not an error, just "nothing to fulfil right now".
            return Result<bool>.Success(false);
        }

        if (!evt.Metadata.TryGetValue("tenantId", out var tenantIdRaw)
            || !Guid.TryParse(tenantIdRaw, out var tenantId)
            || tenantId != currentTenantContext.TenantId)
        {
            return Result<bool>.Failure("Checkout session does not belong to this tenant.");
        }

        // Reuses the exact same fulfillment path a real webhook delivery would take — idempotent via
        // IBillingFulfillmentService.IsSessionAlreadyProcessedAsync, so calling this twice (or a real
        // webhook later arriving for the same session) is harmless.
        await sender.Send(new HandleStripeWebhookEventCommand(evt), cancellationToken);

        return Result<bool>.Success(true);
    }
}
