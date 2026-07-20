using MediatR;
using Rawaj.Application.Common.Models;
using Rawaj.Application.Features.Tenants.Common;

namespace Rawaj.Application.Features.Tenants.GetBrandProfileById;

public record GetBrandProfileByIdQuery(Guid BrandProfileId) : IRequest<Result<TenantBrandProfileResponse>>;
