using MediatR;
using Microsoft.EntityFrameworkCore;
using Rawaj.Application.Common.Interfaces;
using Rawaj.Application.Common.Models;

namespace Rawaj.Application.Features.SocialAccounts.DisconnectSocialAccount;

public class DisconnectSocialAccountCommandHandler(IApplicationDbContext dbContext, ICurrentTenantContext currentTenantContext)
    : IRequestHandler<DisconnectSocialAccountCommand, Result<DisconnectSocialAccountResponse>>
{
    public async Task<Result<DisconnectSocialAccountResponse>> Handle(
        DisconnectSocialAccountCommand request, CancellationToken cancellationToken)
    {
        var tenantId = currentTenantContext.TenantId!.Value;

        var account = await dbContext.SocialAccounts
            .FirstOrDefaultAsync(
                s => s.Id == request.SocialAccountId && s.BrandProfile.TenantId == tenantId,
                cancellationToken);
        if (account is null)
        {
            return Result<DisconnectSocialAccountResponse>.Failure("Social account not found.");
        }

        account.IsActive = false;

        await dbContext.SaveChangesAsync(cancellationToken);

        return Result<DisconnectSocialAccountResponse>.Success(
            new DisconnectSocialAccountResponse(account.Id, account.IsActive));
    }
}
