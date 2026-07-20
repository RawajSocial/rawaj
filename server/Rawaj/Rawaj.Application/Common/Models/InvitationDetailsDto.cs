using Rawaj.Domain.Enums;

namespace Rawaj.Application.Common.Models;

public record InvitationDetailsDto(
    Guid InvitationId,
    Guid TenantId,
    string TenantName,
    TenantMemberRole Role,
    string Email,
    bool RequiresRegistration,
    InvitationStatus Status,
    bool IsExpired);
