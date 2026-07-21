using MediatR;
using Rawaj.Application.Common.Models;

namespace Rawaj.Application.Features.Admin.GetTenants;

public record GetTenantsQuery(int Page = 1, int PageSize = 20) : IRequest<Result<PagedResult<AdminTenantSummary>>>;
