namespace Rawaj.Application.Features.Tenants.InviteMember;

public record InviteTenantMemberResponse(Guid InvitationId, bool RequiresRegistration);
