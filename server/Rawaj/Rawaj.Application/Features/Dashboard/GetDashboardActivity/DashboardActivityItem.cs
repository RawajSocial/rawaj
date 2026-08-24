namespace Rawaj.Application.Features.Dashboard.GetDashboardActivity;

public record DashboardActivityItem(
    Guid Id,
    string Action,
    string? Message,
    string? EntityType,
    Guid? EntityId,
    DateTime CreatedAt);
