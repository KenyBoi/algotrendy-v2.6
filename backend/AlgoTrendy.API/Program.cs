using AlgoTrendy.API.Extensions;
using AlgoTrendy.API.Hubs;
using AlgoTrendy.API.Middleware;
using AlgoTrendy.API.Services;
using AlgoTrendy.Core.Configuration;
using AlgoTrendy.Core.Interfaces;
using AlgoTrendy.Infrastructure.Repositories;
using AlgoTrendy.Infrastructure.Services;
using AlgoTrendy.DataChannels.Channels.REST;
using AlgoTrendy.DataChannels.Services;
using AlgoTrendy.Backtesting.Engines;
using AlgoTrendy.Backtesting.Services;
using AspNetCoreRateLimit;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

// Add Azure Key Vault for secure credential management
builder.AddAzureKeyVault();

// Configure Serilog
var seqUrl = builder.Configuration["SEQ_URL"] ?? Environment.GetEnvironmentVariable("SEQ_URL") ?? "http://localhost:5341";
var seqApiKey = builder.Configuration["SEQ_API_KEY"] ?? Environment.GetEnvironmentVariable("SEQ_API_KEY");

Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .Enrich.FromLogContext()
    .Enrich.WithProperty("Application", "AlgoTrendy.API")
    .Enrich.WithProperty("Environment", builder.Environment.EnvironmentName)
    // .Enrich.WithMachineName() // Requires Serilog.Enrichers.Environment package
    .WriteTo.Console()
    .WriteTo.File(
        "logs/algotrendy-.log",
        rollingInterval: RollingInterval.Day,
        retainedFileCountLimit: 30)
    .WriteTo.Seq(
        serverUrl: seqUrl,
        apiKey: seqApiKey,
        restrictedToMinimumLevel: Serilog.Events.LogEventLevel.Information)
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

// Register ML Services
builder.Services.AddHttpClient<IMLPredictionService, MLPredictionService>();
builder.Services.AddScoped<AlgoTrendy.TradingEngine.Services.MLFeatureService>();
builder.Services.AddScoped<AlgoTrendy.TradingEngine.Services.IndicatorService>();
builder.Services.AddSingleton<AlgoTrendy.Core.Services.SymbolFormatterService>();

// Register MFA (Multi-Factor Authentication) services
builder.Services.AddSingleton<AlgoTrendy.Core.Services.TotpService>(sp =>
    new AlgoTrendy.Core.Services.TotpService(
        issuer: "AlgoTrendy",
        totpSize: 6,
        timeStepSeconds: 30,
        toleranceSteps: 1
    ));
builder.Services.AddScoped<AlgoTrendy.Core.Services.MfaService>();

// Portfolio Optimization and Risk Analytics Services
// NOTE: Temporarily commented out - missing IMarketDataProvider registration
// builder.Services.AddScoped<IPortfolioOptimizationService, AlgoTrendy.TradingEngine.Services.PortfolioOptimizationService>();
// builder.Services.AddScoped<IRiskAnalyticsService, AlgoTrendy.TradingEngine.Services.RiskAnalyticsService>();

// Register market data providers with factory functions
builder.Services.AddScoped<AlgoTrendy.DataChannels.Providers.AlphaVantageProvider>(sp =>
{
    var httpClient = sp.GetRequiredService<IHttpClientFactory>().CreateClient();
    var logger = sp.GetRequiredService<ILogger<AlgoTrendy.DataChannels.Providers.AlphaVantageProvider>>();
    var apiKey = builder.Configuration["DataProviders:AlphaVantage:ApiKey"] ?? "";
    return new AlgoTrendy.DataChannels.Providers.AlphaVantageProvider(httpClient, logger, apiKey);
});

builder.Services.AddScoped<AlgoTrendy.DataChannels.Providers.YFinanceProvider>(sp =>
{
    var httpClient = sp.GetRequiredService<IHttpClientFactory>().CreateClient();
    var logger = sp.GetRequiredService<ILogger<AlgoTrendy.DataChannels.Providers.YFinanceProvider>>();
    var serviceUrl = builder.Configuration["DataProviders:YFinance:ServiceUrl"] ?? "http://localhost:5001";
    return new AlgoTrendy.DataChannels.Providers.YFinanceProvider(httpClient, logger, serviceUrl);
});

// Register all market data channels as scoped services
builder.Services.AddScoped<BinanceRestChannel>();
builder.Services.AddScoped<OKXRestChannel>();
builder.Services.AddScoped<CoinbaseRestChannel>();
builder.Services.AddScoped<KrakenRestChannel>();

// NOTE: Temporarily commented out - depend on AlphaVantageProvider
// builder.Services.AddScoped<StockDataChannel>();
// builder.Services.AddScoped<FuturesDataChannel>();

