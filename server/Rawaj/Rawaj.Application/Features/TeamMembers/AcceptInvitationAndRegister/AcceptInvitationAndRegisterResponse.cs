namespace Rawaj.Application.Features.TeamMembers.AcceptInvitationAndRegister;

public record AcceptInvitationAndRegisterResponse(
    Guid UserId, string Email, string FullName, string AccessToken, string RefreshToken);
