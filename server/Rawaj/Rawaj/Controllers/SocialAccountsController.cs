using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Rawaj.Application.Features.SocialAccounts.ConnectSocialAccount;
using Rawaj.Application.Features.SocialAccounts.DisconnectSocialAccount;
using Rawaj.Application.Features.SocialAccounts.GetAuthorizationUrl;
using Rawaj.Application.Features.SocialAccounts.GetSocialAccounts;
using Rawaj.Application.Features.SocialAccounts.HandleOAuthCallback;
using Rawaj.Common;
using Rawaj.Domain.Enums;

namespace Rawaj.Controllers;

[ApiController]
[Authorize]
[Route("api/v1/social-accounts")]
public class SocialAccountsController(ISender sender, IConfiguration configuration) : ControllerBase
{
    [HttpPost]
    public async Task<IActionResult> Connect(ConnectSocialAccountCommand command, CancellationToken cancellationToken)
    {
        var result = await sender.Send(command, cancellationToken);

        return result.Succeeded
            ? Ok(ApiResponse<ConnectSocialAccountResponse>.Success(result.Data!))
            : BadRequest(ApiResponse<ConnectSocialAccountResponse>.Fail(result.ErrorMessage!));
    }

    [HttpGet("brand/{brandProfileId:guid}")]
    public async Task<IActionResult> GetByBrand(Guid brandProfileId, CancellationToken cancellationToken)
    {
        var result = await sender.Send(new GetSocialAccountsQuery(brandProfileId), cancellationToken);

        return result.Succeeded
            ? Ok(ApiResponse<List<SocialAccountSummary>>.Success(result.Data!))
            : BadRequest(ApiResponse<List<SocialAccountSummary>>.Fail(result.ErrorMessage!));
    }

    [HttpPost("{socialAccountId:guid}/disconnect")]
    public async Task<IActionResult> Disconnect(Guid socialAccountId, CancellationToken cancellationToken)
    {
        var result = await sender.Send(new DisconnectSocialAccountCommand(socialAccountId), cancellationToken);

        return result.Succeeded
            ? Ok(ApiResponse<DisconnectSocialAccountResponse>.Success(result.Data!))
            : BadRequest(ApiResponse<DisconnectSocialAccountResponse>.Fail(result.ErrorMessage!));
    }

    /// <summary>
    /// Called by the frontend (authenticated) to get the URL it should redirect the browser to
    /// in order to start the platform's OAuth consent flow.
    /// </summary>
    [HttpPost("{platform}/authorization-url")]
    public async Task<IActionResult> GetAuthorizationUrl(
        string platform, [FromQuery] Guid brandProfileId, CancellationToken cancellationToken)
    {
        if (!Enum.TryParse<SocialPlatform>(platform, true, out var parsedPlatform))
        {
            return BadRequest(ApiResponse<GetAuthorizationUrlResponse>.Fail($"'{platform}' is not a supported platform."));
        }

        var redirectUri = Url.Action(nameof(Callback), "SocialAccounts", new { platform }, Request.Scheme)!;

        var result = await sender.Send(
            new GetAuthorizationUrlCommand(brandProfileId, parsedPlatform, redirectUri), cancellationToken);

        return result.Succeeded
            ? Ok(ApiResponse<GetAuthorizationUrlResponse>.Success(result.Data!))
            : BadRequest(ApiResponse<GetAuthorizationUrlResponse>.Fail(result.ErrorMessage!));
    }

    /// <summary>
    /// The redirect target registered with the platform's OAuth app. The browser lands here
    /// directly from Meta/LinkedIn's consent screen, so there is no bearer token on this
    /// request — it is secured by the one-time "state" value instead. Not gated by [Authorize].
    /// Since the browser is on this URL directly (not an API caller), the result is a redirect
    /// back into the frontend app rather than a JSON body.
    /// </summary>
    [HttpGet("{platform}/callback")]
    [AllowAnonymous]
    public async Task<IActionResult> Callback(
        string platform, [FromQuery] string code, [FromQuery] string state, CancellationToken cancellationToken)
    {
        var result = await sender.Send(new HandleOAuthCallbackCommand(code, state), cancellationToken);

        var frontendBaseUrl = (configuration["Frontend:BaseUrl"] ?? "http://localhost:4200").TrimEnd('/');
        var destination = result.Succeeded
            ? $"{frontendBaseUrl}/dashboard/social-accounts?connected={Uri.EscapeDataString(result.Data!.Platform.ToString())}"
            : $"{frontendBaseUrl}/dashboard/social-accounts?error={Uri.EscapeDataString(result.ErrorMessage!)}";

        return Redirect(destination);
    }
}
