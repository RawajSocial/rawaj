using Rawaj.Domain.Entities.Tenants;

namespace Rawaj.Application.Common.Models;

public record BrandImageResult(BrandImageOutcome Outcome, TenantBrandProfile? BrandProfile);
