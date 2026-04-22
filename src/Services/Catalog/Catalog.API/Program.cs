using Catalog.API.Data;
using Catalog.API.Repositories;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

// Serilog
Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .Enrich.FromLogContext()
    .WriteTo.Console()
    .WriteTo.Seq("http://localhost:5341")
    .CreateLogger();

builder.Host.UseSerilog();

// MongoDB Context
builder.Services.AddSingleton<ICatalogContext, CatalogContext>();

// Repositories
builder.Services.AddScoped<ICatalogRepository, CatalogRepository>();

// MediatR (CQRS)
builder.Services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(typeof(Program).Assembly));

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

// Serve swagger JSON at /swagger/catalog/v1/swagger.json so it works through the YARP gateway
app.UseSwagger(c => c.RouteTemplate = "swagger/catalog/{documentName}/swagger.json");
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("v1/swagger.json", "Catalog API v1");
    c.RoutePrefix = "swagger/catalog";
});

app.UseCors();
app.UseSerilogRequestLogging();
app.MapControllers();

app.Run();
