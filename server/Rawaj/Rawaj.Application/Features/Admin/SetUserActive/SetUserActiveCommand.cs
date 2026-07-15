using MediatR;
using Rawaj.Application.Common.Models;

namespace Rawaj.Application.Features.Admin.SetUserActive;

public record SetUserActiveCommand(Guid UserId, bool IsActive) : IRequest<Result<bool>>;
