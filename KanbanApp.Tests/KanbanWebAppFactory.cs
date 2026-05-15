using KanbanApp.Backend.Data;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace KanbanApp.Tests;

public class KanbanWebAppFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureAppConfiguration(config =>
        {
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["RateLimiting:Disabled"] = "true",
                ["Jwt:Key"] = "TestJwtKeyForKanbanAppTests1234567890",
                ["Jwt:Issuer"] = "http://localhost",
                ["Jwt:Audience"] = "http://localhost"
            });
        });

        builder.ConfigureLogging(logging => logging.ClearProviders());

        builder.ConfigureServices(services =>
        {
            var toRemove = services
                .Where(d => d.ServiceType.FullName != null &&
                            d.ServiceType.FullName.Contains("DbContext"))
                .ToList();

            foreach (var d in toRemove)
                services.Remove(d);

            var dbName = Guid.NewGuid().ToString();
            services.AddDbContext<ApplicationDbContext>(options =>
                options.UseInMemoryDatabase(dbName));

            var rateLimiter = services.FirstOrDefault(d =>
                d.ServiceType.FullName != null &&
                d.ServiceType.FullName.Contains("RateLimiter"));

            if (rateLimiter != null)
                services.Remove(rateLimiter);
        });
    }
}
