using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Rawaj.Application.Common.Interfaces;
using Rawaj.Application.Common.Policies;
using Rawaj.Application.Features.Campaigns.DeleteCampaign;
using Rawaj.Domain.Entities.Platform;
using Rawaj.Domain.Enums;

namespace Rawaj.Infrastructure.BackgroundJobs;

/// <summary>
/// Finishes a campaign delete off the request thread: revokes any scheduled post the platform had
/// already agreed to publish, then purges the campaign's images from media storage.
///
/// <para>This runs as its own service rather than as another case in
/// <c>OutboxDispatcherHostedService</c> on purpose. A campaign with a dozen images is a dozen
/// sequential Cloudinary round-trips; sharing the dispatcher's loop would have parked every pending
/// SignalR notification behind that for the duration. The two services claim disjoint message types
/// from the same table, so neither steals the other's work.</para>
///
/// <para>Retries come from the outbox itself — the message is only marked processed once the whole
/// cleanup succeeds, and both halves are idempotent (Cloudinary treats "not found" as success;
/// <c>CancelNativeAsync</c> no-ops on a post with no platform handoff), so re-running a partly
/// finished message is safe. Giving up after <see cref="MaxAttempts"/> tells the user rather than
/// failing silently: the campaign is gone from their side either way, but a post still sitting in
/// Facebook's own scheduler is something they need to know to remove by hand.</para>
///
/// <para>Every query here needs <c>IgnoreQueryFilters</c>: the rows this cleans up were soft-deleted
/// by the command that queued the message, so the global filters hide them from ordinary reads.</para>
/// </summary>
public class CampaignCleanupHostedService(
    IServiceScopeFactory scopeFactory,
    ILogger<CampaignCleanupHostedService> logger) : BackgroundService
{
    // Slower than the notification dispatcher's 3s on purpose. Nobody is waiting on this — the
    // campaign is already gone from the user's view — and the interval doubles as the retry spacing,
    // since OutboxMessage records an attempt count but not when the last one happened. Six attempts
    // fifteen seconds apart covers a brief Graph API or Cloudinary blip; a longer outage than that
    // ends in the "cleanup incomplete" notification rather than silent retries forever. Proper
    // exponential backoff would need a LastAttemptAt column and a migration.
    private static readonly TimeSpan PollInterval = TimeSpan.FromSeconds(15);
    private const int MaxAttempts = 6;
    private const int BatchSize = 5;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ProcessPendingAsync(stoppingToken);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                logger.LogError(ex, "Campaign cleanup tick failed.");
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

    private async Task ProcessPendingAsync(CancellationToken cancellationToken)
    {
        using var scope = scopeFactory.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<IApplicationDbContext>();

        var pending = await dbContext.OutboxMessages
            .Where(m => m.Type == CampaignCleanupPublisher.CampaignCleanupType
                        && m.ProcessedAt == null
                        && m.Attempts < MaxAttempts)
            .OrderBy(m => m.CreatedAt)
            .Take(BatchSize)
            .ToListAsync(cancellationToken);

        if (pending.Count == 0)
        {
            return;
        }

        var tokenEncryptor = scope.ServiceProvider.GetRequiredService<ITokenEncryptor>();
        var publishers = scope.ServiceProvider.GetRequiredService<IEnumerable<ISocialPublisher>>();
        var mediaStorage = scope.ServiceProvider.GetRequiredService<IMediaStorageService>();

        foreach (var message in pending)
        {
            try
            {
                var payload = JsonSerializer.Deserialize<CampaignCleanupPayload>(message.PayloadJson)
                    ?? throw new InvalidOperationException("Campaign cleanup payload deserialized to null.");

                await CampaignCleanupExecutor.ExecuteAsync(
                    dbContext, tokenEncryptor, publishers, mediaStorage, payload, cancellationToken);
                message.ProcessedAt = DateTime.UtcNow;
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                message.Attempts++;
                message.LastError = ex.Message;
                logger.LogWarning(ex, "Campaign cleanup message {MessageId} attempt {Attempt} failed.",
                    message.Id, message.Attempts);

                if (message.Attempts >= MaxAttempts)
                {
                    NotifyGaveUp(dbContext, message, ex);
                }
            }
        }

        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private static void NotifyGaveUp(IApplicationDbContext dbContext, OutboxMessage message, Exception ex)
    {
        var payload = JsonSerializer.Deserialize<CampaignCleanupPayload>(message.PayloadJson);
        if (payload is null)
        {
            return;
        }

        NotificationPublisher.Notify(
            dbContext, payload.RequestedByUserId, payload.BrandProfileId,
            NotificationType.Warning, NotificationCategory.System,
            "Campaign cleanup incomplete",
            $"\"{payload.CampaignName}\" was deleted, but some of its scheduled posts or images could not be " +
            $"removed from the platform: {ex.Message}. You may need to remove them manually.",
            payload.CampaignId, "marketing_campaign");
    }
}
