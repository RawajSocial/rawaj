using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Rawaj.Application.Features.Users.ChangeMyPassword;
using Rawaj.Application.Features.Users.DeleteMyAvatar;
using Rawaj.Application.Features.Users.GetMyProfile;
using Rawaj.Application.Features.Users.RequestEmailOtp;
using Rawaj.Application.Features.Users.UpdateMyAvatar;
using Rawaj.Application.Features.Users.UpdateMyProfile;
using Rawaj.Application.Features.Users.VerifyEmailOtp;
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
            ? Ok(ApiResponse<GetMyProfileResponse>.Success(result.Data!))
            : BadRequest(ApiResponse<GetMyProfileResponse>.Fail(result.ErrorMessage!));
    }

    [HttpPost("me/change-password")]
    public async Task<IActionResult> ChangeMyPassword(ChangeMyPasswordCommand command, CancellationToken cancellationToken)
    {
        var result = await sender.Send(command, cancellationToken);

        return result.Succeeded
            ? Ok(ApiResponse<bool>.Success(result.Data))
            : BadRequest(ApiResponse<bool>.Fail(result.ErrorMessage!));
    }

    [HttpPost("me/avatar")]
    public async Task<IActionResult> UpdateAvatar(IFormFile avatar, CancellationToken cancellationToken)
    {
        await using var stream = new MemoryStream();
        await avatar.CopyToAsync(stream, cancellationToken);

        var command = new UpdateMyAvatarCommand(stream.ToArray(), avatar.ContentType, avatar.FileName);
        var result = await sender.Send(command, cancellationToken);

        return result.Succeeded
            ? Ok(ApiResponse<UpdateMyAvatarResponse>.Success(result.Data!))
            : BadRequest(ApiResponse<UpdateMyAvatarResponse>.Fail(result.ErrorMessage!));
    }

    [HttpDelete("me/avatar")]
    public async Task<IActionResult> DeleteAvatar(CancellationToken cancellationToken)
    {
        var result = await sender.Send(new DeleteMyAvatarCommand(), cancellationToken);

        return result.Succeeded
            ? Ok(ApiResponse<bool>.Success(result.Data))
            : BadRequest(ApiResponse<bool>.Fail(result.ErrorMessage!));
    }

    [HttpPost("me/email/request-otp")]
    [EnableRateLimiting("auth")]
    public async Task<IActionResult> RequestEmailOtp(CancellationToken cancellationToken)
    {
        var result = await sender.Send(new RequestEmailOtpCommand(), cancellationToken);

        return result.Succeeded
            ? Ok(ApiResponse<bool>.Success(result.Data))
            : BadRequest(ApiResponse<bool>.Fail(result.ErrorMessage!));
    }

    [HttpPost("me/email/verify-otp")]
    [EnableRateLimiting("auth")]
    public async Task<IActionResult> VerifyEmailOtp(VerifyEmailOtpCommand command, CancellationToken cancellationToken)
    {
        var result = await sender.Send(command, cancellationToken);

        return result.Succeeded
            ? Ok(ApiResponse<bool>.Success(result.Data))
            : BadRequest(ApiResponse<bool>.Fail(result.ErrorMessage!));
    }
}
