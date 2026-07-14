using MediatR;
using Rawaj.Application.Common.Models;

namespace Rawaj.Application.Features.Tenants.AcceptInvitation;

public record AcceptInvitationCommand(Guid InvitationId) : IRequest<Result<Unit>>;
