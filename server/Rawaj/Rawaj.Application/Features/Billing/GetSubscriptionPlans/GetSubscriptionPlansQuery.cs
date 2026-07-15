using MediatR;
using Rawaj.Application.Common.Models;

namespace Rawaj.Application.Features.Billing.GetSubscriptionPlans;

public record GetSubscriptionPlansQuery : IRequest<Result<List<SubscriptionPlanSummary>>>;
