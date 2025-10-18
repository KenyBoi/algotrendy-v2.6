using AlgoTrendy.API.Hubs;
using AlgoTrendy.API.Services;
using AlgoTrendy.Core.Interfaces;
using AlgoTrendy.Infrastructure.Repositories;
using AlgoTrendy.DataChannels.Channels.REST;
using AlgoTrendy.DataChannels.Services;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

// Configure Serilog
Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .Enrich.FromLogContext()
    .WriteTo.Console()
    .WriteTo.File(
        "logs/algotrendy-.log",
        rollingInterval: RollingInterval.Day,
        retainedFileCountLimit: 30)
    .CreateLogger();

builder.Host.UseSerilog();

// Add services to the container
builder.Services.AddControllers();

// Add SignalR
builder.Services.AddSignalR(options =>
{
    options.EnableDetailedErrors = builder.Environment.IsDevelopment();
    options.KeepAliveInterval = TimeSpan.FromSeconds(15);
    options.ClientTimeoutInterval = TimeSpan.FromSeconds(30);
});

// Configure Swagger/OpenAPI
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo
    {
        Title = "AlgoTrendy API",
        Version = "v2.6.0",
        Description = "High-performance cryptocurrency trading platform API",
        Contact = new Microsoft.OpenApi.Models.OpenApiContact
        {
            Name = "AlgoTrendy Development Team",
            Email = "dev@algotrendy.com"
        }
    });

    // Include XML comments
    var xmlFile = $"{System.Reflection.Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
    if (File.Exists(xmlPath))
    {
        options.IncludeXmlComments(xmlPath);
    }
});

// Register repositories and services
var questDbConnectionString = builder.Configuration.GetConnectionString("QuestDB")
    ?? "Host=localhost;Port=8812;Database=qdb;Username=admin;Password=quest";

builder.Services.AddScoped<IMarketDataRepository>(sp =>
    new MarketDataRepository(questDbConnectionString));

// Add HttpClient factory for REST channels
builder.Services.AddHttpClient();

// Register all market data channels as scoped services
builder.Services.AddScoped<BinanceRestChannel>();
builder.Services.AddScoped<OKXRestChannel>();
builder.Services.AddScoped<CoinbaseRestChannel>();
builder.Services.AddScoped<KrakenRestChannel>();

// Add background services
builder.Services.AddHostedService<MarketDataBroadcastService>();
builder.Services.AddHostedService<MarketDataChannelService>();

// Configure CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
    {
        policy.WithOrigins(
                "http://localhost:3000",  // Next.js dev server
                "http://localhost:5000",  // API dev server
                "http://216.238.90.131:3000"  // Production frontend
            )
            .AllowAnyMethod()
            .AllowAnyHeader()
            .AllowCredentials();
    });
});

// Add health checks
builder.Services.AddHealthChecks();

var app = builder.Build();

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/swagger/v1/swagger.json", "AlgoTrendy API v2.6");
        options.RoutePrefix = string.Empty;  // Serve Swagger UI at root
    });
}

app.UseHttpsRedirection();

app.UseCors("AllowFrontend");

app.UseAuthorization();

app.MapControllers();

app.MapHub<MarketDataHub>("/hubs/marketdata");

app.MapHealthChecks("/health");

// Log application startup
Log.Information("AlgoTrendy API v2.6 starting...");
Log.Information("Environment: {Environment}", app.Environment.EnvironmentName);
Log.Information("QuestDB Connection: {ConnectionString}", questDbConnectionString.Replace("Password=quest", "Password=***"));

try
{
    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Application terminated unexpectedly");
}
finally
{
    Log.CloseAndFlush();
}

// Make Program class accessible for integration tests
public partial class Program { }
