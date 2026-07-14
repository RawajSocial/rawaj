using MediatR;
using Rawaj.Application.Common.Models;

namespace Rawaj.Application.Features.Tenants.DeclineInvitation;

public record DeclineInvitationCommand(Guid InvitationId) : IRequest<Result<Unit>>;
