using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Rawaj.Application.Common.Interfaces;
using Rawaj.Application.Features.Scheduling.Common;

namespace Rawaj.Infrastructure.BackgroundJobs;

/// <summary>
/// Confirms posts handed off to a platform's native scheduler actually went live. Polls every 5
/// minutes - frequent enough to catch failures reasonably promptly, far less often than the 30s
/// local-publish poller since native scheduling can legitimately take longer to settle.
/// </summary>
public class PostStatusSyncHostedService(
    IServiceScopeFactory scopeFactory,
    ILogger<PostStatusSyncHostedService> logger) : BackgroundService
{
    private static readonly TimeSpan PollInterval = TimeSpan.FromMinutes(5);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = scopeFactory.CreateScope();
                var dbContext = scope.ServiceProvider.GetRequiredService<IApplicationDbContext>();
                var tokenEncryptor = scope.ServiceProvider.GetRequiredService<ITokenEncryptor>();
                var statusCheckers = scope.ServiceProvider.GetRequiredService<IEnumerable<ISocialPostStatusChecker>>();

                await PostStatusSyncer.SyncNativelyScheduledPostsAsync(dbContext, tokenEncryptor, statusCheckers, logger, stoppingToken);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                logger.LogError(ex, "Post status sync tick failed.");
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
}