// Register backtesting services
builder.Services.AddScoped<IBacktestEngine, CustomBacktestEngine>();
builder.Services.AddScoped<IBacktestService, BacktestService>();

// Configure and register QuantConnect services
builder.Services.Configure<AlgoTrendy.Backtesting.Services.QuantConnectConfig>(options =>
{
    options.UserId = builder.Configuration["QuantConnect:UserId"]
        ?? Environment.GetEnvironmentVariable("QUANTCONNECT_USER_ID")
        ?? "";
    options.ApiToken = builder.Configuration["QuantConnect:ApiToken"]
        ?? Environment.GetEnvironmentVariable("QUANTCONNECT_API_TOKEN")
        ?? "";
    options.BaseUrl = builder.Configuration["QuantConnect:BaseUrl"]
        ?? "https://www.quantconnect.com/api/v2";
    options.DefaultProjectId = builder.Configuration.GetValue<int?>("QuantConnect:DefaultProjectId");
    options.TimeoutSeconds = builder.Configuration.GetValue<int>("QuantConnect:TimeoutSeconds", 300);
});

builder.Services.AddHttpClient<IQuantConnectApiClient, QuantConnectApiClient>();
builder.Services.AddScoped<QuantConnectBacktestEngine>();
builder.Services.AddScoped<IMEMIntegrationService, MEMIntegrationService>();

// Configure and register Finnhub service for cryptocurrency market data
builder.Services.Configure<FinnhubSettings>(options =>
{
    options.ApiKey = builder.Configuration["FINHUB_API_KEY"]
        ?? builder.Configuration["Finnhub:ApiKey"]
        ?? Environment.GetEnvironmentVariable("FINHUB_API_KEY")
        ?? "";
    options.BaseUrl = builder.Configuration["Finnhub:BaseUrl"] ?? "https://finnhub.io/api/v1";
    options.TimeoutSeconds = builder.Configuration.GetValue<int>("Finnhub:TimeoutSeconds", 30);
    options.EnableLogging = builder.Configuration.GetValue<bool>("Finnhub:EnableLogging", false);
    options.RateLimitPerMinute = builder.Configuration.GetValue<int>("Finnhub:RateLimitPerMinute", 60);
});

builder.Services.AddHttpClient<IFinnhubService, FinnhubService>();

// Register broker services (Multi-broker support)

// Configure Binance broker options
builder.Services.Configure<AlgoTrendy.TradingEngine.Brokers.BinanceOptions>(options =>
{
    options.ApiKey = builder.Configuration["Binance__ApiKey"] ?? Environment.GetEnvironmentVariable("BINANCE_API_KEY") ?? "";
    options.ApiSecret = builder.Configuration["Binance__ApiSecret"] ?? Environment.GetEnvironmentVariable("BINANCE_API_SECRET") ?? "";
    options.UseTestnet = builder.Configuration.GetValue<bool>("Binance__UseTestnet", true);
    options.UseBinanceUS = builder.Configuration.GetValue<bool>("Binance__UseBinanceUS", false);
});

// Configure Bybit broker options
builder.Services.Configure<AlgoTrendy.TradingEngine.Brokers.BybitOptions>(options =>
{
    options.ApiKey = builder.Configuration["BYBIT_API_KEY"] ?? Environment.GetEnvironmentVariable("BYBIT_API_KEY") ?? "";
    options.ApiSecret = builder.Configuration["BYBIT_API_SECRET"] ?? Environment.GetEnvironmentVariable("BYBIT_API_SECRET") ?? "";
    options.UseTestnet = builder.Configuration.GetValue<bool>("BYBIT_TESTNET", true);
});

// Configure TradeStation broker options
builder.Services.Configure<AlgoTrendy.TradingEngine.Brokers.TradeStationOptions>(options =>
{
    options.ApiKey = builder.Configuration["TRADESTATION_API_KEY"] ?? Environment.GetEnvironmentVariable("TRADESTATION_API_KEY") ?? "";
    options.ApiSecret = builder.Configuration["TRADESTATION_API_SECRET"] ?? Environment.GetEnvironmentVariable("TRADESTATION_API_SECRET") ?? "";
    options.AccountId = builder.Configuration["TRADESTATION_ACCOUNT_ID"] ?? Environment.GetEnvironmentVariable("TRADESTATION_ACCOUNT_ID") ?? "";
    options.UsePaperTrading = builder.Configuration.GetValue<bool>("TRADESTATION_USE_PAPER", true);
});

