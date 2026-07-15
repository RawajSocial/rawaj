using Rawaj.Domain.Enums;

namespace Rawaj.Application.Features.SocialAccounts.HandleOAuthCallback;

public record HandleOAuthCallbackResponse(
    Guid SocialAccountId,
    SocialPlatform Platform,
    string AccountName,
    string RedirectUri);
