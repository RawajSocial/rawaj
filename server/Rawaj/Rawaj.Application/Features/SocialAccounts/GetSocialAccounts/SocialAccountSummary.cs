using Rawaj.Domain.Enums;

namespace Rawaj.Application.Features.SocialAccounts.GetSocialAccounts;

public record SocialAccountSummary(
    Guid SocialAccountId,
    SocialPlatform Platform,
    string AccountName,
    bool IsActive,
    DateTime? TokenExpiresAt,
    DateTime? LastVerifiedAt);
