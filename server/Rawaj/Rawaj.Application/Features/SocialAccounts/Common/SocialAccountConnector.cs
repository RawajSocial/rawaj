using Microsoft.EntityFrameworkCore;
using Rawaj.Application.Common.Interfaces;
using Rawaj.Application.Common.Models;
using Rawaj.Domain.Entities.SocialMedia;
using Rawaj.Domain.Enums;

namespace Rawaj.Application.Features.SocialAccounts.Common;

public static class SocialAccountConnector
{
    public static async Task<Result<SocialAccount>> ConnectAsync(
        IApplicationDbContext dbContext,
        ITokenEncryptor tokenEncryptor,
        Guid tenantId,
        Guid brandProfileId,
        SocialPlatform platform,
        string accountName,
        string accountIdExternal,
        string accessToken,
        string? refreshToken,
        DateTime? tokenExpiresAt,
        List<string>? scopes,
        CancellationToken cancellationToken)
    {
        var brandProfileBelongsToTenant = await dbContext.TenantBrandProfiles
            .AnyAsync(b => b.Id == brandProfileId && b.TenantId == tenantId, cancellationToken);
        if (!brandProfileBelongsToTenant)
        {
            return Result<SocialAccount>.Failure("Brand profile not found.");
        }

        var existing = await dbContext.SocialAccounts.FirstOrDefaultAsync(
            s => s.BrandProfileId == brandProfileId
                && s.Platform == platform
                && s.AccountIdExternal == accountIdExternal,
            cancellationToken);

        var now = DateTime.UtcNow;

        if (existing is not null)
        {
            if (existing.IsActive)
            {
                return Result<SocialAccount>.Failure("This account is already connected.");
            }

            existing.AccountName = accountName;
            existing.Token = tokenEncryptor.Encrypt(accessToken);
            existing.RefreshTokenEnc = refreshToken is null ? null : tokenEncryptor.Encrypt(refreshToken);
            existing.TokenExpiresAt = tokenExpiresAt;
            existing.Scopes = scopes ?? [];
            existing.IsActive = true;
            existing.LastVerifiedAt = now;

            await dbContext.SaveChangesAsync(cancellationToken);
            return Result<SocialAccount>.Success(existing);
        }

        var maxSocialAccounts = await (
            from tenant in dbContext.Tenants
            join subscription in dbContext.Subscriptions on tenant.SubscriptionId equals subscription.Id
            join plan in dbContext.SubscriptionPlans on subscription.SubscriptionPlanId equals plan.Id
            where tenant.Id == tenantId
            select plan.MaxSocialAccounts
        ).FirstAsync(cancellationToken);

        var connectedCount = await dbContext.SocialAccounts
            .CountAsync(s => s.BrandProfileId == brandProfileId && s.IsActive, cancellationToken);

        if (connectedCount >= maxSocialAccounts)
        {
            return Result<SocialAccount>.Failure(
                $"Your subscription plan allows a maximum of {maxSocialAccounts} connected social account(s). Upgrade to connect more.");
        }

        var socialAccount = new SocialAccount
        {
            Id = Guid.NewGuid(),
            BrandProfileId = brandProfileId,
            Platform = platform,
            AccountName = accountName,
            AccountIdExternal = accountIdExternal,
            Token = tokenEncryptor.Encrypt(accessToken),
            RefreshTokenEnc = refreshToken is null ? null : tokenEncryptor.Encrypt(refreshToken),
            TokenExpiresAt = tokenExpiresAt,
            Scopes = scopes ?? [],
            IsActive = true,
            LastVerifiedAt = now,
            CreatedAt = now
        };

        dbContext.SocialAccounts.Add(socialAccount);
        await dbContext.SaveChangesAsync(cancellationToken);

        return Result<SocialAccount>.Success(socialAccount);
    }
}
