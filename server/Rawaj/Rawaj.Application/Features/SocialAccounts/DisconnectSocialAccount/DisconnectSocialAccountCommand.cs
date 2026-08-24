using MediatR;
using Rawaj.Application.Common.Interfaces;
using Rawaj.Application.Common.Models;
using Rawaj.Domain.Enums;

namespace Rawaj.Application.Features.SocialAccounts.DisconnectSocialAccount;

public record DisconnectSocialAccountCommand(Guid SocialAccountId)
    : IRequest<Result<DisconnectSocialAccountResponse>>, IRequireTenantRole
{
    public TenantMemberRole MinimumRole => TenantMemberRole.Admin;
}
