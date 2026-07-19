using MediatR;
using Rawaj.Application.Common.Models;
using Rawaj.Application.Features.Tenants.Common;

namespace Rawaj.Application.Features.Tenants.RemoveBrandImage;

public record RemoveBrandImageCommand(Guid BrandProfileId) : IRequest<Result<TenantBrandProfileResponse>>;
