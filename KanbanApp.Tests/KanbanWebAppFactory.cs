using KanbanApp.Backend.Data;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;

namespace KanbanApp.Tests;

public class KanbanWebAppFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
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

        builder.UseSetting("RateLimiting:Disabled", "true");
    }
}