using Rawaj.Domain.Enums;

namespace Rawaj.Application.Features.SocialAccounts.ConnectSocialAccount;

public record ConnectSocialAccountResponse(
    Guid SocialAccountId,
    Guid BrandProfileId,
    SocialPlatform Platform,
    string AccountName);
