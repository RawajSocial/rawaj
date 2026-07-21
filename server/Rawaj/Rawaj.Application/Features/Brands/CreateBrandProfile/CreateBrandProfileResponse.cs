using Rawaj.Domain.Enums;

namespace Rawaj.Application.Features.Brands.CreateBrandProfile;

public record CreateBrandProfileResponse(
    Guid BrandProfileId,
    Guid TenantId,
    string Name,
    BrandProfileStatus Status,
    bool IsDefault);
