using MediatR;
using Rawaj.Application.Common.Models;

namespace Rawaj.Application.Features.Billing.GetPublicCoinPricing;

/// <summary>
/// The anonymous-friendly subset of <see cref="Rawaj.Application.Features.Billing.GetCoinPricing.GetCoinPricingQuery"/>
/// — base coin costs, coin packages, and add-on prices, with no per-tenant discount or free-trial
/// state (those require a signed-in tenant). Backs the landing page's pricing section and the
/// public `/pricing` explainer page, neither of which require login.
/// </summary>
public record GetPublicCoinPricingQuery : IRequest<Result<GetPublicCoinPricingResponse>>;
