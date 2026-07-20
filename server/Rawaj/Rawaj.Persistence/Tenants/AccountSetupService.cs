using Microsoft.EntityFrameworkCore;
using Rawaj.Application.Common.Interfaces;
using Rawaj.Application.Common.Models;
using Rawaj.Domain.Entities.Tenants;
using Rawaj.Domain.ValueObjects;

namespace Rawaj.Persistence.Tenants;

public class AccountSetupService(AppDbContext dbContext, IBrandProfileService brandProfileService) : IAccountSetupService
{
    public async Task<GetAccountSetupResult> GetAsync(Guid userId, Guid brandProfileId, CancellationToken cancellationToken)
    {
        var authorization = await brandProfileService.GetAuthorizedBrandProfileAsync(userId, brandProfileId, cancellationToken);

        if (authorization.Outcome is BrandImageOutcome.NotFound)
        {
            return new GetAccountSetupResult(GetAccountSetupOutcome.NotFound, null, null);
        }

        if (authorization.Outcome is BrandImageOutcome.Forbidden)
        {
            return new GetAccountSetupResult(GetAccountSetupOutcome.Forbidden, null, null);
        }

        var accountSetup = await dbContext.TenantAccountSetups.FirstOrDefaultAsync(a => a.BrandProfileId == brandProfileId, cancellationToken);

        return new GetAccountSetupResult(GetAccountSetupOutcome.Success, accountSetup, authorization.BrandProfile!.BrandInfo);
    }

    public async Task<SaveAccountSetupResult> SaveAsync(Guid userId, Guid brandProfileId, AccountSetupFields fields, CancellationToken cancellationToken)
    {
        var authorization = await brandProfileService.GetAuthorizedBrandProfileAsync(userId, brandProfileId, cancellationToken);

        if (authorization.Outcome is BrandImageOutcome.NotFound)
        {
            return new SaveAccountSetupResult(SaveAccountSetupOutcome.NotFound, null, null);
        }

        if (authorization.Outcome is BrandImageOutcome.Forbidden)
        {
            return new SaveAccountSetupResult(SaveAccountSetupOutcome.Forbidden, null, null);
        }

        var now = DateTime.UtcNow;
        var brandProfile = authorization.BrandProfile!;
        var accountSetup = await dbContext.TenantAccountSetups.FirstOrDefaultAsync(a => a.BrandProfileId == brandProfileId, cancellationToken);

        if (accountSetup is null)
        {
            accountSetup = new TenantAccountSetup
            {
                Id = Guid.NewGuid(),
                BrandProfileId = brandProfileId,
                CreatedAt = now
            };
            dbContext.TenantAccountSetups.Add(accountSetup);
        }

        accountSetup.AccountType = fields.AccountType;
        accountSetup.Country = fields.Country;
        accountSetup.City = fields.City;
        accountSetup.UpdatedAt = now;

        accountSetup.SocialLinks = new SocialLinks
        {
            Facebook = fields.Facebook,
            Instagram = fields.Instagram,
            Youtube = fields.Youtube,
            TikTok = fields.TikTok,
            LinkedIn = fields.LinkedIn,
            X = fields.X,
            Snapchat = fields.Snapchat
        };

        accountSetup.AgencyDetails = fields.AccountType == "agency"
            ? new AgencyDetails
            {
                AgencyName = fields.AgencyName,
                AgencyPhone = fields.AgencyPhone,
                AgencyCountryCode = fields.AgencyCountryCode,
                AgencySize = fields.AgencySize,
                ActiveClients = fields.ActiveClients,
                PrimaryServices = fields.PrimaryServices ?? []
            }
            : null;

        accountSetup.BusinessDetails = fields.AccountType == "business"
            ? new BusinessDetails
            {
                BusinessName = fields.BusinessName,
                BusinessPhone = fields.BusinessPhone,
                BusinessCountryCode = fields.BusinessCountryCode,
                BusinessSize = fields.BusinessSize,
                HearAboutUs = fields.HearAboutUs
            }
            : null;

        var existingBrandInfo = brandProfile.BrandInfo;
        var updatedBrandInfo = new BrandInfo
        {
            Tagline = existingBrandInfo?.Tagline,
            Industry = fields.Industry,
            TargetAudience = existingBrandInfo?.TargetAudience,
            Colors = existingBrandInfo?.Colors ?? [],
            LogoUrl = existingBrandInfo?.LogoUrl,
            WebsiteUrl = fields.Website,
            SupportedLanguages = existingBrandInfo?.SupportedLanguages ?? [],
            Keywords = existingBrandInfo?.Keywords ?? [],
            IsDefault = existingBrandInfo?.IsDefault ?? false
        };
        brandProfile.BrandInfo = updatedBrandInfo;
        brandProfile.UpdatedAt = now;

        await dbContext.SaveChangesAsync(cancellationToken);

        return new SaveAccountSetupResult(SaveAccountSetupOutcome.Saved, accountSetup, updatedBrandInfo);
    }
}
