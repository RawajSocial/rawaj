using MediatR;
using Rawaj.Application.Common.Models;

namespace Rawaj.Application.Features.Tenants.GetMyTenant;

public record GetMyTenantQuery : IRequest<Result<GetMyTenantResponse>>;
