using MediatR;
using Rawaj.Application.Common.Interfaces;
using Rawaj.Application.Common.Models;
using Rawaj.Application.Features.SocialAccounts.Common;

namespace Rawaj.Application.Features.SocialAccounts.ConnectSocialAccount;

public class ConnectSocialAccountCommandHandler(
    IApplicationDbContext dbContext,
    ICurrentTenantContext currentTenantContext,
    ITokenEncryptor tokenEncryptor)
    : IRequestHandler<ConnectSocialAccountCommand, Result<ConnectSocialAccountResponse>>
{
    public async Task<Result<ConnectSocialAccountResponse>> Handle(
        ConnectSocialAccountCommand request, CancellationToken cancellationToken)
    {
        var tenantId = currentTenantContext.TenantId!.Value;

        var result = await SocialAccountConnector.ConnectAsync(
            dbContext,
            tokenEncryptor,
            tenantId,
            request.BrandProfileId,
            request.Platform,
            request.AccountName,
            request.AccountIdExternal,
            request.AccessToken,
            request.RefreshToken,
            request.TokenExpiresAt,
            request.Scopes,
            cancellationToken);

        if (!result.Succeeded)
        {
            return Result<ConnectSocialAccountResponse>.Failure(result.ErrorMessage!);
        }

        var account = result.Data!;
        return Result<ConnectSocialAccountResponse>.Success(
            new ConnectSocialAccountResponse(account.Id, account.BrandProfileId, account.Platform, account.AccountName));
    }
}
