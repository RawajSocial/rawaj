using Rawaj.Domain.Enums;

namespace Rawaj.Application.Features.Brands.GetBrandProfiles;

public record BrandProfileSummary(
    Guid BrandProfileId,
    string Name,
    string? Description,
    BrandVoice? BrandVoice,
    BrandProfileStatus Status,
    bool IsDefault);
