using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Rawaj.Application.Features.Tenants.Common;
using Rawaj.Application.Features.Tenants.CreateBrandProfile;
using Rawaj.Application.Features.Tenants.InviteMember;
using Rawaj.Application.Features.Tenants.RemoveBrandImage;
using Rawaj.Application.Features.Tenants.UploadBrandImage;
using Rawaj.Common;

namespace Rawaj.Controllers;

[ApiController]
[Authorize]
[Route("api/v1/tenants")]
public class TenantsController(ISender sender) : ControllerBase
{
    [HttpPost("invitations")]
    public async Task<IActionResult> InviteMember(InviteTenantMemberCommand command, CancellationToken cancellationToken)
    {
        var result = await sender.Send(command, cancellationToken);

        return result.Succeeded
            ? Ok(ApiResponse<string>.Success("Invitation sent."))
            : BadRequest(ApiResponse<string>.Fail(result.ErrorMessage!));
    }

    [HttpPost("brand-profile")]
    public async Task<IActionResult> CreateBrandProfile(CreateBrandProfileCommand command, CancellationToken cancellationToken)
    {
        var result = await sender.Send(command, cancellationToken);

        return result.Succeeded
            ? Ok(ApiResponse<TenantBrandProfileResponse>.Success(result.Data!))
            : BadRequest(ApiResponse<TenantBrandProfileResponse>.Fail(result.ErrorMessage!));
    }

    [HttpPost("brand-profile/{brandProfileId:guid}/image")]
    public async Task<IActionResult> UploadBrandImage(Guid brandProfileId, IFormFile file, CancellationToken cancellationToken)
    {
        var openedStream = file.OpenReadStream();
        var stream = openedStream.CanSeek ? openedStream : new MemoryStream();
        if (!ReferenceEquals(stream, openedStream))
        {
            await using (openedStream)
            {
                await openedStream.CopyToAsync(stream, cancellationToken);
            }
            stream.Position = 0;
        }

        await using var _ = stream;
        var command = new UploadBrandImageCommand(brandProfileId, stream, file.FileName, file.ContentType, file.Length);
        var result = await sender.Send(command, cancellationToken);

        return result.Succeeded
            ? Ok(ApiResponse<TenantBrandProfileResponse>.Success(result.Data!))
            : BadRequest(ApiResponse<TenantBrandProfileResponse>.Fail(result.ErrorMessage!));
    }

    [HttpDelete("brand-profile/{brandProfileId:guid}/image")]
    public async Task<IActionResult> RemoveBrandImage(Guid brandProfileId, CancellationToken cancellationToken)
    {
        var result = await sender.Send(new RemoveBrandImageCommand(brandProfileId), cancellationToken);

        return result.Succeeded
            ? Ok(ApiResponse<TenantBrandProfileResponse>.Success(result.Data!))
            : BadRequest(ApiResponse<TenantBrandProfileResponse>.Fail(result.ErrorMessage!));
    }
}
