using MediatR;
using Rawaj.Application.Common.Interfaces;
using Rawaj.Application.Common.Models;
using Rawaj.Domain.Enums;

namespace Rawaj.Application.Features.SocialAccounts.GetAuthorizationUrl;

public record GetAuthorizationUrlCommand(Guid BrandProfileId, SocialPlatform Platform, string RedirectUri)
    : IRequest<Result<GetAuthorizationUrlResponse>>, IRequireTenantRole
{
    public TenantMemberRole MinimumRole => TenantMemberRole.Admin;
}
