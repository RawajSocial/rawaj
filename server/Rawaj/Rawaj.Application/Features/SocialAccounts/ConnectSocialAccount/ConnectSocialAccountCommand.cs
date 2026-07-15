using MediatR;
using Rawaj.Application.Common.Interfaces;
using Rawaj.Application.Common.Models;
using Rawaj.Domain.Enums;

namespace Rawaj.Application.Features.SocialAccounts.ConnectSocialAccount;

public record ConnectSocialAccountCommand(
    Guid BrandProfileId,
    SocialPlatform Platform,
    string AccountName,
    string AccountIdExternal,
    string AccessToken,
    string? RefreshToken,
    DateTime? TokenExpiresAt,
    List<string>? Scopes) : IRequest<Result<ConnectSocialAccountResponse>>, IRequireTenantRole
{
    public TenantMemberRole MinimumRole => TenantMemberRole.Admin;
}
