using Rawaj.Application.Common.Interfaces;
using Rawaj.Application.Common.Models;
using Rawaj.Application.Features.AiPipeline.Common;
using Rawaj.Application.Features.AiPipeline.Prompts;
using Rawaj.Application.Features.Content.Common;
using Rawaj.Domain.Entities.AiOperations;
using Rawaj.Domain.Entities.Campaigns;
using Rawaj.Domain.Entities.Tenants;
using Rawaj.Domain.Enums;

namespace Rawaj.Application.Features.AiPipeline.Services;

/// <summary>
/// Refines the current strategy artifact by free-text feedback — the pipeline-native replacement for
/// <c>RefineCampaignPlanCommandHandler</c>'s overwrite-in-place.
///
/// <para>Writes a new artifact version rather than mutating the existing one, via the same
/// <see cref="IPipelineArtifactStore"/> every stage uses. That is the entire point of moving this off
/// the old <c>AiPlanJson</c> column: a refined-then-regretted strategy keeps its earlier version on
/// record, and <c>MarketingCampaign.ApprovedStrategyArtifactId</c> can never be silently invalidated
/// by a refinement that lands after it.</para>
///
/// <para>Refuses once the strategy is approved — the same rule the legacy handler enforces. An
/// approved version is what content generation was gated on and what the user actually reviewed;
/// refining it after the fact would let the artifact diverge from what was signed off without anyone
/// re-approving.</para>
///
/// <para>Not a stage: the graph has no node for "user typed feedback into a box", so the provider
/// call is logged through <see cref="AiJobRecorder"/>'s stage-less overload and the new version is
/// stored with no <c>SourceStageId</c>. Coin charging is left to the caller, matching how every other
/// entry point in this feature charges at the command-handling layer rather than inside the piece
/// that talks to the provider.</para>
/// </summary>
public class PipelineStrategyRefinementService(
    IApplicationDbContext dbContext,
    IAiTextGenerationService textGenerationService,
    IPipelineArtifactStore artifacts)
    : IPipelineStrategyRefinementService
{
    public async Task<Result<AiArtifact>> RefineAsync(
        TenantBrandProfile brand,
        MarketingCampaign campaign,
        Guid userId,
        string feedback,
        CancellationToken cancellationToken)
    {
        if (campaign.PlanApprovedAt is not null)
        {
            return Result<AiArtifact>.Failure(
                "This campaign's strategy is already approved and can no longer be refined.");
        }

        var current = await artifacts.GetCurrentAsync(
            brand.Id, campaign.Id, AiArtifactKind.Strategy, cancellationToken);

        if (current is null)
        {
            return Result<AiArtifact>.Failure("Generate a strategy before refining it.");
        }

        // Normalized for the same reason the artifact store does it on read: a strategy stored with
        // escaped Arabic costs ~4.7x the tokens here, and this prompt embeds the whole thing.
        var prompt = StrategyRefinementPrompt.Build(
            brand, campaign, AiJsonResponseParser.Normalize(current.ContentJson) ?? current.ContentJson, feedback);
        var startedAt = DateTime.UtcNow;

        var generation = await textGenerationService.GenerateTextAsync(prompt, cancellationToken);

        AiJobRecorder.RecordText(
            dbContext, brand.TenantId, brand.Id, userId, campaign.Id, pipelineStageId: null,
            AiJobType.PlanGeneration, prompt, generation, startedAt);

        if (!generation.Succeeded)
        {
            return Result<AiArtifact>.Failure(
                generation.ErrorMessage ?? "Strategy refinement failed. Please try again.");
        }

        // Especially important here: this is about to replace an existing, already-paid-for
        // strategy, so an unreadable refinement must leave the previous version current rather than
        // being stored over it.
        var refinedJson = AiJsonResponseParser.ExtractJsonPayload(generation.Text);

        if (!ArtifactSchema.IsValid(AiArtifactKind.Strategy, refinedJson, out var validationError))
        {
            return Result<AiArtifact>.Failure(
                validationError ??
                "The refined strategy could not be read. Your existing strategy is unchanged — please try again.");
        }

        var artifact = await artifacts.AddVersionAsync(
            stage: null, brand.TenantId, brand.Id, campaign.Id, AiArtifactKind.Strategy,
            refinedJson!, inputHash: null, cancellationToken);

        // Phase 6 write-through — same reason StrategyAssemble's completion projects into
        // AiPlanJson: the legacy strategy review UI reads the column, not the artifact.
        campaign.AiPlanJson = refinedJson;
        campaign.AiGeneratedAt = DateTime.UtcNow;
        campaign.UpdatedAt = DateTime.UtcNow;

        return Result<AiArtifact>.Success(artifact);
    }
}
