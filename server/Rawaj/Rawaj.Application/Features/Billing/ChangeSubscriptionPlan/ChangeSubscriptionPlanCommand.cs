using MediatR;
using Rawaj.Application.Common.Interfaces;
using Rawaj.Application.Common.Models;
using Rawaj.Domain.Enums;

namespace Rawaj.Application.Features.Billing.ChangeSubscriptionPlan;

public record ChangeSubscriptionPlanCommand(Guid SubscriptionPlanId)
    : IRequest<Result<ChangeSubscriptionPlanResponse>>, IRequireTenantRole
{
    public TenantMemberRole MinimumRole => TenantMemberRole.Owner;
}
