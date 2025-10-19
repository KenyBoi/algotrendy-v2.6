using AlgoTrendy.API.Extensions;
using AlgoTrendy.API.Hubs;
using AlgoTrendy.API.Services;
using AlgoTrendy.Core.Interfaces;
using AlgoTrendy.Infrastructure.Repositories;
using AlgoTrendy.DataChannels.Channels.REST;
using AlgoTrendy.DataChannels.Services;
using AlgoTrendy.Backtesting.Engines;
using AlgoTrendy.Backtesting.Services;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

// Add Azure Key Vault for secure credential management
builder.AddAzureKeyVault();

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
        Description = @"High-performance cryptocurrency trading platform API with support for:
- Multi-exchange order execution (Binance, OKX, Coinbase, Kraken)
- Real-time market data via WebSocket and REST
- Idempotent order submission with client-side order IDs
- Comprehensive risk management and validation
- Secure credential management with Azure Key Vault

## Order Idempotency
All order submissions support idempotency through the `ClientOrderId` field. If a network retry occurs with the same `ClientOrderId`, the API will return the original order instead of creating a duplicate.

## Rate Limits
- Market Data: 1200 requests/minute per IP
- Trading Operations: 600 requests/minute per API key
- Batch Operations: 100 requests/minute per API key

## Error Handling
The API uses standard HTTP status codes:
- 200: Success
- 400: Bad Request (validation errors)
- 404: Resource not found
- 429: Rate limit exceeded
- 500: Internal server error",
        Contact = new Microsoft.OpenApi.Models.OpenApiContact
        {
            Name = "AlgoTrendy Development Team",
            Email = "dev@algotrendy.com"
        },
        License = new Microsoft.OpenApi.Models.OpenApiLicense
        {
            Name = "Proprietary",
            Url = new Uri("https://algotrendy.com/license")
        }
    });

    // Include XML comments from the API project
    var xmlFile = $"{System.Reflection.Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
    if (File.Exists(xmlPath))
    {
        options.IncludeXmlComments(xmlPath, includeControllerXmlComments: true);
    }

    // Include XML comments from Core models
    var coreXmlFile = "AlgoTrendy.Core.xml";
    var coreXmlPath = Path.Combine(AppContext.BaseDirectory, coreXmlFile);
    if (File.Exists(coreXmlPath))
    {
        options.IncludeXmlComments(coreXmlPath);
    }

    // Add API key authentication (for future implementation)
    options.AddSecurityDefinition("ApiKey", new Microsoft.OpenApi.Models.OpenApiSecurityScheme
    {
        Type = Microsoft.OpenApi.Models.SecuritySchemeType.ApiKey,
        In = Microsoft.OpenApi.Models.ParameterLocation.Header,
        Name = "X-API-Key",
        Description = "API Key authentication. Obtain your API key from the AlgoTrendy dashboard."
    });

    // Enable annotations for better schema documentation
    // Note: Requires Swashbuckle.AspNetCore.Annotations package
    // options.EnableAnnotations();

    // Custom schema IDs to avoid conflicts
    options.CustomSchemaIds(type => type.FullName);

    // Add operation filters for additional documentation
    options.OperationFilter<AlgoTrendy.API.Swagger.SwaggerDefaultValues>();

    // Add schema filters for examples and custom schema definitions
    options.SchemaFilter<AlgoTrendy.API.Swagger.SwaggerSchemaExamples>();

    // Order endpoints by path
    options.OrderActionsBy(apiDesc => apiDesc.RelativePath);
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

// Register backtesting services
builder.Services.AddScoped<IBacktestEngine, CustomBacktestEngine>();
builder.Services.AddScoped<IBacktestService, BacktestService>();

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
