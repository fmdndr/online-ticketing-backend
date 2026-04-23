using Microsoft.EntityFrameworkCore;
using Payment.API.Consumers;
using Payment.API.Data;
using Payment.API.Kafka;
using Serilog;
using Shared.Common.Auth;

var builder = WebApplication.CreateBuilder(args);

Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .Enrich.FromLogContext()
    .CreateLogger();

builder.Host.UseSerilog();

var isInContainer = Environment.GetEnvironmentVariable("DOTNET_RUNNING_IN_CONTAINER") == "true";
var connectionString = builder.Configuration.GetConnectionString("PaymentDb") ?? string.Empty;
if (isInContainer)
{
    connectionString = connectionString
        .Replace("Host=localhost", "Host=host.docker.internal")
        .Replace("Server=localhost", "Server=host.docker.internal");
}

builder.Services.AddDbContext<PaymentDbContext>(options =>
    options.UseNpgsql(connectionString));

builder.Services.AddSingleton<IKafkaProducer, KafkaProducer>();

builder.Services.AddHostedService<TicketReservedEventConsumer>();

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

using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<PaymentDbContext>();
    await dbContext.Database.MigrateAsync();
}

app.UseSwagger(c => c.RouteTemplate = "swagger/payment/{documentName}/swagger.json");
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("v1/swagger.json", "Payment API v1");
    c.RoutePrefix = "swagger/payment";
});

app.UseCors();
app.UseSerilogRequestLogging();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

app.Run();
