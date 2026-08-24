using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Rawaj.Application.Common.Interfaces;
using Rawaj.Application.Features.SocialAccounts.Common;

namespace Rawaj.Infrastructure.BackgroundJobs;

/// <summary>
/// Periodically renews social account tokens nearing expiry so publishing doesn't silently start
/// failing weeks after a user connected an account. Runs every 6 hours, not on the 30s cadence of
/// ScheduledPostPublisherHostedService, since token windows are measured in weeks, not minutes.
/// </summary>
public class SocialAccountTokenRefresherHostedService(
    IServiceScopeFactory scopeFactory,
    ILogger<SocialAccountTokenRefresherHostedService> logger) : BackgroundService
{
    private static readonly TimeSpan PollInterval = TimeSpan.FromHours(6);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = scopeFactory.CreateScope();
                var dbContext = scope.ServiceProvider.GetRequiredService<IApplicationDbContext>();
                var tokenEncryptor = scope.ServiceProvider.GetRequiredService<ITokenEncryptor>();
                var providers = scope.ServiceProvider.GetRequiredService<IEnumerable<ISocialOAuthProvider>>();

                await SocialAccountTokenRefresher.RefreshDueTokensAsync(dbContext, tokenEncryptor, providers, logger, stoppingToken);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                logger.LogError(ex, "Social account token refresh tick failed.");
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
