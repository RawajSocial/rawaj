using MediatR;
using Rawaj.Application.Common.Models;
using Rawaj.Domain.Enums;

namespace Rawaj.Application.Features.Tenants.CreateTenant;

public record CreateTenantCommand(
    string Name,
    string Subdomain,
    TenantType TenantType) : IRequest<Result<CreateTenantResponse>>;
