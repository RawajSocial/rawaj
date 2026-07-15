namespace Rawaj.Application.Features.Auth.Login;

public record LoginResponse(Guid UserId, string Email, string FullName, string AccessToken, string RefreshToken);
