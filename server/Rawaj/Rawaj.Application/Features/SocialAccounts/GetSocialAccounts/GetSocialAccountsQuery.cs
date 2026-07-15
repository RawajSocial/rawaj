using MediatR;
using Rawaj.Application.Common.Interfaces;
using Rawaj.Application.Common.Models;
using Rawaj.Domain.Enums;

namespace Rawaj.Application.Features.SocialAccounts.GetSocialAccounts;

public record GetSocialAccountsQuery(Guid BrandProfileId)
    : IRequest<Result<List<SocialAccountSummary>>>, IRequireTenantRole
{
    public TenantMemberRole MinimumRole => TenantMemberRole.Viewer;
}
