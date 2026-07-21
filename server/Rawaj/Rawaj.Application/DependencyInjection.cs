using FluentValidation;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Rawaj.Application.Common.Behaviors;
using Rawaj.Application.Common.Interfaces;
using Rawaj.Application.Common.Services;

namespace Rawaj.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        var assembly = typeof(DependencyInjection).Assembly;

        services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(assembly));
        services.AddValidatorsFromAssembly(assembly);

        services.AddScoped<ICurrentTenantContext, CurrentTenantContext>();

        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(TenantAuthorizationBehavior<,>));
        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(BrandAccessAuthorizationBehavior<,>));
        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));

        return services;
    }
}
