using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Rawaj.Application.Common.Interfaces;
using Rawaj.Domain.Enums;

namespace Rawaj.Infrastructure.BackgroundJobs;

/// <summary>
/// Reclaims stages a crashed or killed worker left <c>Running</c>. Without this, a process that dies
/// mid-stage leaves the row claimed forever — nothing else in the system ever revisits a
/// <c>Running</c> stage on its own, since <c>AiPipelinePolicy.GetRunnableStages</c> only ever selects
/// <c>Pending</c> ones.
///
/// <para>The reclaim is a plain bulk <c>UPDATE ... WHERE LeaseExpiresAt &lt; now</c>: no per-row
/// atomicity concern here, because setting an already-expired lease's owner back to null twice (two
/// reapers running at once, say) is harmless — both writes agree on the outcome. That is different
/// from the worker's claim, where two racing writers must not both believe they won.</para>
///
/// <para>Attempt count is left untouched on reclaim — the work was never actually tried to
/// completion, so it shouldn't count against the stage's retry budget (see
/// <c>AiPipelineStage.LeaseExpiresAt</c>'s remarks).</para>
/// </summary>
public class AiPipelineReaperHostedService(
    IServiceScopeFactory scopeFactory, ILogger<AiPipelineReaperHostedService> logger) : BackgroundService
{
    private static readonly TimeSpan PollInterval = TimeSpan.FromSeconds(30);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ReclaimExpiredLeasesAsync(stoppingToken);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                logger.LogError(ex, "AI pipeline lease reaper tick failed.");
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

    private async Task ReclaimExpiredLeasesAsync(CancellationToken cancellationToken)
    {
        using var scope = scopeFactory.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<IApplicationDbContext>();
        var now = DateTime.UtcNow;

        var reclaimed = await dbContext.AiPipelineStages
            .Where(s => s.Status == AiPipelineStageStatus.Running && s.LeaseExpiresAt != null && s.LeaseExpiresAt < now)
            .ExecuteUpdateAsync(s => s
                .SetProperty(x => x.Status, AiPipelineStageStatus.Pending)
                .SetProperty(x => x.LeaseOwner, (string?)null)
                .SetProperty(x => x.LeaseExpiresAt, (DateTime?)null), cancellationToken);

        if (reclaimed > 0)
        {
            logger.LogWarning(
                "AI pipeline lease reaper reclaimed {Count} stage(s) abandoned by a crashed or killed worker.", reclaimed);
        }
    }
}
