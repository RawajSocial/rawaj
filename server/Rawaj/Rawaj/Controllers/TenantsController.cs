using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Rawaj.Application.Features.Tenants.ArchiveBrandProfile;
using Rawaj.Application.Features.Tenants.Common;
using Rawaj.Application.Features.Tenants.CreateBrandProfile;
using Rawaj.Application.Features.Tenants.GetAccountSetup;
using Rawaj.Application.Features.Tenants.GetBrandProfileById;
using Rawaj.Application.Features.Tenants.GetBrandProfiles;
using Rawaj.Application.Features.Tenants.InviteMember;
using Rawaj.Application.Features.Tenants.RemoveBrandImage;
using Rawaj.Application.Features.Tenants.SaveAccountSetup;
using Rawaj.Application.Features.Tenants.UpdateBrandProfile;
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
            ? Ok(ApiResponse<InviteTenantMemberResponse>.Success(result.Data!))
            : BadRequest(ApiResponse<InviteTenantMemberResponse>.Fail(result.ErrorMessage!));
    }

    [HttpGet("brand-profile/{brandProfileId:guid}/account-setup")]
    public async Task<IActionResult> GetAccountSetup(Guid brandProfileId, CancellationToken cancellationToken)
    {
        var result = await sender.Send(new GetAccountSetupQuery(brandProfileId), cancellationToken);

        if (result.Succeeded)
        {
            return Ok(ApiResponse<TenantAccountSetupResponse?>.Success(result.Data));
        }

        return result.ErrorMessage == "Brand profile not found."
            ? NotFound(ApiResponse<TenantAccountSetupResponse?>.Fail(result.ErrorMessage!))
            : BadRequest(ApiResponse<TenantAccountSetupResponse?>.Fail(result.ErrorMessage!));
    }

    [HttpPut("brand-profile/{brandProfileId:guid}/account-setup")]
    public async Task<IActionResult> SaveAccountSetup(Guid brandProfileId, SaveAccountSetupCommand command, CancellationToken cancellationToken)
    {
        if (brandProfileId != command.BrandProfileId)
        {
            return BadRequest(ApiResponse<TenantAccountSetupResponse>.Fail("Route brandProfileId does not match request body."));
        }

        var result = await sender.Send(command, cancellationToken);

        return result.Succeeded
            ? Ok(ApiResponse<TenantAccountSetupResponse>.Success(result.Data!))
            : BadRequest(ApiResponse<TenantAccountSetupResponse>.Fail(result.ErrorMessage!));
    }

    [HttpGet("brand-profile")]
    public async Task<IActionResult> GetBrandProfiles(CancellationToken cancellationToken)
    {
        var result = await sender.Send(new GetBrandProfilesQuery(), cancellationToken);

        return result.Succeeded
            ? Ok(ApiResponse<List<TenantBrandProfileResponse>>.Success(result.Data!))
            : BadRequest(ApiResponse<List<TenantBrandProfileResponse>>.Fail(result.ErrorMessage!));
    }

    [HttpGet("brand-profile/{brandProfileId:guid}")]
    public async Task<IActionResult> GetBrandProfile(Guid brandProfileId, CancellationToken cancellationToken)
    {
        var result = await sender.Send(new GetBrandProfileByIdQuery(brandProfileId), cancellationToken);

        if (result.Succeeded)
        {
            return Ok(ApiResponse<TenantBrandProfileResponse>.Success(result.Data!));
        }

        return result.ErrorMessage == "Brand profile not found."
            ? NotFound(ApiResponse<TenantBrandProfileResponse>.Fail(result.ErrorMessage!))
            : BadRequest(ApiResponse<TenantBrandProfileResponse>.Fail(result.ErrorMessage!));
    }

    [HttpPost("brand-profile")]
    public async Task<IActionResult> CreateBrandProfile(CreateBrandProfileCommand command, CancellationToken cancellationToken)
    {
        var result = await sender.Send(command, cancellationToken);

        return result.Succeeded
            ? Ok(ApiResponse<TenantBrandProfileResponse>.Success(result.Data!))
            : BadRequest(ApiResponse<TenantBrandProfileResponse>.Fail(result.ErrorMessage!));
    }

    [HttpPut("brand-profile/{brandProfileId:guid}")]
    public async Task<IActionResult> UpdateBrandProfile(Guid brandProfileId, UpdateBrandProfileCommand command, CancellationToken cancellationToken)
    {
        if (brandProfileId != command.BrandProfileId)
        {
            return BadRequest(ApiResponse<TenantBrandProfileResponse>.Fail("Route brandProfileId does not match request body."));
        }

        var result = await sender.Send(command, cancellationToken);

        return result.Succeeded
            ? Ok(ApiResponse<TenantBrandProfileResponse>.Success(result.Data!))
            : BadRequest(ApiResponse<TenantBrandProfileResponse>.Fail(result.ErrorMessage!));
    }

    [HttpDelete("brand-profile/{brandProfileId:guid}")]
    public async Task<IActionResult> ArchiveBrandProfile(Guid brandProfileId, CancellationToken cancellationToken)
    {
        var result = await sender.Send(new ArchiveBrandProfileCommand(brandProfileId), cancellationToken);

        return result.Succeeded
            ? Ok(ApiResponse<string>.Success("Brand profile archived."))
            : BadRequest(ApiResponse<string>.Fail(result.ErrorMessage!));
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
