using MediatR;
using Microsoft.EntityFrameworkCore;
using Rawaj.Application.Common.Interfaces;
using Rawaj.Application.Common.Models;
using Rawaj.Domain.Enums;

namespace Rawaj.Application.Features.Brands.ArchiveBrandProfile;

public class ArchiveBrandProfileCommandHandler(IApplicationDbContext dbContext, ICurrentTenantContext currentTenantContext)
    : IRequestHandler<ArchiveBrandProfileCommand, Result<bool>>
{
    public async Task<Result<bool>> Handle(ArchiveBrandProfileCommand request, CancellationToken cancellationToken)
    {
        var tenantId = currentTenantContext.TenantId!.Value;

        var brandProfile = await dbContext.TenantBrandProfiles
            .FirstOrDefaultAsync(b => b.Id == request.BrandProfileId && b.TenantId == tenantId, cancellationToken);
        if (brandProfile is null)
        {
            return Result<bool>.Failure("Brand profile not found.");
        }

        brandProfile.Status = BrandProfileStatus.Archived;
        if (brandProfile.BrandInfo is not null)
        {
            brandProfile.BrandInfo.IsDefault = false;
        }
        brandProfile.UpdatedAt = DateTime.UtcNow;

        await dbContext.SaveChangesAsync(cancellationToken);

        return Result<bool>.Success(true);
    }
}
