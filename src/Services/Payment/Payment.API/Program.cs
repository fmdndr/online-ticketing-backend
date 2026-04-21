using MassTransit;
using Microsoft.EntityFrameworkCore;
using Payment.API.Consumers;
using Payment.API.Data;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

// Serilog - read entirely from config (no hardcoded localhost)
Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .Enrich.FromLogContext()
    .CreateLogger();

builder.Host.UseSerilog();

// When running inside a Docker container (Rider dev runner sets DOTNET_RUNNING_IN_CONTAINER=true),
// replace 'localhost' with 'host.docker.internal' so the container can reach host-mapped ports.
// docker-compose overrides ConnectionStrings__PaymentDb via environment: section, so this only
// activates when no explicit override is present.
var connectionString = builder.Configuration.GetConnectionString("PaymentDb") ?? string.Empty;
if (Environment.GetEnvironmentVariable("DOTNET_RUNNING_IN_CONTAINER") == "true")
{
    connectionString = connectionString
        .Replace("Host=localhost", "Host=host.docker.internal")
        .Replace("Server=localhost", "Server=host.docker.internal");
}

// PostgreSQL + EF Core
builder.Services.AddDbContext<PaymentDbContext>(options =>
    options.UseNpgsql(connectionString));

// MassTransit + RabbitMQ
var isInContainer = Environment.GetEnvironmentVariable("DOTNET_RUNNING_IN_CONTAINER") == "true";
builder.Services.AddMassTransit(x =>
{
    x.AddConsumer<TicketReservedEventConsumer>();

    x.UsingRabbitMq((context, cfg) =>
    {
        var rabbitHost = builder.Configuration.GetValue<string>("RabbitMQ:Host") ?? "localhost";
        if (isInContainer && rabbitHost == "localhost")
            rabbitHost = "host.docker.internal";

        cfg.Host(rabbitHost, "/", h =>
        {
            h.Username(builder.Configuration.GetValue<string>("RabbitMQ:Username") ?? "guest");
            h.Password(builder.Configuration.GetValue<string>("RabbitMQ:Password") ?? "guest");
        });

        cfg.ConfigureEndpoints(context);
    });
});

// Controllers
builder.Services.AddControllers();

// Swagger
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// CORS
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod();
    });
});

var app = builder.Build();

// Auto-migrate database
using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<PaymentDbContext>();
    await dbContext.Database.MigrateAsync();
}

app.UseSwagger();
app.UseSwaggerUI();

app.UseCors();
app.UseSerilogRequestLogging();
app.MapControllers();

app.Run();
