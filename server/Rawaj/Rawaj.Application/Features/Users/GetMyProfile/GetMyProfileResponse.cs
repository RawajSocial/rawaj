using Rawaj.Domain.Enums;

namespace Rawaj.Application.Features.Users.GetMyProfile;

public record GetMyProfileResponse(Guid UserId, string Email, string Username, string FullName, string? AvatarUrl, Language PreferredLanguage);
