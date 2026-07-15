namespace Rawaj.Application.Features.Competitors.ScrapeWebsite;

public record ScrapeWebsiteResponse(Guid CompetitorId, Guid RagDocumentId, string? Title, string TextContent);
