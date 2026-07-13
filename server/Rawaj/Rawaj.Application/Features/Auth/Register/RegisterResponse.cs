namespace Rawaj.Application.Features.Auth.Register;

public record RegisterResponse(Guid UserId, string Email, string UserName, string FullName, string AccessToken);
