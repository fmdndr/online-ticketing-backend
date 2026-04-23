using Basket.API.Consumers;
using Basket.API.Kafka;
using Basket.API.Repositories;
using Serilog;
using Shared.Common.Auth;
using StackExchange.Redis;

var builder = WebApplication.CreateBuilder(args);

Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .Enrich.FromLogContext()
    .CreateLogger();

builder.Host.UseSerilog();

builder.Services.AddSingleton<IConnectionMultiplexer>(sp =>
{
    var connectionString = builder.Configuration.GetValue<string>("Redis:ConnectionString") ?? "localhost:6379";
    return ConnectionMultiplexer.Connect(connectionString);
});

builder.Services.AddScoped<IBasketRepository, BasketRepository>();

builder.Services.AddSingleton<IKafkaProducer, KafkaProducer>();

builder.Services.AddHostedService<PaymentCompletedEventConsumer>();
builder.Services.AddHostedService<PaymentFailedEventConsumer>();

builder.Services.AddTicketingJwtAuth(builder.Configuration);

builder.Services.AddControllers();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod();
    });
});

var app = builder.Build();

app.UseSwagger(c => c.RouteTemplate = "swagger/basket/{documentName}/swagger.json");
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("v1/swagger.json", "Basket API v1");
    c.RoutePrefix = "swagger/basket";
});

app.UseCors();
app.UseSerilogRequestLogging();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

app.Run();