// Configure NinjaTrader broker options
builder.Services.Configure<AlgoTrendy.TradingEngine.Brokers.NinjaTraderOptions>(options =>
{
    options.Username = builder.Configuration["NINJATRADER_USERNAME"] ?? Environment.GetEnvironmentVariable("NINJATRADER_USERNAME") ?? "";
    options.Password = builder.Configuration["NINJATRADER_PASSWORD"] ?? Environment.GetEnvironmentVariable("NINJATRADER_PASSWORD") ?? "";
    options.AccountId = builder.Configuration["NINJATRADER_ACCOUNT_ID"] ?? Environment.GetEnvironmentVariable("NINJATRADER_ACCOUNT_ID") ?? "";
    options.ConnectionType = builder.Configuration["NINJATRADER_CONNECTION_TYPE"] ?? Environment.GetEnvironmentVariable("NINJATRADER_CONNECTION_TYPE") ?? "REST";
    options.Host = builder.Configuration["NINJATRADER_HOST"] ?? Environment.GetEnvironmentVariable("NINJATRADER_HOST") ?? "localhost";
    options.Port = builder.Configuration.GetValue<int>("NINJATRADER_PORT", 36973);
});

// Configure Interactive Brokers options
builder.Services.Configure<AlgoTrendy.TradingEngine.Brokers.InteractiveBrokersOptions>(options =>
{
    options.Username = builder.Configuration["IBKR_USERNAME"] ?? Environment.GetEnvironmentVariable("IBKR_USERNAME") ?? "";
    options.Password = builder.Configuration["IBKR_PASSWORD"] ?? Environment.GetEnvironmentVariable("IBKR_PASSWORD") ?? "";
    options.AccountId = builder.Configuration["IBKR_ACCOUNT_ID"] ?? Environment.GetEnvironmentVariable("IBKR_ACCOUNT_ID") ?? "";
    options.GatewayHost = builder.Configuration["IBKR_GATEWAY_HOST"] ?? Environment.GetEnvironmentVariable("IBKR_GATEWAY_HOST") ?? "localhost";
    options.GatewayPort = builder.Configuration.GetValue<int>("IBKR_GATEWAY_PORT", 4002); // 4002 for paper, 4001 for live
    options.ClientId = builder.Configuration.GetValue<int>("IBKR_CLIENT_ID", 1);
    options.UsePaperTrading = builder.Configuration.GetValue<bool>("IBKR_USE_PAPER", true);
});

// Configure Kraken broker options (DISABLED - Package API mismatch, needs REST API implementation)
// builder.Services.Configure<AlgoTrendy.TradingEngine.Brokers.KrakenOptions>(options =>
// {
//     options.ApiKey = builder.Configuration["Kraken__ApiKey"] ?? Environment.GetEnvironmentVariable("KRAKEN_API_KEY") ?? "";
//     options.ApiSecret = builder.Configuration["Kraken__ApiSecret"] ?? Environment.GetEnvironmentVariable("KRAKEN_API_SECRET") ?? "";
// });

// Configure Coinbase broker options
builder.Services.Configure<AlgoTrendy.TradingEngine.Brokers.CoinbaseOptions>(options =>
{
    options.ApiKey = builder.Configuration["Coinbase__ApiKey"] ?? Environment.GetEnvironmentVariable("COINBASE_API_KEY") ?? "";
    options.ApiSecret = builder.Configuration["Coinbase__ApiSecret"] ?? Environment.GetEnvironmentVariable("COINBASE_API_SECRET") ?? "";
});

// Configure MEXC broker options (DISABLED - API compatibility work in progress)
// builder.Services.Configure<AlgoTrendy.TradingEngine.Brokers.MEXCOptions>(options =>
// {
//     options.ApiKey = builder.Configuration["MEXC__ApiKey"] ?? Environment.GetEnvironmentVariable("MEXC_API_KEY") ?? "";
//     options.ApiSecret = builder.Configuration["MEXC__ApiSecret"] ?? Environment.GetEnvironmentVariable("MEXC_API_SECRET") ?? "";
//     options.UseTestnet = builder.Configuration.GetValue<bool>("MEXC__UseTestnet", true);
// });

// Register all brokers as named services
builder.Services.AddScoped<AlgoTrendy.TradingEngine.Brokers.BinanceBroker>();
builder.Services.AddScoped<AlgoTrendy.TradingEngine.Brokers.BybitBroker>();
builder.Services.AddScoped<AlgoTrendy.TradingEngine.Brokers.TradeStationBroker>();
builder.Services.AddScoped<AlgoTrendy.TradingEngine.Brokers.NinjaTraderBroker>();
builder.Services.AddScoped<AlgoTrendy.TradingEngine.Brokers.InteractiveBrokersBroker>();
// builder.Services.AddScoped<AlgoTrendy.TradingEngine.Brokers.KrakenBroker>(); // DISABLED - Package API mismatch
builder.Services.AddScoped<AlgoTrendy.TradingEngine.Brokers.CoinbaseBroker>(); // ✅ ACTIVE
// builder.Services.AddScoped<AlgoTrendy.TradingEngine.Brokers.MEXCBroker>(); // 🚧 WIP - API compatibility in progress

