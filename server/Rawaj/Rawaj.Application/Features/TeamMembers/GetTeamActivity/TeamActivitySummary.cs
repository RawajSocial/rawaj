namespace Rawaj.Application.Features.TeamMembers.GetTeamActivity;

public record TeamActivitySummary(
    Guid Id, string Action, string? Message, Guid? UserId, string? UserFullName, DateTime CreatedAt);

public record TeamActivityPageResponse(List<TeamActivitySummary> Items, int TotalCount);
