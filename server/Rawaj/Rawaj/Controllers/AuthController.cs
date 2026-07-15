using MediatR;
using Microsoft.AspNetCore.Mvc;
using Rawaj.Application.Features.Auth.Login;
using Rawaj.Application.Features.Auth.Logout;
using Rawaj.Application.Features.Auth.RefreshToken;
using Rawaj.Application.Features.Auth.Register;
using Rawaj.Common;

namespace Rawaj.Controllers;

[ApiController]
[Route("api/v1/auth")]
public class AuthController(ISender sender) : ControllerBase
{
    [HttpPost("register")]
    public async Task<IActionResult> Register(RegisterCommand command, CancellationToken cancellationToken)
    {
        var result = await sender.Send(command, cancellationToken);

        return result.Succeeded
            ? Ok(ApiResponse<RegisterResponse>.Success(result.Data!))
            : BadRequest(ApiResponse<RegisterResponse>.Fail(result.ErrorMessage!));
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login(LoginCommand command, CancellationToken cancellationToken)
    {
        var result = await sender.Send(command, cancellationToken);

        return result.Succeeded
            ? Ok(ApiResponse<LoginResponse>.Success(result.Data!))
            : Unauthorized(ApiResponse<LoginResponse>.Fail(result.ErrorMessage!));
    }

    [HttpPost("refresh-token")]
    public async Task<IActionResult> RefreshToken(RefreshTokenRequest request, CancellationToken cancellationToken)
    {
        var result = await sender.Send(new RefreshTokenCommand(request.RefreshToken), cancellationToken);

        return result.Succeeded
            ? Ok(ApiResponse<RefreshTokenResponse>.Success(result.Data!))
            : Unauthorized(ApiResponse<RefreshTokenResponse>.Fail(result.ErrorMessage!));
    }

    [HttpPost("logout")]
    public async Task<IActionResult> Logout(RefreshTokenRequest request, CancellationToken cancellationToken)
    {
        var result = await sender.Send(new LogoutCommand(request.RefreshToken), cancellationToken);

        return result.Succeeded
            ? Ok(ApiResponse<bool>.Success(result.Data))
            : BadRequest(ApiResponse<bool>.Fail(result.ErrorMessage!));
    }

    public record RefreshTokenRequest(string RefreshToken);
}
