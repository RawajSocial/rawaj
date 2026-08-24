using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Rawaj.Application.Common.Interfaces;
using Rawaj.Application.Features.Scheduling.Common;
using Rawaj.Domain.Enums;

namespace Rawaj.Infrastructure.BackgroundJobs;

/// <summary>
/// Fallback publisher for platforms without native scheduling support. SchedulePost hands posts
/// off to the platform's own scheduler immediately (e.g. Facebook's scheduled_publish_time), so
/// a row with PostId already set means the platform itself is responsible for publishing it —
/// this poller must skip those or it would create a duplicate post. It only acts on Pending rows
/// that never got a PostId, i.e. platforms with no ISocialPublisher-side native scheduling.
/// Runs as a system process with no tenant/user context, so it calls ScheduledPostPublisher
/// directly rather than going through the RBAC-gated MediatR command.
/// </summary>
public class ScheduledPostPublisherHostedService(
    IServiceScopeFactory scopeFactory,
    ILogger<ScheduledPostPublisherHostedService> logger) : BackgroundService
{
    private static readonly TimeSpan PollInterval = TimeSpan.FromSeconds(30);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await PublishDuePostsAsync(stoppingToken);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                logger.LogError(ex, "Scheduled post publisher tick failed.");
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

    private async Task PublishDuePostsAsync(CancellationToken cancellationToken)
    {
        using var scope = scopeFactory.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<IApplicationDbContext>();
        var tokenEncryptor = scope.ServiceProvider.GetRequiredService<ITokenEncryptor>();
        var publishers = scope.ServiceProvider.GetRequiredService<IEnumerable<ISocialPublisher>>();

        var now = DateTime.UtcNow;

        var duePostIds = await dbContext.ScheduledPosts
            .Where(s => s.Status == ScheduledPostStatus.Pending && s.ScheduledAt <= now && s.PostId == null)
            .Select(s => s.Id)
            .ToListAsync(cancellationToken);

        foreach (var scheduledPostId in duePostIds)
        {
            var result = await ScheduledPostPublisher.PublishNowAsync(
                dbContext, tokenEncryptor, publishers, scheduledPostId, cancellationToken);

            if (!result.Succeeded)
            {
                logger.LogWarning("Failed to publish scheduled post {ScheduledPostId}: {Error}", scheduledPostId, result.ErrorMessage);
            }
        }
    }
}
