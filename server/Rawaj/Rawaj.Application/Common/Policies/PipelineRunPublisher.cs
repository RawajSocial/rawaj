using System.Text.Json;
using Rawaj.Application.Common.Interfaces;
using Rawaj.Domain.Entities.AiOperations;
using Rawaj.Domain.Entities.Platform;
using Rawaj.Domain.Enums;

namespace Rawaj.Application.Common.Policies;

public record PipelineStageStatusPayload(
    AiPipelineStageKind Kind, AiPipelineStageStatus Status, int Attempts, int MaxAttempts, string? LastError,
    Guid? TargetRefId);

/// <summary>The outbox payload shape for a "PipelineRunUpdated" delivery - shaped to match
/// GetRunStatusResponse field-for-field so the frontend can apply a pushed update the same way it
/// applies a polled one, without a second translation step.</summary>
public record PipelineRunUpdatedPayload(
    Guid RunId,
    Guid UserId,
    AiPipelineRunStatus Status,
    PipelineProgress Progress,
    int TotalCoinsSpent,
    string? LastError,
    IReadOnlyList<PipelineStageStatusPayload> Stages,
    int Version);

/// <summary>
/// Queues a "run status changed" OutboxMessage so PipelineOrchestrator's own SaveChangesAsync
/// commits it atomically with the status change it describes - same pattern and guarantee as
/// <see cref="NotificationPublisher"/>. Lets the frontend replace 2-second HTTP polling with a
/// SignalR push, mirroring how notifications stopped needing to be polled every few seconds.
/// </summary>
public static class PipelineRunPublisher
{
    public const string PipelineRunUpdatedType = "PipelineRunUpdated";

    /// <param name="version">Passed explicitly rather than read off <paramref name="run"/>.Version —
    /// the per-stage-completion caller bumps the row's version via an atomic <c>ExecuteUpdateAsync</c>
    /// that bypasses the change tracker, and assigning the fresh value back onto the tracked entity
    /// would mark it dirty with a now-stale in-memory RowVersion, causing a spurious
    /// DbUpdateConcurrencyException on that scope's next SaveChangesAsync.</param>
    /// <param name="totalCoinsSpent">Same reasoning as <paramref name="version"/>: coin charges are
    /// now also applied via an atomic <c>ExecuteUpdateAsync</c> (see
    /// <c>AiPipelineCoinPolicy.TryChargeAsync</c>), so <paramref name="run"/>.TotalCoinsSpent can be
    /// stale by the time this is called.</param>
    public static void Queue(
        IApplicationDbContext dbContext, AiPipelineRun run, IReadOnlyCollection<AiPipelineStage> stages,
        int version, int totalCoinsSpent)
    {
        var progress = AiPipelineProgressPolicy.Calculate(stages, run.Status);
        var stagePayloads = stages
            .Select(s => new PipelineStageStatusPayload(s.Kind, s.Status, s.Attempts, s.MaxAttempts, s.LastError, s.TargetRefId))
            .ToList();

        var payload = new PipelineRunUpdatedPayload(
            run.Id, run.TriggeredBy, run.Status, progress, totalCoinsSpent, run.LastError, stagePayloads, version);

        dbContext.OutboxMessages.Add(new OutboxMessage
        {
            Id = Guid.NewGuid(),
            Type = PipelineRunUpdatedType,
            PayloadJson = JsonSerializer.Serialize(payload),
            CreatedAt = DateTime.UtcNow
        });
    }
}
