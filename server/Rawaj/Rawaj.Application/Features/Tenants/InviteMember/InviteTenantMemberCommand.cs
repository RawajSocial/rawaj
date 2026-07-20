using MediatR;
using Rawaj.Application.Common.Models;
using Rawaj.Domain.Enums;

namespace Rawaj.Application.Features.Tenants.InviteMember;

public record InviteTenantMemberCommand(Guid TenantId, string Email, TenantMemberRole Role) : IRequest<Result<InviteTenantMemberResponse>>;
