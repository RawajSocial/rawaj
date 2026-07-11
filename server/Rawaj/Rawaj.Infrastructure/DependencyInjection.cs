using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Rawaj.Application.Common.Interfaces;
using Rawaj.Infrastructure.Auth;

namespace Rawaj.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<JwtSettings>(configuration.GetSection(JwtSettings.SectionName));
        services.AddScoped<IJwtTokenGenerator, JwtTokenGenerator>();

        return services;
    }
}
