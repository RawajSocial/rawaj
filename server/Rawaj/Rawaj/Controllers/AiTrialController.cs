using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Rawaj.Application.Features.AiTrial.GenerateTrialContent;
using Rawaj.Application.Features.AiTrial.GenerateTrialImage;
using Rawaj.Common;

namespace Rawaj.Controllers;

[ApiController]
[Authorize]
[Route("api/v1/ai-trial")]
public class AiTrialController(ISender sender) : ControllerBase
{
    [HttpPost("content")]
    public async Task<IActionResult> GenerateContent(GenerateTrialContentCommand command, CancellationToken cancellationToken)
    {
        var result = await sender.Send(command, cancellationToken);

        return result.Succeeded
            ? Ok(ApiResponse<GenerateTrialContentResponse>.Success(result.Data!))
            : BadRequest(ApiResponse<GenerateTrialContentResponse>.Fail(result.ErrorMessage!));
    }

    [HttpPost("image")]
    public async Task<IActionResult> GenerateImage(GenerateTrialImageCommand command, CancellationToken cancellationToken)
    {
        var result = await sender.Send(command, cancellationToken);

        return result.Succeeded
            ? Ok(ApiResponse<GenerateTrialImageResponse>.Success(result.Data!))
            : BadRequest(ApiResponse<GenerateTrialImageResponse>.Fail(result.ErrorMessage!));
    }
}
