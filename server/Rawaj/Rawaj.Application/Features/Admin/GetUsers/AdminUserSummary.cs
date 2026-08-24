namespace Rawaj.Application.Features.Admin.GetUsers;

public record AdminUserSummary(Guid UserId, string Email, string FullName, bool IsActive, bool IsPlatformAdmin);
