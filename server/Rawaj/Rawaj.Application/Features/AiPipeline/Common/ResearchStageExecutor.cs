using System.Text.Json;
using Rawaj.Application.Common.Interfaces;
using Rawaj.Application.Common.Models;
using Rawaj.Application.Common.Policies;
using Rawaj.Application.Common.Services;
using Rawaj.Application.Features.AiPipeline.Prompts;
using Rawaj.Application.Features.Content.Common;
using Rawaj.Domain.Entities.Tenants;
using Rawaj.Domain.Enums;

namespace Rawaj.Application.Features.AiPipeline.Common;

/// <summary>
/// Base for the two research stages: run the searches the campaign analysis asked for, then have the
/// model synthesise the results into an artifact.
///
/// <para><b>Best-effort by design.</b> Research "may help and may not help", and the existing product
/// rule is that a search failure must never block a campaign. Here that is explicit rather than
/// conventional: these stages are <c>IsOptional</c> in the graph, an empty or failed search still
/// completes with <c>unavailable: true</c>, and nothing is charged — no research value was
/// delivered, so the tenant does not pay for it.</para>
///
/// <para><b>The containment boundary.</b> These stages are the only ones that ever see raw web text.
/// What they emit is a synthesised artifact, and every stage downstream reads that instead — so a
/// hostile page cannot reach the strategy or the content prompts even if it defeats the sanitizer.</para>
/// </summary>
public abstract class ResearchStageExecutor(
    IApplicationDbContext dbContext,
    ITavilySearchService searchService,
    IAiTextGenerationService textGenerationService,
    IPromptTemplateProvider templates) : IPipelineStageExecutor
{
    /// <summary>Enough for a picture of the market without letting one verbose page dominate the
    /// synthesis, and a hard bound on what a hostile page can spend of our token budget.</summary>
    private const int MaxExcerptsPerQuery = 5;
    private const int MaxTotalExcerpts = 12;
    private const int SnippetMaxLength = 400;

    public abstract AiPipelineStageKind Kind { get; }

    protected abstract AiArtifactKind ArtifactKind { get; }

    /// <summary>Which half of <c>researchQueries</c> this stage runs.</summary>
    protected abstract string QuerySelector { get; }

    protected abstract string BuildSynthesisPrompt(
        TenantBrandProfile brand, IReadOnlyList<string> queries, IReadOnlyList<string> excerpts, string? searchAnswer);

    /// <summary>Fallback when the campaign analysis produced no usable queries — mirrors how the
    /// current handler builds its single query, so a degraded run still searches for something
    /// sensible rather than nothing at all.</summary>
    protected abstract string BuildFallbackQuery(TenantBrandProfile brand);

    /// <summary>Hook for a stage that wants to keep the raw results as well; only competitor research
    /// does, preserving the RagDocument rows brand-level readers already depend on.</summary>
    protected virtual void OnResultsGathered(StageContext context, IReadOnlyList<TavilySearchItem> items)
    {
    }

    public async Task<StageResult> ExecuteAsync(StageContext context, CancellationToken cancellationToken)
    {
        var queries = ResolveQueries(context);
        var jobIds = new List<Guid>();
        var items = new List<TavilySearchItem>();
        string? searchAnswer = null;
        string? lastError = null;

        // Fired concurrently rather than one at a time: these queries are independent of each other,
        // and AiProviderConcurrencyLimiter already allows up to 4 simultaneous Tavily calls (matching
        // the query cap below), so a sequential await loop here was pure added latency for no benefit.
        // Task.WhenAll preserves the input order in its result array, so job recording and the
        // searchAnswer/items assembly below stay deterministic by query order, not by which call
        // happens to land first over the network.
        var searchTasks = queries
            .Select(async query =>
            {
                var startedAt = DateTime.UtcNow;
                var search = await searchService.SearchAsync(query, cancellationToken);
                return (query, search, startedAt);
            })
            .ToList();

        var searchResults = await Task.WhenAll(searchTasks);

        foreach (var (query, search, startedAt) in searchResults)
        {
            var job = AiJobRecorder.RecordSearch(
                dbContext, context, query, search.Succeeded, search.ErrorMessage, startedAt);
            jobIds.Add(job.Id);

            if (!search.Succeeded)
            {
                lastError = search.ErrorMessage;
                continue;
            }

            searchAnswer ??= search.Answer;
            items.AddRange(search.Results.Take(MaxExcerptsPerQuery));
        }

        // Deduplicate by URL: overlapping queries routinely return the same page, and paying a model
        // to read it three times makes the synthesis worse as well as more expensive.
        var distinctItems = items
            .GroupBy(i => i.Url)
            .Select(g => g.First())
            .Take(MaxTotalExcerpts)
            .ToList();

        if (distinctItems.Count == 0)
        {
            // Not a failure. The run continues, and the strategy stages see an artifact that honestly
            // says nothing was found instead of silently missing an input.
            return StageResult.SucceededWithoutValue(
                ArtifactKind, UnavailableArtifact(lastError), jobIds);
        }

        OnResultsGathered(context, distinctItems);

        var excerpts = distinctItems
            .Select(i => $"{i.Title} ({i.Url}): {Truncate(i.Content, SnippetMaxLength)}")
            .ToList();

        var prompt = BuildSynthesisPrompt(context.Brand, queries, excerpts, searchAnswer);

        if (context.RepairPrompt)
        {
            prompt += " " + PromptFragments.RepairInstruction;
        }

        var template = templates.For(Kind);
        var generationStartedAt = DateTime.UtcNow;

        var generation = await textGenerationService.GenerateTextAsync(
            prompt,
            cancellationToken,
            new AiTextGenerationOptions(template.TaskName, template.JsonMode, template.Temperature));

        var synthesisJob = AiJobRecorder.RecordText(
            dbContext, context, AiJobType.MarketAnalysis, prompt, generation, generationStartedAt);
        jobIds.Add(synthesisJob.Id);

        if (!generation.Succeeded)
        {
            return StageResult.Failure(
                AiPipelinePolicy.ClassifyProviderError(generation.ErrorMessage),
                generation.ErrorMessage ?? "Research synthesis failed.",
                jobIds);
        }

        var payload = AiJsonResponseParser.ExtractJsonPayload(generation.Text);

        if (!ArtifactSchema.IsValid(ArtifactKind, payload, out var validationError))
        {
            return StageResult.Failure(
                payload is null ? AiFailureKind.Parse : AiFailureKind.Validation,
                validationError ?? "The research response could not be read.",
                jobIds);
        }

        // Real research was delivered, so this is the stage that gets charged (competitor research
        // only — market research is folded into the same charge point and stays free).
        return StageResult.Success(ArtifactKind, payload!, jobIds);
    }

    /// <summary>
    /// The queries the campaign analysis proposed, falling back to a constructed one. Reading them
    /// from the analysis is the improvement: the current handler concatenates brand name, industry
    /// and keyword fields, so research quality tracks how carefully a form was filled in.
    /// </summary>
    private List<string> ResolveQueries(StageContext context)
    {
        var analysisJson = context.Input(AiArtifactKind.CampaignAnalysis);

        if (!string.IsNullOrWhiteSpace(analysisJson))
        {
            try
            {
                using var document = JsonDocument.Parse(analysisJson);

                if (document.RootElement.TryGetProperty("researchQueries", out var queriesElement) &&
                    queriesElement.TryGetProperty(QuerySelector, out var selected) &&
                    selected.ValueKind == JsonValueKind.Array)
                {
                    var queries = selected.EnumerateArray()
                        .Select(q => q.GetString())
                        .Where(q => !string.IsNullOrWhiteSpace(q))
                        .Select(q => q!.Trim())
                        .Distinct()
                        .Take(4)
                        .ToList();

                    if (queries.Count > 0)
                    {
                        return queries;
                    }
                }
            }
            catch (JsonException)
            {
                // A malformed analysis artifact should degrade to the fallback query rather than
                // failing a stage that is meant to be best-effort.
            }
        }

        return [BuildFallbackQuery(context.Brand)];
    }

    private static string UnavailableArtifact(string? note) =>
        JsonSerializer.Serialize(new
        {
            trends = Array.Empty<string>(),
            demandSignals = Array.Empty<string>(),
            competitors = Array.Empty<object>(),
            contentPatterns = Array.Empty<string>(),
            gaps = Array.Empty<string>(),
            sources = Array.Empty<object>(),
            unavailable = true,
            note = note ?? "No research data was found for this brand."
        });

    private static string Truncate(string text, int maxLength) =>
        text.Length <= maxLength ? text : text[..maxLength] + "…";
}
