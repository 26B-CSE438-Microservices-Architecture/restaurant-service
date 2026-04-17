using Microsoft.EntityFrameworkCore;
using RestaurantService.API.Data;
using RestaurantService.API.Services;
using MassTransit;
using RestaurantService.API.Consumers;

var builder = WebApplication.CreateBuilder(args);

// ── Database ──
if (builder.Environment.EnvironmentName != "Testing")
{
    builder.Services.AddDbContext<AppDbContext>(options =>
        options.UseNpgsql(
            builder.Configuration.GetConnectionString("DefaultConnection"),
            o => o.UseNetTopologySuite()
        ));
}

// ── Services (DI) ──
builder.Services.AddScoped<IRestaurantService, RestaurantServiceImpl>();
builder.Services.AddScoped<IMenuService, MenuServiceImpl>();

// ── Controllers ──
builder.Services.AddControllers();

// ── Swagger ──
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new()
    {
        Title = "Restaurant Service API",
        Version = "v1",
        Description = "Trendyol GO Clone — Restaurant microservice for managing restaurants, menus, and products."
    });
});

// ── CORS ──
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

// ── MassTransit & RabbitMQ ──
builder.Services.AddMassTransit(x =>
{
    x.AddConsumer<OrderCompletedEventConsumer>();
    x.AddConsumer<UserRoleChangedEventConsumer>();

    x.UsingRabbitMq((context, cfg) =>
    {
        cfg.Host(builder.Configuration["RabbitMQ:Host"], h =>
        {
            h.Username(builder.Configuration["RabbitMQ:Username"] ?? "guest");
            h.Password(builder.Configuration["RabbitMQ:Password"] ?? "guest");
        });
        
        cfg.ConfigureEndpoints(context);
    });
});

var app = builder.Build();

// ── Auto-migrate on startup (skip in Testing — InMemory DB kullanılır) ──
if (!app.Environment.IsEnvironment("Testing"))
{
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.Migrate();
}

// ── Middleware ──
app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "Restaurant Service API v1");
    c.RoutePrefix = "swagger";
});

app.UseCors();
app.MapControllers();

app.Run();

// Integration testleri için WebApplicationFactory erişimi
public partial class Program { }
