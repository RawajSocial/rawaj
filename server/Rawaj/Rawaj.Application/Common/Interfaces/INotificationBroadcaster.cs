namespace Rawaj.Application.Common.Interfaces;

/// <summary>
/// Real-time delivery boundary — the Application/Infrastructure boundary here mirrors
/// IMediaStorageService: OutboxDispatcherHostedService (Infrastructure) depends on this interface,
/// never on SignalR directly, so swapping the transport later needs no change outside its one
/// implementation.
/// </summary>
public interface INotificationBroadcaster
{
    Task BroadcastAsync(Guid userId, string eventName, object payload, CancellationToken cancellationToken);
}
