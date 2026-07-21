using System.Text;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.IdentityModel.Tokens;
using Rawaj.Application;
using Rawaj.Infrastructure;
using Rawaj.Infrastructure.Auth;
using Rawaj.Application.Common.Interfaces;
using Rawaj.Middleware;
using Rawaj.Persistence;
using Rawaj.Services;

namespace Rawaj
{
    public class Program
    {
        public static void Main(string[] args)
        {
            // Ensures WebRootPath resolves (StaticFileMiddleware warns otherwise) - must exist
            // before CreateBuilder resolves the web root, not just before UseStaticFiles.
            Directory.CreateDirectory(Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "media"));

            var builder = WebApplication.CreateBuilder(args);

            // Add services to the container.

            builder.Services.AddControllers()
                .AddJsonOptions(options =>
                    options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));
            // Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
            builder.Services.AddOpenApi();

            // Dev-friendly: allow any origin. Auth uses a Bearer token (not cookies), so
            // AllowAnyOrigin is safe here — tighten to specific origins before production.
            builder.Services.AddCors(options =>
            {
                options.AddDefaultPolicy(policy =>
                {
                    policy.AllowAnyOrigin()
                        .AllowAnyMethod()
                        .AllowAnyHeader();
                });
            });

            builder.Services.AddDataProtection();

            builder.Services.AddHttpContextAccessor();
            builder.Services.AddScoped<ICurrentUserService, CurrentUserService>();

            builder.Services.AddApplication();
            builder.Services.AddPersistence(builder.Configuration);
            builder.Services.AddInfrastructure(builder.Configuration);

            var jwtSettings = builder.Configuration.GetSection(JwtSettings.SectionName).Get<JwtSettings>()
                ?? throw new InvalidOperationException("Jwt settings are not configured.");

            builder.Services
                .AddAuthentication(options =>
                {
                    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
                })
                .AddJwtBearer(options =>
                {
                    options.TokenValidationParameters = new TokenValidationParameters
                    {
                        ValidateIssuer = true,
                        ValidateAudience = true,
                        ValidateLifetime = true,
                        ValidateIssuerSigningKey = true,
                        ValidIssuer = jwtSettings.Issuer,
                        ValidAudience = jwtSettings.Audience,
                        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings.Secret)),
                        ClockSkew = TimeSpan.Zero
                    };
                });

            builder.Services.AddAuthorization(options =>
            {
                // Platform admins are a separate concern from tenant-scoped RBAC (TenantMemberRole)
                // - this claim marks a user as SaaS operator staff, not a role within any tenant.
                options.AddPolicy("PlatformAdmin", policy => policy.RequireClaim("platform_admin", "true"));
            });

            builder.Services.AddHealthChecks()
                .AddDbContextCheck<AppDbContext>("database");

            var app = builder.Build();

            // Configure the HTTP request pipeline.
            if (app.Environment.IsDevelopment())
            {
                app.MapOpenApi();
            }

            app.UseMiddleware<RequestLoggingMiddleware>();
            app.UseMiddleware<ExceptionHandlingMiddleware>();

            app.UseHttpsRedirection();

            // /health/live: process is up, no dependency checks (fast, for restart probes).
            // /health: also verifies the database is reachable (for readiness/monitoring).
            app.MapHealthChecks("/health/live", new HealthCheckOptions { Predicate = _ => false });
            app.MapHealthChecks("/health");

            // Serves images re-hosted by IPublicImageHostingService (needed for Instagram's
            // image_url-based publishing API) under /media.
            app.UseStaticFiles();

            app.UseCors();

            app.UseAuthentication();
            app.UseAuthorization();

            app.MapControllers();

            app.Run();
        }
    }
}
