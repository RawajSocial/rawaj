using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Rawaj.Application.Features.Auth.SendOtp;
using Rawaj.Application.Features.Auth.VerifyOtp;
using Rawaj.Common;

namespace Rawaj.Controllers;

[ApiController]
[Authorize]
[Route("api/v1/otp")]
public class OtpController(ISender sender) : ControllerBase
{
    [HttpPost("send")]
    public async Task<IActionResult> Send(CancellationToken cancellationToken)
    {
        var result = await sender.Send(new SendOtpCommand(), cancellationToken);

        return result.Succeeded
            ? Ok(ApiResponse<string>.Success("OTP sent."))
            : BadRequest(ApiResponse<string>.Fail(result.ErrorMessage!));
    }

    [HttpPost("verify")]
    public async Task<IActionResult> Verify(VerifyOtpCommand command, CancellationToken cancellationToken)
    {
        var result = await sender.Send(command, cancellationToken);

        return result.Succeeded
            ? Ok(ApiResponse<string>.Success("Email verified."))
            : BadRequest(ApiResponse<string>.Fail(result.ErrorMessage!));
    }
}
