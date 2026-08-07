using System.Text.Json;
using Rawaj.Application.Common.Interfaces;
using Rawaj.Domain.Entities.AiOperations;
using Rawaj.Domain.Entities.Platform;
using Rawaj.Domain.Enums;

namespace Rawaj.Application.Common.Policies;

public record PipelineStageStatusPayload(
    AiPipelineStageKind Kind, AiPipelineStageStatus Status, int Attempts, int MaxAttempts, string? LastError);

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
    IReadOnlyList<PipelineStageStatusPayload> Stages);

/// <summary>
/// Queues a "run status changed" OutboxMessage so PipelineOrchestrator's own SaveChangesAsync
/// commits it atomically with the status change it describes - same pattern and guarantee as
/// <see cref="NotificationPublisher"/>. Lets the frontend replace 2-second HTTP polling with a
/// SignalR push, mirroring how notifications stopped needing to be polled every few seconds.
/// </summary>
public static class PipelineRunPublisher
{
    public const string PipelineRunUpdatedType = "PipelineRunUpdated";

    public static void Queue(
        IApplicationDbContext dbContext, AiPipelineRun run, IReadOnlyCollection<AiPipelineStage> stages)
    {
        var progress = AiPipelineProgressPolicy.Calculate(stages, run.Status);
        var stagePayloads = stages
            .Select(s => new PipelineStageStatusPayload(s.Kind, s.Status, s.Attempts, s.MaxAttempts, s.LastError))
            .ToList();

        var payload = new PipelineRunUpdatedPayload(
            run.Id, run.TriggeredBy, run.Status, progress, run.TotalCoinsSpent, run.LastError, stagePayloads);

        dbContext.OutboxMessages.Add(new OutboxMessage
        {
            Id = Guid.NewGuid(),
            Type = PipelineRunUpdatedType,
            PayloadJson = JsonSerializer.Serialize(payload),
            CreatedAt = DateTime.UtcNow
        });
    }
}
