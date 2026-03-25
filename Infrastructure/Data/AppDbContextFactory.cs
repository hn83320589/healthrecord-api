using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace HealthRecord.API.Infrastructure.Data;

public class AppDbContextFactory : IDesignTimeDbContextFactory<AppDbContext>
{
    public AppDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<AppDbContext>();
        optionsBuilder.UseMySql(
            "Server=localhost;Port=3306;Database=healthrecord;User=root;Password=password;",
            new MySqlServerVersion(new Version(8, 0, 0)));

        return new AppDbContext(optionsBuilder.Options);
    }
}
