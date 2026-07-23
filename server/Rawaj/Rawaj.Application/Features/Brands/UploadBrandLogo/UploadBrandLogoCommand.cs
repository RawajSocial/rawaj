using MediatR;
using Rawaj.Application.Common.Interfaces;
using Rawaj.Application.Common.Models;
using Rawaj.Domain.Enums;

namespace Rawaj.Application.Features.Brands.UploadBrandLogo;

public record UploadBrandLogoCommand(byte[] Content, string ContentType, string FileName)
    : IRequest<Result<UploadBrandLogoResponse>>, IRequireTenantRole
{
    public TenantMemberRole MinimumRole => TenantMemberRole.Admin;
}
