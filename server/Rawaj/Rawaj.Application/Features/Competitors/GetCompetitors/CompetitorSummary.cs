using Rawaj.Domain.Enums;

namespace Rawaj.Application.Features.Competitors.GetCompetitors;

public record CompetitorSummary(
    Guid CompetitorId,
    string Name,
    string? Url,
    bool Ragged,
    CompetitorStatus Status,
    DateTime? LastScrapedAt);
