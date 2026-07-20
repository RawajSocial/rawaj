using MediatR;
using Rawaj.Application.Common.Models;
using Rawaj.Application.Features.Tenants.Common;

namespace Rawaj.Application.Features.Tenants.GetAccountSetup;

public record GetAccountSetupQuery(Guid BrandProfileId) : IRequest<Result<TenantAccountSetupResponse?>>;
