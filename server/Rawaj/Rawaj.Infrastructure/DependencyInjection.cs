using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Rawaj.Application.Common.Interfaces;
using Rawaj.Infrastructure.Auth;
using Rawaj.Infrastructure.Email;

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

        return services;
    }
}
