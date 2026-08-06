using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Rawaj.Application.Common.Interfaces;
using Rawaj.Application.Common.Models;
using Rawaj.Domain.Entities.AiOperations;
using Rawaj.Domain.Enums;

namespace Rawaj.Application.Features.AiPipeline.Common;

/// <summary>
/// Writes the provider-call log row for a stage's model call.
///
/// <para>One place rather than one copy per executor, because the same eleven-field construction
/// repeated across every AI call site is exactly how the existing handlers ended up with drifting
/// values in it — and it is why <c>AiJob.Cost</c> is null everywhere and <c>Tokens</c> only
/// populated for text.</para>
/// </summary>
public static class AiJobRecorder
{
    public static AiJob RecordText(
        IApplicationDbContext dbContext,
        StageContext context,
        AiJobType jobType,
        string prompt,
        AiTextGenerationResult result,
        DateTime startedAt) =>
        RecordText(
            dbContext, context.TenantId, context.Brand.Id, context.UserId, context.Run.CampaignId,
            context.Stage.Id, jobType, prompt, result, startedAt);

    /// <summary>
    /// The stage-less overload — for an AI call that has no backing <see cref="AiPipelineStage"/> at
    /// all, such as strategy refinement, which is a versioned write but not a node in the graph.
    /// Kept as one place rather than a second copy of this construction, same as the stage-bound one.
    /// </summary>
    public static AiJob RecordText(
        IApplicationDbContext dbContext,
        Guid tenantId,
        Guid brandProfileId,
        Guid triggeredBy,
        Guid? campaignId,
        Guid? pipelineStageId,
        AiJobType jobType,
        string prompt,
        AiTextGenerationResult result,
        DateTime startedAt)
    {
        var now = DateTime.UtcNow;

        var job = new AiJob
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            BrandProfileId = brandProfileId,
            TriggeredBy = triggeredBy,
            PipelineStageId = pipelineStageId,
            JobType = jobType,
            Status = result.Succeeded ? AiJobStatus.Completed : AiJobStatus.Failed,

            // The prompt is stored once, here, and referenced by hash — content generation used to
            // also copy the whole batch prompt onto every ContentItem it produced (roughly eleven
            // copies of a multi-KB string per ten-post batch); that field was retired in C23 since
            // nothing ever read it back.
            InputParams = JsonSerializer.Serialize(new { prompt }),
            PromptHash = Hash(prompt),

            Provider = "Groq",
            Model = result.Model,
            LatencyMs = result.LatencyMs,
            Tokens = result.TokensUsed,
            ErrorMessage = result.ErrorMessage,
            OutputRefId = campaignId,
            OutputRefType = "marketing_campaign",
            StartedAt = startedAt,
            CompletedAt = now,
            CreatedAt = now
        };

        dbContext.AiJobs.Add(job);

        return job;
    }

    public static AiJob RecordSearch(
        IApplicationDbContext dbContext,
        StageContext context,
        string query,
        bool succeeded,
        string? errorMessage,
        DateTime startedAt)
    {
        var now = DateTime.UtcNow;

        var job = new AiJob
        {
            Id = Guid.NewGuid(),
            TenantId = context.TenantId,
            BrandProfileId = context.Brand.Id,
            TriggeredBy = context.UserId,
            PipelineStageId = context.Stage.Id,
            JobType = AiJobType.MarketAnalysis,
            Status = succeeded ? AiJobStatus.Completed : AiJobStatus.Failed,
            InputParams = JsonSerializer.Serialize(new { query }),
            PromptHash = Hash(query),
            Provider = "Tavily",
            ErrorMessage = errorMessage,
            OutputRefId = context.Run.CampaignId,
            OutputRefType = "marketing_campaign",
            StartedAt = startedAt,
            CompletedAt = now,
            CreatedAt = now
        };

        dbContext.AiJobs.Add(job);

        return job;
    }

    public static AiJob RecordImage(
        IApplicationDbContext dbContext,
        StageContext context,
        string prompt,
        AiImageGenerationResult result,
        DateTime startedAt,
        Guid? visualAssetId)
    {
        var now = DateTime.UtcNow;

        var job = new AiJob
        {
            Id = Guid.NewGuid(),
            TenantId = context.TenantId,
            BrandProfileId = context.Brand.Id,
            TriggeredBy = context.UserId,
            PipelineStageId = context.Stage.Id,
            JobType = AiJobType.ImageGeneration,
            Status = result.Succeeded ? AiJobStatus.Completed : AiJobStatus.Failed,
            InputParams = JsonSerializer.Serialize(new { prompt }),
            PromptHash = Hash(prompt),
            Provider = "HuggingFace",
            ErrorMessage = result.ErrorMessage,
            OutputRefId = visualAssetId,
            OutputRefType = "visual_asset",
            StartedAt = startedAt,
            CompletedAt = now,
            CreatedAt = now
        };

        dbContext.AiJobs.Add(job);

        return job;
    }

    private static string Hash(string value) =>
        Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(value)));
}
