using Microsoft.AspNetCore.HttpOverrides;
using Serilog;
using Shared.Common.Auth;

var builder = WebApplication.CreateBuilder(args);

var seqUrl = builder.Configuration.GetValue<string>("Serilog:WriteTo:1:Args:serverUrl")
             ?? "http://localhost:5341";

Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .Enrich.FromLogContext()
    .WriteTo.Console()
    .WriteTo.Seq(seqUrl)
    .CreateLogger();

builder.Host.UseSerilog();

builder.Services.AddReverseProxy()
    .LoadFromConfig(builder.Configuration.GetSection("ReverseProxy"));

builder.Services.AddTicketingJwtAuth(builder.Configuration);

builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
    options.KnownNetworks.Clear();
    options.KnownProxies.Clear();
});

var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>()
    ?? new[] { "http://localhost:5173", "http://localhost:3000" };

builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.WithOrigins(allowedOrigins)
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});

var app = builder.Build();

app.UseForwardedHeaders();
app.UseCors();
app.UseSerilogRequestLogging();

app.UseAuthentication();
app.UseAuthorization();

app.MapReverseProxy();

app.MapGet("/", () => "Event Ticketing Gateway is running");

app.Run();
