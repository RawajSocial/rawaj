using MediatR;
using Rawaj.Application.Common.Models;

namespace Rawaj.Application.Features.SocialAccounts.HandleOAuthCallback;

/// <summary>
/// Handles a provider's OAuth redirect. Unlike other commands, this one is not gated by
/// IRequireTenantRole/JWT — the browser hitting this endpoint has no bearer token. Security
/// instead comes from the one-time, server-issued "state" value, which was created during an
/// authenticated GetAuthorizationUrl call and carries the original user/tenant/brand context.
/// </summary>
public record HandleOAuthCallbackCommand(string Code, string State)
    : IRequest<Result<HandleOAuthCallbackResponse>>;
