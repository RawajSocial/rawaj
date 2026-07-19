using Rawaj.Application.Common.Models;
using Rawaj.Domain.Entities.Tenants;
using Rawaj.Domain.Enums;
using Rawaj.Domain.ValueObjects;

namespace Rawaj.Application.Common.Interfaces;

public interface IBrandProfileService
{
    Task<CreateBrandProfileResult> CreateFirstBrandProfileAsync(
        Guid ownerUserId,
        string name,
        string? description,
        BrandVoice? brandVoice,
        BrandInfo? brandInfo,
        CancellationToken cancellationToken);

    Task<BrandImageResult> GetAuthorizedBrandProfileAsync(Guid userId, Guid brandProfileId, CancellationToken cancellationToken);

    Task<TenantBrandProfile> SetBrandImageAsync(Guid brandProfileId, string? imageUrl, CancellationToken cancellationToken);
}
