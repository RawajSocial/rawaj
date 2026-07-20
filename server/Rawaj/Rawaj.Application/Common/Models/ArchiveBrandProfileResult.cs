using Rawaj.Domain.Entities.Tenants;

namespace Rawaj.Application.Common.Models;

public enum ArchiveBrandProfileOutcome
{
    Archived,
    NotFound,
    Forbidden
}

public record ArchiveBrandProfileResult(ArchiveBrandProfileOutcome Outcome, TenantBrandProfile? BrandProfile);
