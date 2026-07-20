using Rawaj.Domain.Entities.Tenants;

namespace Rawaj.Application.Common.Models;

public enum UpdateBrandProfileOutcome
{
    Updated,
    NotFound,
    Forbidden
}

public record UpdateBrandProfileResult(UpdateBrandProfileOutcome Outcome, TenantBrandProfile? BrandProfile);
