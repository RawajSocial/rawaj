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
            JobType = jobType,
            Status = result.Succeeded ? AiJobStatus.Completed : AiJobStatus.Failed,

            // The prompt is stored once, here, and referenced by hash. Content generation currently
            // copies the whole batch prompt onto every ContentItem it produces as well as into this
            // row — roughly eleven copies of a multi-KB string per ten-post batch.
            InputParams = JsonSerializer.Serialize(new { prompt }),
            PromptHash = Hash(prompt),

            Provider = "Groq",
            Model = result.Model,
            LatencyMs = result.LatencyMs,
            Tokens = result.TokensUsed,
            ErrorMessage = result.ErrorMessage,
            OutputRefId = context.Run.CampaignId,
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

    private static string Hash(string value) =>
        Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(value)));
}
