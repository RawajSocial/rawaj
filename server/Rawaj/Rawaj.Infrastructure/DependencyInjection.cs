using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Rawaj.Application.Common.Interfaces;
using Rawaj.Domain.Enums;
using Rawaj.Infrastructure.Ai;
using Rawaj.Infrastructure.Auth;
using Rawaj.Infrastructure.BackgroundJobs;
using Rawaj.Infrastructure.Media;
using Rawaj.Infrastructure.Resilience;
using Rawaj.Infrastructure.Scraping;
using Rawaj.Infrastructure.Security;
using Rawaj.Infrastructure.SocialOAuth;
using Rawaj.Infrastructure.SocialPublishing;

namespace Rawaj.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<JwtSettings>(configuration.GetSection(JwtSettings.SectionName));
        services.AddScoped<IJwtTokenGenerator, JwtTokenGenerator>();

        services.Configure<GroqSettings>(configuration.GetSection(GroqSettings.SectionName));
        services.Configure<HuggingFaceSettings>(configuration.GetSection(HuggingFaceSettings.SectionName));
        services.Configure<TavilySettings>(configuration.GetSection(TavilySettings.SectionName));
        services.Configure<EncryptionSettings>(configuration.GetSection(EncryptionSettings.SectionName));
        services.Configure<MetaOAuthSettings>(configuration.GetSection(MetaOAuthSettings.SectionName));
        services.Configure<LinkedInOAuthSettings>(configuration.GetSection(LinkedInOAuthSettings.SectionName));
        services.Configure<PublicImageHostingSettings>(configuration.GetSection(PublicImageHostingSettings.SectionName));

        services.AddResilientHttpClient("Groq", attemptTimeout: TimeSpan.FromSeconds(45), totalTimeout: TimeSpan.FromSeconds(120));
        services.AddResilientHttpClient("HuggingFace", attemptTimeout: TimeSpan.FromSeconds(60), totalTimeout: TimeSpan.FromSeconds(150));
        services.AddResilientHttpClient("Tavily", attemptTimeout: TimeSpan.FromSeconds(30), totalTimeout: TimeSpan.FromSeconds(60));
        services.AddResilientHttpClient("Meta", attemptTimeout: TimeSpan.FromSeconds(20), totalTimeout: TimeSpan.FromSeconds(45));
        services.AddResilientHttpClient("LinkedIn", attemptTimeout: TimeSpan.FromSeconds(20), totalTimeout: TimeSpan.FromSeconds(45));
        services.AddResilientHttpClient(
            "WebScraper",
            attemptTimeout: TimeSpan.FromSeconds(20),
            totalTimeout: TimeSpan.FromSeconds(45),
            configureClient: client => client.DefaultRequestHeaders.UserAgent.ParseAdd(
                "Mozilla/5.0 (compatible; RawajBot/1.0; +https://rawaj.example/bot)"));

        services.AddScoped<IAiTextGenerationService, GroqTextGenerationService>();
        services.AddScoped<IAiImageGenerationService, HuggingFaceImageGenerationService>();
        services.AddScoped<ITavilySearchService, TavilySearchService>();
        services.AddScoped<IWebScraperService, HtmlAgilityPackWebScraperService>();
        services.AddSingleton<ITokenEncryptor, AesTokenEncryptor>();
        services.AddSingleton<IPublicImageHostingService, LocalFilePublicImageHostingService>();
        services.AddSingleton<IAvatarStorageService, LocalAvatarStorageService>();

        services.AddMemoryCache();
        services.AddSingleton<IOAuthStateStore, OAuthStateStore>();

        // OAuth providers are only registered once real app credentials are configured, so that
        // GetAuthorizationUrl honestly reports "not configured" for a platform instead of building
        // a broken authorization URL with an empty client_id.
        if (!string.IsNullOrWhiteSpace(configuration["SocialOAuth:Meta:ClientId"]))
        {
            services.AddScoped<ISocialOAuthProvider>(sp => new MetaOAuthProvider(
                SocialPlatform.Facebook,
                sp.GetRequiredService<IHttpClientFactory>(),
                sp.GetRequiredService<IOptions<MetaOAuthSettings>>(),
                sp.GetRequiredService<ILogger<MetaOAuthProvider>>()));
            services.AddScoped<ISocialOAuthProvider>(sp => new MetaOAuthProvider(
                SocialPlatform.Instagram,
                sp.GetRequiredService<IHttpClientFactory>(),
                sp.GetRequiredService<IOptions<MetaOAuthSettings>>(),
                sp.GetRequiredService<ILogger<MetaOAuthProvider>>()));
        }

        // LinkedIn (and any other platform beyond Facebook/Instagram) is intentionally deferred -
        // the provider implementation stays in the codebase for later, but is not registered so it
        // can never be offered as a connect option, regardless of configuration.

        // Publishing only needs a valid stored access token (however it was obtained), not our
        // own app's OAuth client id/secret, so it is registered unconditionally.
        services.AddScoped<ISocialPublisher, MetaPostPublisher>();
        services.AddScoped<ISocialPublisher, InstagramPostPublisher>();
        services.AddScoped<ISocialAnalyticsProvider, MetaAnalyticsProvider>();
        services.AddScoped<ISocialPostStatusChecker, MetaPostStatusChecker>();

        services.AddHostedService<ScheduledPostPublisherHostedService>();
        services.AddHostedService<SocialAccountTokenRefresherHostedService>();
        services.AddHostedService<PostStatusSyncHostedService>();

        return services;
    }
}
