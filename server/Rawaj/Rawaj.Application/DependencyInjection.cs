using FluentValidation;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Rawaj.Application.Common.Behaviors;
using Rawaj.Application.Common.Interfaces;
using Rawaj.Application.Common.Services;
using Rawaj.Application.Features.AiPipeline.Services;

namespace Rawaj.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        var assembly = typeof(DependencyInjection).Assembly;

        services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(assembly));
        services.AddValidatorsFromAssembly(assembly);

        services.AddScoped<ICurrentTenantContext, CurrentTenantContext>();
        services.AddScoped<TenantProvisioningService>();
        services.AddScoped<IPipelineArtifactStore, PipelineArtifactStore>();
        services.AddSingleton<IPromptTemplateProvider, PromptTemplateProvider>();
        services.AddScoped<IPipelineApprovalService, PipelineApprovalService>();
        services.AddScoped<IPipelineStrategyRefinementService, PipelineStrategyRefinementService>();
        services.AddScoped<IPipelineOrchestrator, PipelineOrchestrator>();

        // Stage executors are resolved as a set and matched on their Kind, so adding a stage is one
        // new class plus one entry in AiPipelinePolicy.Graph — never an edit to a dispatch switch.
        // Done by reflection rather than by pulling in Scrutor for a single registration.
        foreach (var executorType in assembly.GetTypes()
                     .Where(t => t is { IsAbstract: false, IsInterface: false })
                     .Where(t => typeof(IPipelineStageExecutor).IsAssignableFrom(t)))
        {
            services.AddScoped(typeof(IPipelineStageExecutor), executorType);
        }

        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(TenantAuthorizationBehavior<,>));
        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(BrandAccessAuthorizationBehavior<,>));
        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));
        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(ConcurrencyBehavior<,>));

        return services;
    }
}
