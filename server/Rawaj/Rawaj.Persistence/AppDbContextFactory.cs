using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Rawaj.Persistence;

public class AppDbContextFactory : IDesignTimeDbContextFactory<AppDbContext>
{
    public AppDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<AppDbContext>();
        optionsBuilder.UseSqlServer(
            "Server=LAPTOP-7VP1SNSU\\SQLEXPRESS;Database=RawajDB;Integrated security=true;TrustServerCertificate=True");

        return new AppDbContext(optionsBuilder.Options);
    }
}
