namespace Rawaj.Application.Features.Auth.Login;

public record LoginResponse(Guid UserId, string Email, string UserName, string FullName, string AccessToken);
