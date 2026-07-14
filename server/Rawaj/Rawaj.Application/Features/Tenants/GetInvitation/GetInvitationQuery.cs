using MediatR;
using Rawaj.Application.Common.Models;

namespace Rawaj.Application.Features.Tenants.GetInvitation;

public record GetInvitationQuery(Guid InvitationId) : IRequest<Result<InvitationDetailsDto>>;
