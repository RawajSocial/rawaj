using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Rawaj.Application.Features.Users.ChangeMyPassword;
using Rawaj.Application.Features.Users.GetMyProfile;
using Rawaj.Application.Features.Users.UpdateMyProfile;
using Rawaj.Common;

namespace Rawaj.Controllers;

[ApiController]
[Authorize]
[Route("api/v1/users")]
public class UsersController(ISender sender) : ControllerBase
{
    [HttpGet("me")]
    public async Task<IActionResult> GetMe(CancellationToken cancellationToken)
    {
        var result = await sender.Send(new GetMyProfileQuery(), cancellationToken);

        return result.Succeeded
            ? Ok(ApiResponse<GetMyProfileResponse>.Success(result.Data!))
            : BadRequest(ApiResponse<GetMyProfileResponse>.Fail(result.ErrorMessage!));
    }

    [HttpPut("me")]
    public async Task<IActionResult> UpdateMe(UpdateMyProfileCommand command, CancellationToken cancellationToken)
    {
        var result = await sender.Send(command, cancellationToken);

        return result.Succeeded
            ? Ok(ApiResponse<UpdateMyProfileResponse>.Success(result.Data!))
            : BadRequest(ApiResponse<UpdateMyProfileResponse>.Fail(result.ErrorMessage!));
    }

    [HttpPost("me/change-password")]
    public async Task<IActionResult> ChangeMyPassword(ChangeMyPasswordCommand command, CancellationToken cancellationToken)
    {
        var result = await sender.Send(command, cancellationToken);

        return result.Succeeded
            ? Ok(ApiResponse<bool>.Success(result.Data))
            : BadRequest(ApiResponse<bool>.Fail(result.ErrorMessage!));
    }
}
