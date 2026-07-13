using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace Rawaj.Persistence;

public class AppDbContextFactory : IDesignTimeDbContextFactory<AppDbContext>
{
    private readonly IConfiguration _configuration;
    public AppDbContextFactory() : this(BuildConfiguration()) { }
    public AppDbContextFactory(IConfiguration configuration) => _configuration = configuration;
    public AppDbContext CreateDbContext(string[] args)
    {
        var connectionString = _configuration.GetConnectionString("DefaultConnection");

        var optionsBuilder = new DbContextOptionsBuilder<AppDbContext>();
        optionsBuilder.UseSqlServer(connectionString);

        return new AppDbContext(optionsBuilder.Options);
    }
    private static IConfiguration BuildConfiguration()
    {
        var basePath = Path.Combine(Directory.GetCurrentDirectory(), "..", "Rawaj");
        var environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Development";
        return new ConfigurationBuilder()
            .SetBasePath(basePath)
            .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
            .AddJsonFile($"appsettings.{environment}.json", optional: true, reloadOnChange: true)
            .Build();
    }
}
