using Rawaj.Domain.Enums;

namespace Rawaj.Application.Features.Users.UpdateMyProfile;

public record UpdateMyProfileResponse(string FullName, string? AvatarUrl, Language PreferredLanguage);
