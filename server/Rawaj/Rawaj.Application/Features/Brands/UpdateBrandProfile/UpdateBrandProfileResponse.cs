using Rawaj.Domain.Enums;

namespace Rawaj.Application.Features.Brands.UpdateBrandProfile;

public record UpdateBrandProfileResponse(
    Guid BrandProfileId,
    string Name,
    string? Description,
    BrandVoice? BrandVoice,
    BrandProfileStatus Status,
    bool IsDefault);
