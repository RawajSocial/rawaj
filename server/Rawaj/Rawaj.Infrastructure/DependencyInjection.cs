using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Rawaj.Application.Common.Interfaces;
using Rawaj.Application.Common.Options;
using Rawaj.Infrastructure.Auth;
using Rawaj.Infrastructure.Email;
using Rawaj.Infrastructure.Storage;

namespace Rawaj.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<JwtSettings>(configuration.GetSection(JwtSettings.SectionName));
        services.AddScoped<IJwtTokenGenerator, JwtTokenGenerator>();

        services.AddHttpContextAccessor();
        services.AddScoped<ICurrentUserService, CurrentUserService>();

        services.Configure<EmailSettings>(configuration.GetSection(EmailSettings.SectionName));
        services.AddScoped<IEmailService, SmtpEmailService>();

        services.Configure<StorageSettings>(configuration.GetSection(StorageSettings.SectionName));
        services.AddScoped<IStorageService, LocalFileStorageService>();

        services.Configure<BrandImageUploadOptions>(configuration.GetSection(BrandImageUploadOptions.SectionName));

        return services;
    }
}
