using MediatR;
using Rawaj.Application.Common.Interfaces;
using Rawaj.Application.Common.Models;
using Rawaj.Application.Features.SocialAccounts.Common;

namespace Rawaj.Application.Features.SocialAccounts.HandleOAuthCallback;

public class HandleOAuthCallbackCommandHandler(
    IApplicationDbContext dbContext,
    ITokenEncryptor tokenEncryptor,
    IOAuthStateStore oAuthStateStore,
    IEnumerable<ISocialOAuthProvider> oAuthProviders)
    : IRequestHandler<HandleOAuthCallbackCommand, Result<HandleOAuthCallbackResponse>>
{
    public async Task<Result<HandleOAuthCallbackResponse>> Handle(
        HandleOAuthCallbackCommand request, CancellationToken cancellationToken)
    {
        var context = oAuthStateStore.Consume(request.State);
        if (context is null)
        {
            return Result<HandleOAuthCallbackResponse>.Failure("This authorization request is invalid or has expired. Please try connecting again.");
        }

        var provider = oAuthProviders.FirstOrDefault(p => p.Platform == context.Platform);
        if (provider is null)
        {
            return Result<HandleOAuthCallbackResponse>.Failure($"OAuth is not configured for {context.Platform}.");
        }

        var tokenResult = await provider.ExchangeCodeAsync(request.Code, context.RedirectUri, cancellationToken);
        if (!tokenResult.Succeeded)
        {
            return Result<HandleOAuthCallbackResponse>.Failure(
                tokenResult.ErrorMessage ?? "Failed to exchange the authorization code for an access token.");
        }

        var profile = await provider.GetAccountProfileAsync(tokenResult.AccessToken!, cancellationToken);
        if (!profile.Succeeded)
        {
            return Result<HandleOAuthCallbackResponse>.Failure(
                profile.ErrorMessage ?? "Failed to retrieve the connected account's profile.");
        }

        var accessTokenToStore = profile.AccountAccessToken ?? tokenResult.AccessToken!;

        var connectResult = await SocialAccountConnector.ConnectAsync(
            dbContext,
            tokenEncryptor,
            context.TenantId,
            context.BrandProfileId,
            context.Platform,
            profile.AccountName!,
            profile.AccountIdExternal!,
            accessTokenToStore,
            tokenResult.RefreshToken,
            tokenResult.ExpiresAt,
            scopes: null,
            cancellationToken);

        if (!connectResult.Succeeded)
        {
            return Result<HandleOAuthCallbackResponse>.Failure(connectResult.ErrorMessage!);
        }

        var account = connectResult.Data!;

        return Result<HandleOAuthCallbackResponse>.Success(
            new HandleOAuthCallbackResponse(account.Id, account.Platform, account.AccountName, context.RedirectUri));
    }
}
