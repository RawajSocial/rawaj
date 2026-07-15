using MediatR;
using Rawaj.Application.Common.Models;

namespace Rawaj.Application.Features.Notifications.MarkNotificationRead;

public record MarkNotificationReadCommand(Guid NotificationId) : IRequest<Result<bool>>;
