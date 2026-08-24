namespace Rawaj.Application.Features.Competitors.AnalyzeCompetitor;

public record AnalyzeCompetitorSource(string Title, string Url);

public record AnalyzeCompetitorResponse(
    Guid CompetitorId,
    string? Summary,
    List<AnalyzeCompetitorSource> Sources);
