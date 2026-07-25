using Microsoft.AspNetCore.SignalR;
using Rawaj.Application.Common.Interfaces;

namespace Rawaj.Infrastructure.RealTime;

public class SignalRNotificationBroadcaster(IHubContext<NotificationsHub> hubContext) : INotificationBroadcaster
{
    public Task BroadcastAsync(Guid userId, string eventName, object payload, CancellationToken cancellationToken) =>
        hubContext.Clients.Group(NotificationsHub.GroupName(userId.ToString())).SendAsync(eventName, payload, cancellationToken);
}
