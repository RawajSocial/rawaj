using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace Rawaj.Infrastructure.RealTime;

/// <summary>
/// One group per user ("user:{userId}"), joined on connect — mirrors Notification's per-user (not
/// per-tenant) semantics. Auth is the same JWT bearer scheme as the REST API; SignalR can't attach
/// an Authorization header on the browser's WebSocket handshake, so the token travels via the
/// "access_token" query string instead (wired in JwtServiceExtensions' OnMessageReceived, scoped to
/// paths under /hubs so REST endpoints are unaffected).
/// </summary>
[Authorize]
public class NotificationsHub : Hub
{
    public override async Task OnConnectedAsync()
    {
        var userId = Context.User?.FindFirst(c => c.Type is "sub" or ClaimTypes.NameIdentifier)?.Value;
        if (!string.IsNullOrWhiteSpace(userId))
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, GroupName(userId));
        }

        await base.OnConnectedAsync();
    }

    public static string GroupName(string userId) => $"user:{userId}";
}
