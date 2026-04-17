using MassTransit;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using RestaurantService.API.Data;

namespace RestaurantService.IntegrationTests;

/// <summary>
/// Test ortamı için WebApplicationFactory.
/// Environment "Testing" olarak ayarlanır → Program.cs'de PostgreSQL kaydedilmez.
/// Burada InMemory DB ve MassTransit Test Harness eklenir.
/// </summary>
public class CustomWebApplicationFactory : WebApplicationFactory<Program>
{
    private readonly string _dbName = "IntegrationTestDb_" + Guid.NewGuid();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");

        builder.ConfigureServices(services =>
        {
            // ── InMemory DB ekle ──
            services.AddDbContext<AppDbContext>(options =>
            {
                options.UseInMemoryDatabase(_dbName);
            });

            // ── MassTransit / RabbitMQ → InMemory Test Harness ──
            var massTransitDescriptors = services
                .Where(d => d.ServiceType.FullName != null &&
                           (d.ServiceType.FullName.Contains("MassTransit") ||
                            d.ServiceType.FullName.Contains("IBus") ||
                            d.ServiceType.FullName.Contains("IPublishEndpoint") ||
                            d.ServiceType.FullName.Contains("ISendEndpointProvider")))
                .ToList();

            foreach (var d in massTransitDescriptors)
                services.Remove(d);

            services.AddMassTransitTestHarness();
        });
    }
}

/// <summary>
/// RestaurantApiTests için izole factory.
/// </summary>
public class RestaurantApiFactory : CustomWebApplicationFactory { }

/// <summary>
/// MenuApiTests için izole factory.
/// </summary>
public class MenuApiFactory : CustomWebApplicationFactory { }
