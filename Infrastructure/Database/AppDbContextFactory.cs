using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace OptimusPrime.Infrastructure.Database;

public class AppDbContextFactory : IDesignTimeDbContextFactory<AppDbcontext> {

    public AppDbcontext CreateDbContext(string[] args) {

        // ========================================================================================================
        // Provides EF with the coordinates of the local database to generate the migration files
        // ========================================================================================================

        // configuration reader
        IConfigurationRoot configuration = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
            .AddJsonFile("appsettings.Development.json", optional: true)
            .Build();

        var connectionString = configuration.GetConnectionString("PostgresConnection") ?? throw new Exception("Postgres connection string not found");

        var optionsBuilder = new DbContextOptionsBuilder<AppDbcontext>();
        optionsBuilder.UseNpgsql(connectionString);

        return new AppDbcontext(optionsBuilder.Options);
    }
} 