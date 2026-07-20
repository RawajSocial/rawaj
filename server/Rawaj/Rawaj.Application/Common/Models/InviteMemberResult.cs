namespace Rawaj.Application.Common.Models;

public enum InviteMemberOutcome
{
    Invited,
    AlreadyMember,
    AlreadyInvited
}

public record InviteMemberResult(InviteMemberOutcome Outcome, Guid InvitationId, bool RequiresRegistration);
