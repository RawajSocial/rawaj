using MediatR;
using Rawaj.Application.Common.Interfaces;
using Rawaj.Application.Common.Models;
using Rawaj.Domain.Enums;

namespace Rawaj.Application.Features.Billing.GetBillingHistory;

public record GetBillingHistoryQuery(int Page = 1, int PageSize = 20)
    : IRequest<Result<GetBillingHistoryResponse>>, IRequireTenantRole
{
    public TenantMemberRole MinimumRole => TenantMemberRole.Admin;
}
