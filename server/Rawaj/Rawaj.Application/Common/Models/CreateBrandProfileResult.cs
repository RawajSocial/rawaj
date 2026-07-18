using Rawaj.Domain.Entities.Tenants;

namespace Rawaj.Application.Common.Models;

public record CreateBrandProfileResult(CreateBrandProfileOutcome Outcome, TenantBrandProfile? BrandProfile);
