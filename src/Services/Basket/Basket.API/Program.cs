using Basket.API.Consumers;
using Basket.API.Repositories;
using MassTransit;
using Serilog;
using Shared.Common.Events;
using StackExchange.Redis;

var builder = WebApplication.CreateBuilder(args);

// Serilog — read entirely from config so Production uses "http://seq:5341"
// and Development falls back to "http://localhost:5341"
Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .Enrich.FromLogContext()
    .CreateLogger();

builder.Host.UseSerilog();

// Redis
builder.Services.AddSingleton<IConnectionMultiplexer>(sp =>
{
    var connectionString = builder.Configuration.GetValue<string>("Redis:ConnectionString") ?? "localhost:6379";
    return ConnectionMultiplexer.Connect(connectionString);
});

// Repository
builder.Services.AddScoped<IBasketRepository, BasketRepository>();

// MassTransit + Kafka
builder.Services.AddMassTransit(x =>
{
    x.UsingInMemory();

    x.AddRider(rider =>
    {
        rider.AddConsumer<PaymentCompletedEventConsumer>();
        rider.AddConsumer<PaymentFailedEventConsumer>();

        rider.AddProducer<TicketReservedEvent>("ticket-reserved");

        rider.UsingKafka((context, k) =>
        {
            k.Host(builder.Configuration.GetValue<string>("Kafka:BootstrapServers") ?? "localhost:9092");

            k.TopicEndpoint<PaymentCompletedEvent>("payment-completed", "basket-service", e =>
            {
                e.ConfigureConsumer<PaymentCompletedEventConsumer>(context);
            });

            k.TopicEndpoint<PaymentFailedEvent>("payment-failed", "basket-service", e =>
            {
                e.ConfigureConsumer<PaymentFailedEventConsumer>(context);
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

// Serve swagger JSON at /swagger/basket/v1/swagger.json so it works through the YARP gateway
app.UseSwagger(c => c.RouteTemplate = "swagger/basket/{documentName}/swagger.json");
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("v1/swagger.json", "Basket API v1");
    c.RoutePrefix = "swagger/basket";
});

app.UseCors();
app.UseSerilogRequestLogging();
app.MapControllers();

app.Run();
