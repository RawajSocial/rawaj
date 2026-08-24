using MediatR;
using Rawaj.Application.Common.Interfaces;
using Rawaj.Application.Common.Models;

namespace Rawaj.Application.Features.Brands.UploadBrandLogo;

public class UploadBrandLogoCommandHandler(IMediaStorageService mediaStorageService)
    : IRequestHandler<UploadBrandLogoCommand, Result<UploadBrandLogoResponse>>
{
    private const string LogosFolder = "brand-logos";

    public async Task<Result<UploadBrandLogoResponse>> Handle(UploadBrandLogoCommand request, CancellationToken cancellationToken)
    {
        // Not tied to a brand row at upload time (this endpoint runs before the brand profile it
        // will be attached to necessarily exists yet), so it can't use a deterministic per-brand
        // public id the way avatars do — each upload gets its own random id under a shared folder.
        string logoUrl;
        if (mediaStorageService.IsConfigured)
        {
            var upload = await mediaStorageService.UploadImageAsync(request.Content, request.ContentType, LogosFolder, cancellationToken);
            logoUrl = upload.Url;
        }
        else
        {
            logoUrl = $"data:{request.ContentType};base64,{Convert.ToBase64String(request.Content)}";
        }

        return Result<UploadBrandLogoResponse>.Success(new UploadBrandLogoResponse(logoUrl));
    }
}
