using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Rawaj.Application.Common.Interfaces;
using Rawaj.Persistence.Campaigns;
using Rawaj.Persistence.Common;
using Rawaj.Persistence.Identity;
using Rawaj.Persistence.Tenants;

namespace Rawaj.Persistence;

public static class DependencyInjection
{
    public static IServiceCollection AddPersistence(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddDbContext<AppDbContext>(options =>
            options.UseSqlServer(configuration.GetConnectionString("DefaultConnection")));

        services
            .AddIdentityCore<ApplicationUser>(options =>
            {
                options.Password.RequiredLength = 8;
                options.User.RequireUniqueEmail = true;
            })
            .AddEntityFrameworkStores<AppDbContext>()
            .AddDefaultTokenProviders();

        services.AddScoped<IIdentityService, IdentityService>();
        services.AddScoped<ITenantProvisioningService, TenantProvisioningService>();
        services.AddScoped<IBrandProfileService, BrandProfileService>();
        services.AddScoped<ICampaignOnboardingService, CampaignOnboardingService>();
        services.AddScoped<IUnitOfWork, UnitOfWork>();

        return services;
    }
}
