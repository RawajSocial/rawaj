using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Rawaj.Application.Common.Interfaces;
using Rawaj.Application.Common.Policies;
using Rawaj.Domain.Entities.Platform;

namespace Rawaj.Infrastructure.BackgroundJobs;

/// <summary>
/// Polls OutboxMessages for unprocessed rows and delivers them — today, only "NotificationCreated"
/// is understood, broadcast over SignalR. This is the single place future delivery channels
/// (email/push/webhook) would plug in without touching NotificationPublisher or any handler.
///
/// Single-instance safe only: this doesn't yet claim rows atomically (e.g. a DB-level
/// "UPDATE ... SET ProcessedAt = @now OUTPUT ... WHERE ProcessedAt IS NULL" claim), so running two
/// instances of this service concurrently could double-deliver a notification. Fine for today's
/// single-instance deployment; add row-claiming before horizontally scaling (see Phase 5 notes).
/// </summary>
public class OutboxDispatcherHostedService(
    IServiceScopeFactory scopeFactory,
    ILogger<OutboxDispatcherHostedService> logger) : BackgroundService
{
    private static readonly TimeSpan PollInterval = TimeSpan.FromSeconds(3);
    private const int MaxAttempts = 5;
    private const int BatchSize = 50;

    /// <summary>The message types this dispatcher claims. Other types belong to another service and
    /// are left alone — see the note in <see cref="DispatchPendingAsync"/>.</summary>
    private static readonly string[] HandledTypes = [NotificationPublisher.NotificationCreatedType];

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await DispatchPendingAsync(stoppingToken);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                logger.LogError(ex, "Outbox dispatch tick failed.");
            }

            try
            {
                await Task.Delay(PollInterval, stoppingToken);
            }
            catch (OperationCanceledException)
            {
                // shutting down
            }
        }
    }

    private async Task DispatchPendingAsync(CancellationToken cancellationToken)
    {
        using var scope = scopeFactory.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<IApplicationDbContext>();
        var broadcaster = scope.ServiceProvider.GetRequiredService<INotificationBroadcaster>();

        // Scoped to the types this service owns. The outbox is shared with
        // CampaignCleanupHostedService, whose messages are slow (many Cloudinary round-trips) and
        // must not park notification delivery behind them — nor be burned through this service's
        // attempt budget by the "unknown type" throw below.
        var pending = await dbContext.OutboxMessages
            .Where(m => HandledTypes.Contains(m.Type) && m.ProcessedAt == null && m.Attempts < MaxAttempts)
            .OrderBy(m => m.CreatedAt)
            .Take(BatchSize)
            .ToListAsync(cancellationToken);

        if (pending.Count == 0)
        {
            return;
        }

        foreach (var message in pending)
        {
            try
            {
                await DeliverAsync(message, broadcaster, cancellationToken);
                message.ProcessedAt = DateTime.UtcNow;
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                message.Attempts++;
                message.LastError = ex.Message;
                logger.LogWarning(ex, "Outbox message {MessageId} (type {Type}) delivery attempt {Attempt} failed.",
                    message.Id, message.Type, message.Attempts);
            }
        }

        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private static async Task DeliverAsync(OutboxMessage message, INotificationBroadcaster broadcaster, CancellationToken cancellationToken)
    {
        switch (message.Type)
        {
            case NotificationPublisher.NotificationCreatedType:
                var payload = JsonSerializer.Deserialize<NotificationCreatedPayload>(message.PayloadJson)
                    ?? throw new InvalidOperationException("Outbox message payload deserialized to null.");
                await broadcaster.BroadcastAsync(payload.UserId, "notificationReceived", payload, cancellationToken);
                break;
            default:
                // Unknown type — likely a message written by a newer app version during a rolling
                // deploy. Leave it for a future dispatcher revision rather than failing forever.
                throw new InvalidOperationException($"Unknown outbox message type: '{message.Type}'.");
        }
    }
}
