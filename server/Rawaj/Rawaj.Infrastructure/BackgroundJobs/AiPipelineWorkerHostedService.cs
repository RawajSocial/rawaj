using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Rawaj.Application.Common.Interfaces;
using Rawaj.Application.Features.AiPipeline.Services;
using Rawaj.Domain.Enums;

namespace Rawaj.Infrastructure.BackgroundJobs;

/// <summary>
/// Advances every pipeline run that has work ready, replacing the hand-chained <c>subscribe()</c>
/// calls in <c>onboarding-plan-approval.ts</c> — where a closed browser tab strands a campaign
/// mid-generation with coins already spent and no artifact to show for it. This service is what makes
/// that impossible: the run lives entirely server-side, and it keeps being advanced whether or not
/// anyone is watching.
///
/// <para>Each candidate run is advanced in its own DI scope — its own <c>DbContext</c>, its own
/// <see cref="PipelineOrchestrator"/> — so several runs progress genuinely concurrently rather than
/// serialised behind one shared context. What keeps that safe for a single stage rather than just for
/// whole runs is <see cref="PipelineOrchestrator"/>'s atomic per-stage claim: two instances of this
/// service (a rolling deploy briefly running old and new side by side, say) racing the same
/// <c>Pending</c> stage can only ever have one of them win it.</para>
///
/// <para>The candidate query is deliberately loose — any run with a stage that looks runnable, not a
/// faithful reproduction of <c>AiPipelinePolicy.GetRunnableStages</c>'s dependency check. Over-selecting
/// costs one cheap, mostly-no-op <c>AdvanceAsync</c> call; under-selecting would silently stall a run.
/// </para>
/// </summary>
public class AiPipelineWorkerHostedService(
    IServiceScopeFactory scopeFactory, ILogger<AiPipelineWorkerHostedService> logger) : BackgroundService
{
    private static readonly TimeSpan PollInterval = TimeSpan.FromSeconds(2);

    private readonly string _workerId = $"{Environment.MachineName}:{Guid.NewGuid():N}";

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await AdvanceRunnableRunsAsync(stoppingToken);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                logger.LogError(ex, "AI pipeline worker tick failed.");
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

    private async Task AdvanceRunnableRunsAsync(CancellationToken cancellationToken)
    {
        List<Guid> runIds;

        using (var scope = scopeFactory.CreateScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<IApplicationDbContext>();
            var now = DateTime.UtcNow;

            runIds = await dbContext.AiPipelineStages
                .Where(s => s.Status == AiPipelineStageStatus.Pending && (s.NextAttemptAt == null || s.NextAttemptAt <= now))
                .Select(s => s.RunId)
                .Distinct()
                .ToListAsync(cancellationToken);
        }

        if (runIds.Count == 0)
        {
            return;
        }

        await Task.WhenAll(runIds.Select(id => AdvanceOneRunAsync(id, cancellationToken)));
    }

    private async Task AdvanceOneRunAsync(Guid runId, CancellationToken cancellationToken)
    {
        using var scope = scopeFactory.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<IApplicationDbContext>();
        var orchestrator = scope.ServiceProvider.GetRequiredService<IPipelineOrchestrator>();

        try
        {
            var run = await dbContext.AiPipelineRuns.FirstOrDefaultAsync(r => r.Id == runId, cancellationToken);

            // Re-checked here even though the candidate query already filtered on stage status: the
            // run itself could have moved to AwaitingApproval/AwaitingCoins/Cancelled between that
            // read and this scope opening, and dispatching into a parked or cancelled run would just
            // be wasted work.
            if (run is null || run.Status is not (AiPipelineRunStatus.Pending or AiPipelineRunStatus.Running))
            {
                return;
            }

            var role = await dbContext.TenantMembers
                .Where(m => m.TenantId == run.TenantId && m.UserId == run.TriggeredBy)
                .Select(m => (TenantMemberRole?)m.Role)
                .FirstOrDefaultAsync(cancellationToken) ?? TenantMemberRole.Owner;

            await orchestrator.AdvanceAsync(run, role, cancellationToken, _workerId);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            logger.LogError(ex, "AI pipeline run {RunId} failed to advance.", runId);
        }
    }
}
