using MediatR;
using Rawaj.Application.Common.Models;

namespace Rawaj.Application.Features.Notifications.MarkAllNotificationsRead;

public record MarkAllNotificationsReadCommand : IRequest<Result<int>>;
