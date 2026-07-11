using MediatR;
using Microsoft.AspNetCore.Mvc;
using Rawaj.Application.Features.Auth.Login;
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
}
