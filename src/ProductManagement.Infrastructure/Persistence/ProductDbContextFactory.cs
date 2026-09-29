using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace ProductManagement.Infrastructure.Persistence;

public sealed class ProductDbContextFactory : IDesignTimeDbContextFactory<ProductDbContext>
{
    public ProductDbContext CreateDbContext(string[] args)
    {
        var currentDirectory = Directory.GetCurrentDirectory();
        var settingsPaths = new[]
        {
            Path.Combine(currentDirectory, "src", "ProductManagement.API"),
            Path.Combine(currentDirectory, "..", "ProductManagement.API"),
            currentDirectory
        };
        var settingsPath = settingsPaths.FirstOrDefault(path => File.Exists(Path.Combine(path, "appsettings.json")));
        var configurationBuilder = new ConfigurationBuilder()
            .AddEnvironmentVariables();

        if (settingsPath is not null)
        {
            configurationBuilder
                .SetBasePath(settingsPath)
                .AddJsonFile("appsettings.json", optional: false)
                .AddJsonFile("appsettings.Development.json", optional: true);
        }

        var configuration = configurationBuilder
            .Build();

        var options = new DbContextOptionsBuilder<ProductDbContext>()
            .UseSqlServer(configuration.GetConnectionString("ProductDatabase"))
            .Options;

        return new ProductDbContext(options);
    }
}
