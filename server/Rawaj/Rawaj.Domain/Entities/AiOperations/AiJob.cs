using Rawaj.Domain.Common;
using Rawaj.Domain.Entities.Tenants;
using Rawaj.Domain.Enums;

namespace Rawaj.Domain.Entities.AiOperations;

public class AiJob : BaseEntity
{
    /// <summary>
    /// Who this call was made for. Denormalised deliberately: tenant attribution used to run through
    /// <see cref="BrandProfileId"/>, which is nullable — so every trial generation was invisible to
    /// any per-tenant query (including AiCreditsPolicy's usage count, which inner-joins brand
    /// profiles and therefore silently skips them), and the rest needed a join to attribute at all.
    ///
    /// Not a foreign key. This is an append-only log whose historical rows include some whose tenant
    /// can only be inferred, and a write on this table must never fail on a referential edge case
    /// while the real work has already been paid for at the provider.
    /// </summary>
    public Guid TenantId { get; set; }

    public Guid? BrandProfileId { get; set; }
    public Guid TriggeredBy { get; set; }

    /// <summary>The pipeline stage this call belongs to, when it was made by one. Null for the
    /// standalone content, visual-asset and trial generators, which don't run through the pipeline.
    /// Several jobs can share a stage — a research stage searches and then synthesises, and a
    /// parse-repair re-ask is a second call.</summary>
    public Guid? PipelineStageId { get; set; }

    public AiJobType JobType { get; set; }
    public AiJobStatus Status { get; set; }
    public string? InputParams { get; set; }
    public Guid? OutputRefId { get; set; }
    public string? OutputRefType { get; set; }

    /// <summary>Which service was called ("Groq", "Cloudflare", "HuggingFace", "Tavily").</summary>
    public string? Provider { get; set; }

    /// <summary>The specific model or endpoint used. Everything runs on one text model today; this
    /// column is what makes "which model produced this" answerable once that stops being true.</summary>
    public string? Model { get; set; }

    /// <summary>Wall-clock duration of the provider call. Together with Provider and Model it turns
    /// <see cref="Cost"/> from a column nothing populates into something computable from a rate
    /// table — today we cannot answer what a campaign actually costs us to produce.</summary>
    public int? LatencyMs { get; set; }

    /// <summary>Hash of the prompt in <see cref="InputParams"/>, for deduplication and for finding
    /// every call that used a given prompt without scanning multi-KB text columns.</summary>
    public string? PromptHash { get; set; }

    public int? Tokens { get; set; }
    public decimal? Cost { get; set; }
    public string? ErrorMessage { get; set; }
    public DateTime? StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public DateTime CreatedAt { get; set; }

    public TenantBrandProfile? BrandProfile { get; set; }
    public AiPipelineStage? PipelineStage { get; set; }
}
