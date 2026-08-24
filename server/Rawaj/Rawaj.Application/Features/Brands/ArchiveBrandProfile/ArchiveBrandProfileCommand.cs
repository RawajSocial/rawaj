using MediatR;
using Rawaj.Application.Common.Interfaces;
using Rawaj.Application.Common.Models;
using Rawaj.Domain.Enums;

namespace Rawaj.Application.Features.Brands.ArchiveBrandProfile;

public record ArchiveBrandProfileCommand(Guid BrandProfileId) : IRequest<Result<bool>>, IRequireTenantRole
{
    public TenantMemberRole MinimumRole => TenantMemberRole.Admin;
}
