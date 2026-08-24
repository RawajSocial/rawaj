namespace Rawaj.Application.Features.Competitors.GetCompetitorAnalysis;

public record RagDocumentSummary(
    Guid RagDocumentId,
    string? SourceUrl,
    string? CompetitorsData,
    DateTime? IndexedAt);