// Register default broker (can be configured via environment variable)
builder.Services.AddScoped<IBroker>(sp =>
{
    var defaultBroker = builder.Configuration["DEFAULT_BROKER"] ?? Environment.GetEnvironmentVariable("DEFAULT_BROKER") ?? "bybit";

    return defaultBroker.ToLower() switch
    {
        "binance" => sp.GetRequiredService<AlgoTrendy.TradingEngine.Brokers.BinanceBroker>(),
        "bybit" => sp.GetRequiredService<AlgoTrendy.TradingEngine.Brokers.BybitBroker>(),
        "tradestation" => sp.GetRequiredService<AlgoTrendy.TradingEngine.Brokers.TradeStationBroker>(),
        "ninjatrader" => sp.GetRequiredService<AlgoTrendy.TradingEngine.Brokers.NinjaTraderBroker>(),
        "interactivebrokers" or "ibkr" => sp.GetRequiredService<AlgoTrendy.TradingEngine.Brokers.InteractiveBrokersBroker>(),
        // "kraken" => sp.GetRequiredService<AlgoTrendy.TradingEngine.Brokers.KrakenBroker>(), // DISABLED - Package API mismatch
        "coinbase" => sp.GetRequiredService<AlgoTrendy.TradingEngine.Brokers.CoinbaseBroker>(), // ✅ ACTIVE
        // "mexc" => sp.GetRequiredService<AlgoTrendy.TradingEngine.Brokers.MEXCBroker>(), // 🚧 WIP - API compatibility in progress
        _ => sp.GetRequiredService<AlgoTrendy.TradingEngine.Brokers.BybitBroker>() // Default to Bybit
    };
});

// Add background services
builder.Services.AddHostedService<MarketDataBroadcastService>();
builder.Services.AddHostedService<MarketDataChannelService>();
builder.Services.AddHostedService<LiquidationMonitoringService>();

// Configure CORS with strict security policy
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
    {
        // Get additional allowed origins from configuration
        var configuredOrigins = builder.Configuration["AllowedOrigins"]
            ?.Split(',', StringSplitOptions.RemoveEmptyEntries)
            ?? Array.Empty<string>();

        var allowedOrigins = new List<string>
        {
            "http://localhost:3000",  // Next.js dev server
            "http://localhost:5000",  // API dev server
            "http://localhost:5298",  // .NET API dev server
            "https://localhost:7228", // .NET API dev server HTTPS
            "http://216.238.90.131:3000",  // Legacy production frontend
            // Production domains
            "https://algotrendy.com",
            "https://www.algotrendy.com",
            "https://app.algotrendy.com",
            "https://api.algotrendy.com"
        };

        // Add any configured origins
        allowedOrigins.AddRange(configuredOrigins);

        // Strict CORS policy - only allow specific methods and headers
        policy.WithOrigins(allowedOrigins.ToArray())
            .WithMethods("GET", "POST", "PUT", "DELETE", "PATCH", "OPTIONS")
            .WithHeaders(
                "Authorization",
                "Content-Type",
                "Accept",
                "Origin",
                "X-Requested-With",
                "X-API-Key",
                "X-Correlation-ID",
                "X-ClientId")
            .AllowCredentials()
            .SetIsOriginAllowedToAllowWildcardSubdomains()
            .WithExposedHeaders("X-Correlation-ID", "X-API-Version");
    });
});

// Add health checks
builder.Services.AddHealthChecks();

// Configure Rate Limiting
builder.Services.AddMemoryCache();
builder.Services.AddCustomRateLimiting(builder.Configuration);
builder.Services.AddInMemoryRateLimiting();
builder.Services.AddSingleton<IRateLimitConfiguration, AspNetCoreRateLimit.RateLimitConfiguration>();

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

// Only use HTTPS redirection if explicitly enabled
if (builder.Configuration.GetValue<bool>("EnableHttpsRedirection", false))
{
    app.UseHttpsRedirection();
}

// Add security headers (must be early in pipeline)
app.UseSecurityHeaders();

app.UseCors("AllowFrontend");

// Add correlation ID middleware for request tracing
app.UseCorrelationId();

// Add JWT authentication middleware (before authorization)
app.UseJwtAuthentication();

// Add request logging with Serilog
app.UseSerilogRequestLogging(options =>
{
    options.EnrichDiagnosticContext = (diagnosticContext, httpContext) =>
    {
        diagnosticContext.Set("RequestHost", httpContext.Request.Host.Value);
        diagnosticContext.Set("RequestScheme", httpContext.Request.Scheme);
        diagnosticContext.Set("RemoteIP", httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown");
    };
});

// Add rate limiting middleware (must be before authorization)
app.UseIpRateLimiting();
app.UseClientRateLimiting();
app.UseRateLimitHeaders();

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
