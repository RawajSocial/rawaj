namespace Rawaj.Domain.Enums;

public enum Language
{
    En,
    Ar
}

public enum TenantType
{
    Business,
    Agency,
    Freelancer
}

public enum BrandVoice
{
    Professional,
    Friendly,
    Bold,
    Playful,
    Elegant,
    Inspiring,
    Educational,
    Innovative,
    Motivating,
    WittyFunny
}

public enum BrandProfileStatus
{
    Active,
    Archived,
    Draft
}

public enum TenantMemberRole
{
    Owner,
    Admin,
    Editor,
    Viewer
}

public enum InvitationStatus
{
    Pending,
    Accepted,
    /// <summary>The invitee said no. Only ever set by DeclineInviteCommandHandler (the existing-user
    /// TenantMember path) — see Revoked for the admin-cancelled equivalent.</summary>
    Declined,
    /// <summary>The inviting admin cancelled it before it was accepted/declined — distinct from
    /// Declined so the audit trail/UI can tell "the invitee said no" apart from "we cancelled it".</summary>
    Revoked,
    /// <summary>Never accepted before ExpiresAt — flipped lazily on read (see
    /// GetPendingInvitationsQueryHandler) rather than by a background sweep, since nothing needs to
    /// act on the transition itself, only reflect it when someone looks.</summary>
    Expired
}
