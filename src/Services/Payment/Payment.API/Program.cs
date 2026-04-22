using MassTransit;
using Microsoft.EntityFrameworkCore;
using Payment.API.Consumers;
using Payment.API.Data;
using Serilog;
using Shared.Common.Events;

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
var isInContainer = Environment.GetEnvironmentVariable("DOTNET_RUNNING_IN_CONTAINER") == "true";
var connectionString = builder.Configuration.GetConnectionString("PaymentDb") ?? string.Empty;
if (isInContainer)
{
    connectionString = connectionString
        .Replace("Host=localhost", "Host=host.docker.internal")
        .Replace("Server=localhost", "Server=host.docker.internal");
}

// PostgreSQL + EF Core
builder.Services.AddDbContext<PaymentDbContext>(options =>
    options.UseNpgsql(connectionString));

// MassTransit + Kafka
builder.Services.AddMassTransit(x =>
{
    x.UsingInMemory();

    x.AddRider(rider =>
    {
        rider.AddConsumer<TicketReservedEventConsumer>();

        rider.AddProducer<PaymentCompletedEvent>("payment-completed");
        rider.AddProducer<PaymentFailedEvent>("payment-failed");

        rider.UsingKafka((context, k) =>
        {
            var kafkaHost = builder.Configuration.GetValue<string>("Kafka:BootstrapServers") ?? "localhost:9092";
            if (isInContainer && kafkaHost.StartsWith("localhost"))
                kafkaHost = kafkaHost.Replace("localhost", "host.docker.internal");

            k.Host(kafkaHost);

            k.TopicEndpoint<TicketReservedEvent>("ticket-reserved", "payment-service", e =>
            {
                e.ConfigureConsumer<TicketReservedEventConsumer>(context);
            });
        });
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

// Serve swagger JSON at /swagger/payment/v1/swagger.json so it works through the YARP gateway
app.UseSwagger(c => c.RouteTemplate = "swagger/payment/{documentName}/swagger.json");
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("v1/swagger.json", "Payment API v1");
    c.RoutePrefix = "swagger/payment";
});

app.UseCors();
app.UseSerilogRequestLogging();
app.MapControllers();

app.Run();
