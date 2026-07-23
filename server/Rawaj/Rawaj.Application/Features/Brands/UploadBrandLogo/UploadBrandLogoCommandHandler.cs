using MediatR;
using Rawaj.Application.Common.Interfaces;
using Rawaj.Application.Common.Models;

namespace Rawaj.Application.Features.Brands.UploadBrandLogo;

public class UploadBrandLogoCommandHandler(ILocalImageStorageService imageStorageService)
    : IRequestHandler<UploadBrandLogoCommand, Result<UploadBrandLogoResponse>>
{
    private const string LogosSubfolder = "brandprofiles/logourls";

    public async Task<Result<UploadBrandLogoResponse>> Handle(UploadBrandLogoCommand request, CancellationToken cancellationToken)
    {
        var logoUrl = await imageStorageService.SaveAsync(request.Content, request.ContentType, LogosSubfolder, cancellationToken);

        return Result<UploadBrandLogoResponse>.Success(new UploadBrandLogoResponse(logoUrl));
    }
}
