using MediatR;
using Rawaj.Application.Common.Models;
using Rawaj.Application.Features.Tenants.Common;

namespace Rawaj.Application.Features.Tenants.UploadBrandImage;

public record UploadBrandImageCommand(
    Guid BrandProfileId,
    Stream Content,
    string FileName,
    string ContentType,
    long Length) : IRequest<Result<TenantBrandProfileResponse>>;
