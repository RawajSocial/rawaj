namespace Rawaj.Application.Features.Content.GetContentRevisions;

public record ContentRevisionSummary(
    Guid Id,
    int RevisionNumber,
    string RevisionPrompt,
    string Previous,
    string Current,
    Guid RevisedBy,
    DateTime CreatedAt);
