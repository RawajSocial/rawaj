using Rawaj.Domain.Common;

namespace Rawaj.Domain.Entities.Platform;

/// <summary>
/// A reliable-delivery record written in the SAME SaveChangesAsync call as the business data it
/// describes (e.g. a Notification row) — so both commit atomically, or neither does. A background
/// dispatcher (OutboxDispatcherHostedService) polls for unprocessed rows and delivers them (today:
/// SignalR broadcast; later: email/push/webhook), retrying on failure instead of silently losing a
/// delivery if the process crashes between the commit and an in-process handler running.
/// </summary>
public class OutboxMessage : BaseEntity
{
    /// <summary>What kind of delivery this is — e.g. "NotificationCreated". Lets the dispatcher (and
    /// any future delivery channel) branch on payload shape without a separate table per type.</summary>
    public string Type { get; set; } = null!;
    public string PayloadJson { get; set; } = null!;
    public DateTime CreatedAt { get; set; }
    public DateTime? ProcessedAt { get; set; }
    public int Attempts { get; set; }
    public string? LastError { get; set; }
}
