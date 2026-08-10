using Microsoft.EntityFrameworkCore;
using Rawaj.Application.Common.Interfaces;
using Rawaj.Domain.Entities.Billing;

namespace Rawaj.Application.Features.Billing.Common;

public class PendingCheckoutSessionService(IApplicationDbContext dbContext) : IPendingCheckoutSessionService
{
    public async Task<string?> FindOpenCheckoutUrlAsync(Guid tenantId, string intentKey, CancellationToken cancellationToken)
    {
        var now = DateTime.UtcNow;

        var existing = await dbContext.PendingCheckoutSessions
            .FirstOrDefaultAsync(p => p.TenantId == tenantId && p.IntentKey == intentKey, cancellationToken);

        if (existing is null) return null;

        // Stale (Stripe would no longer accept a payment against it) — not reusable, and there's no
        // point keeping the row around since TrackAsync will overwrite it for the next attempt.
        if (existing.ExpiresAt <= now)
        {
            dbContext.PendingCheckoutSessions.Remove(existing);
            await dbContext.SaveChangesAsync(cancellationToken);
            return null;
        }

        return existing.CheckoutUrl;
    }

    public async Task TrackAsync(
        Guid tenantId, string intentKey, string stripeSessionId, string checkoutUrl, DateTime expiresAt,
        CancellationToken cancellationToken)
    {
        var existing = await dbContext.PendingCheckoutSessions
            .FirstOrDefaultAsync(p => p.TenantId == tenantId && p.IntentKey == intentKey, cancellationToken);

        if (existing is null)
        {
            dbContext.PendingCheckoutSessions.Add(new PendingCheckoutSession
            {
                Id = Guid.NewGuid(),
                TenantId = tenantId,
                IntentKey = intentKey,
                StripeSessionId = stripeSessionId,
                CheckoutUrl = checkoutUrl,
                CreatedAt = DateTime.UtcNow,
                ExpiresAt = expiresAt,
            });
        }
        else
        {
            existing.StripeSessionId = stripeSessionId;
            existing.CheckoutUrl = checkoutUrl;
            existing.ExpiresAt = expiresAt;
        }

        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task ClearAsync(string stripeSessionId, CancellationToken cancellationToken)
    {
        var existing = await dbContext.PendingCheckoutSessions
            .FirstOrDefaultAsync(p => p.StripeSessionId == stripeSessionId, cancellationToken);
        if (existing is null) return;

        dbContext.PendingCheckoutSessions.Remove(existing);
        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
