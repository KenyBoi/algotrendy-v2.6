# Swagger/OpenAPI Documentation Enhancement Guide

This guide shows how to enhance the Swagger/OpenAPI documentation in AlgoTrendy with better examples, descriptions, and schemas.

## Current Setup

The API uses Swashbuckle.AspNetCore for automatic OpenAPI/Swagger documentation generation. The Swagger UI is available at:

- Development: `http://localhost:5002/swagger`
- Production: `https://api.algotrendy.com/swagger`

## Enhancement Checklist

- [x] Basic Swagger setup
- [ ] Add XML documentation comments
- [ ] Add request/response examples
- [ ] Document authentication
- [ ] Add error response schemas
- [ ] Include rate limiting information
- [ ] Add operation tags and grouping
- [ ] Custom schema examples

---

## 1. XML Documentation Comments

### Enable XML Documentation

Update `AlgoTrendy.API.csproj`:

```xml
<PropertyGroup>
  <GenerateDocumentationFile>true</GenerateDocumentationFile>
  <NoWarn>$(NoWarn);1591</NoWarn> <!-- Suppress missing XML comment warnings -->
</PropertyGroup>
```

### Add Comprehensive XML Comments

**Before:**
```csharp
[HttpGet("{symbol}")]
public async Task<ActionResult<MarketData>> GetLatest(string symbol)
{
    var data = await _marketDataService.GetLatestAsync(symbol);
    return Ok(data);
}
```

**After:**
```csharp
/// <summary>
/// Gets the latest market data for a specific symbol
/// </summary>
/// <param name="symbol">The trading symbol (e.g., "BTCUSDT", "ETHUSDT")</param>
/// <returns>Latest market data including price, volume, and timestamp</returns>
/// <response code="200">Returns the latest market data</response>
/// <response code="404">Symbol not found</response>
/// <response code="429">Rate limit exceeded</response>
/// <remarks>
/// Sample request:
///
///     GET /api/MarketData/latest/BTCUSDT
///
/// This endpoint returns real-time market data aggregated from multiple exchanges.
/// Data is cached for 1 second to optimize performance.
/// </remarks>
[HttpGet("latest/{symbol}")]
[ProducesResponseType(typeof(MarketDataResponse), StatusCodes.Status200OK)]
[ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
[ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status429TooManyRequests)]
public async Task<ActionResult<MarketDataResponse>> GetLatest(string symbol)
{
    var data = await _marketDataService.GetLatestAsync(symbol);
    return Ok(data);
}
```

---

## 2. Request/Response Examples

### Using Swashbuckle Annotations

Install package (if not already installed):
```bash
dotnet add package Swashbuckle.AspNetCore.Annotations
```

Enable in `Program.cs`:
```csharp
builder.Services.AddSwaggerGen(options =>
{
    options.EnableAnnotations();

    // Include XML comments
    var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
    options.IncludeXmlComments(xmlPath);
});
```

### Add Examples to Models

```csharp
using Swashbuckle.AspNetCore.Annotations;

/// <summary>
/// Market order request
/// </summary>
public class MarketOrderRequest
{
    /// <summary>
    /// Trading symbol (e.g., "BTCUSDT")
    /// </summary>
    /// <example>BTCUSDT</example>
    [Required]
    public string Symbol { get; set; }

    /// <summary>
    /// Order side (Buy or Sell)
    /// </summary>
    /// <example>Buy</example>
    [Required]
    public OrderSide Side { get; set; }

    /// <summary>
    /// Order quantity in base currency
    /// </summary>
    /// <example>0.001</example>
    [Required]
    [Range(0.00000001, double.MaxValue)]
    public decimal Quantity { get; set; }

    /// <summary>
    /// Broker ID (bybit, binance, etc.)
    /// </summary>
    /// <example>bybit</example>
    public string BrokerId { get; set; } = "bybit";
}
```

### Add Swagger Operation Examples

```csharp
[HttpPost("market")]
[SwaggerOperation(
    Summary = "Place a market order",
    Description = "Executes an immediate market order at the best available price",
    OperationId = "PlaceMarketOrder",
    Tags = new[] { "Orders" }
)]
[SwaggerResponse(200, "Order placed successfully", typeof(OrderResponse))]
[SwaggerResponse(400, "Invalid request", typeof(ErrorResponse))]
[SwaggerResponse(401, "Unauthorized")]
[SwaggerResponse(429, "Rate limit exceeded")]
public async Task<ActionResult<OrderResponse>> PlaceMarketOrder(
    [FromBody, SwaggerRequestBody("Market order details", Required = true)]
    MarketOrderRequest request)
{
    var result = await _orderService.PlaceMarketOrderAsync(request);
    return Ok(result);
}
```

---

## 3. Document Authentication

### Add Security Definitions

Update `Program.cs`:

```csharp
builder.Services.AddSwaggerGen(options =>
{
    // ... existing config

    // Define Bearer authentication
    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = @"JWT Authorization header using the Bearer scheme.
                      Enter 'Bearer' [space] and then your token in the text input below.
                      Example: 'Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...'",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.ApiKey,
        Scheme = "Bearer",
        BearerFormat = "JWT"
    });

    options.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
});
```

### Mark Endpoints as Authorized

```csharp
/// <summary>
/// Gets portfolio summary
/// </summary>
/// <remarks>
/// Requires authentication. Include JWT token in Authorization header.
/// </remarks>
[HttpGet("summary")]
[Authorize]
[SwaggerOperation(Summary = "Get portfolio summary")]
[SwaggerResponse(200, "Portfolio summary retrieved", typeof(PortfolioSummary))]
[SwaggerResponse(401, "Unauthorized - Invalid or missing token")]
public async Task<ActionResult<PortfolioSummary>> GetSummary()
{
    // ...
}
```

---

## 4. Error Response Schemas

### Create Standard Error Models

```csharp
/// <summary>
/// Standard error response
/// </summary>
public class ErrorResponse
{
    /// <summary>
    /// Error code
    /// </summary>
    /// <example>INVALID_SYMBOL</example>
    public string Code { get; set; }

    /// <summary>
    /// Human-readable error message
    /// </summary>
    /// <example>The specified symbol 'INVALID' was not found</example>
    public string Message { get; set; }

    /// <summary>
    /// Additional error details
    /// </summary>
    public Dictionary<string, object> Details { get; set; }

    /// <summary>
    /// Timestamp of the error
    /// </summary>
    /// <example>2025-10-21T12:00:00Z</example>
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// Validation error response
/// </summary>
public class ValidationErrorResponse : ErrorResponse
{
    /// <summary>
    /// Field-specific validation errors
    /// </summary>
    public Dictionary<string, string[]> Errors { get; set; }
}
```

### Document Error Responses

```csharp
[HttpPost]
[SwaggerResponse(200, "Success", typeof(OrderResponse))]
[SwaggerResponse(400, "Validation failed", typeof(ValidationErrorResponse),
    Example = @"{
        ""code"": ""VALIDATION_ERROR"",
        ""message"": ""One or more validation errors occurred"",
        ""errors"": {
            ""quantity"": [""Quantity must be greater than 0""]
        }
    }")]
[SwaggerResponse(404, "Not found", typeof(ErrorResponse),
    Example = @"{
        ""code"": ""SYMBOL_NOT_FOUND"",
        ""message"": ""The specified symbol was not found""
    }")]
public async Task<ActionResult<OrderResponse>> PlaceOrder(OrderRequest request)
{
    // ...
}
```

---

## 5. Rate Limiting Information

### Add Rate Limit Headers Documentation

```csharp
/// <summary>
/// Gets market data
/// </summary>
/// <remarks>
/// **Rate Limiting:**
/// - Unauthenticated: 100 requests/minute
/// - Authenticated: 1000 requests/minute
///
/// Rate limit headers are included in the response:
/// - `X-RateLimit-Limit`: Maximum requests allowed
/// - `X-RateLimit-Remaining`: Remaining requests in current window
/// - `X-RateLimit-Reset`: Unix timestamp when the limit resets
///
/// If rate limited, returns 429 status with `Retry-After` header.
/// </remarks>
[HttpGet]
[SwaggerResponse(429, "Rate limit exceeded", typeof(ErrorResponse),
    Example = @"{
        ""code"": ""RATE_LIMIT_EXCEEDED"",
        ""message"": ""Rate limit exceeded. Please try again in 60 seconds"",
        ""details"": {
            ""retryAfter"": 60,
            ""limit"": 100,
            ""remaining"": 0
        }
    }")]
public async Task<ActionResult<MarketData>> GetMarketData()
{
    // ...
}
```

---

## 6. Operation Tags and Grouping

### Organize Endpoints by Feature

```csharp
[ApiController]
[Route("api/[controller]")]
[Tags("Market Data")]
public class MarketDataController : ControllerBase
{
    // All endpoints in this controller will be grouped under "Market Data"
}
```

### Custom Grouping

```csharp
[HttpGet("symbols")]
[SwaggerOperation(Tags = new[] { "Market Data", "Reference Data" })]
public async Task<ActionResult<List<string>>> GetSymbols()
{
    // This endpoint appears in both "Market Data" and "Reference Data" groups
}
```

---

## 7. Custom Schema Examples

### ISchemaFilter Implementation

Create a custom schema filter:

```csharp
public class ExampleSchemaFilter : ISchemaFilter
{
    public void Apply(OpenApiSchema schema, SchemaFilterContext context)
    {
        if (context.Type == typeof(MarketOrderRequest))
        {
            schema.Example = new OpenApiObject
            {
                ["symbol"] = new OpenApiString("BTCUSDT"),
                ["side"] = new OpenApiString("Buy"),
                ["quantity"] = new OpenApiDouble(0.001),
                ["brokerId"] = new OpenApiString("bybit")
            };
        }

        if (context.Type == typeof(OrderResponse))
        {
            schema.Example = new OpenApiObject
            {
                ["orderId"] = new OpenApiString("123456789"),
                ["symbol"] = new OpenApiString("BTCUSDT"),
                ["status"] = new OpenApiString("Filled"),
                ["filledQuantity"] = new OpenApiDouble(0.001),
                ["avgPrice"] = new OpenApiDouble(65432.10),
                ["timestamp"] = new OpenApiString("2025-10-21T12:00:00Z")
            };
        }
    }
}
```

Register in `Program.cs`:

```csharp
builder.Services.AddSwaggerGen(options =>
{
    // ... existing config
    options.SchemaFilter<ExampleSchemaFilter>();
});
```

---

## 8. API Versioning in Swagger

### Add Version Support

Install package:
```bash
dotnet add package Asp.Versioning.Mvc
dotnet add package Asp.Versioning.Mvc.ApiExplorer
```

Configure in `Program.cs`:

```csharp
builder.Services.AddApiVersioning(options =>
{
    options.DefaultApiVersion = new ApiVersion(1, 0);
    options.AssumeDefaultVersionWhenUnspecified = true;
    options.ReportApiVersions = true;
}).AddApiExplorer(options =>
{
    options.GroupNameFormat = "'v'VVV";
    options.SubstituteApiVersionInUrl = true;
});

builder.Services.AddSwaggerGen(options =>
{
    var provider = app.Services.GetRequiredService<IApiVersionDescriptionProvider>();

    foreach (var description in provider.ApiVersionDescriptions)
    {
        options.SwaggerDoc(description.GroupName, new OpenApiInfo
        {
            Title = $"AlgoTrendy API {description.ApiVersion}",
            Version = description.ApiVersion.ToString(),
            Description = "Algorithmic Trading Platform API"
        });
    }
});
```

---

## 9. Complete Example Controller

```csharp
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Swashbuckle.AspNetCore.Annotations;

namespace AlgoTrendy.API.Controllers
{
    /// <summary>
    /// Market data operations
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    [Tags("Market Data")]
    [Produces("application/json")]
    public class MarketDataController : ControllerBase
    {
        private readonly IMarketDataService _marketDataService;
        private readonly ILogger<MarketDataController> _logger;

        public MarketDataController(
            IMarketDataService marketDataService,
            ILogger<MarketDataController> logger)
        {
            _marketDataService = marketDataService;
            _logger = logger;
        }

        /// <summary>
        /// Get list of available trading symbols
        /// </summary>
        /// <returns>List of trading symbols</returns>
        /// <response code="200">Returns the list of symbols</response>
        /// <response code="429">Rate limit exceeded</response>
        /// <remarks>
        /// Sample request:
        ///
        ///     GET /api/MarketData/symbols
        ///
        /// Returns all symbols available for trading across all integrated exchanges.
        /// </remarks>
        [HttpGet("symbols")]
        [AllowAnonymous]
        [SwaggerOperation(
            Summary = "Get available symbols",
            Description = "Returns all trading symbols available across integrated exchanges",
            OperationId = "GetSymbols"
        )]
        [SwaggerResponse(200, "List of symbols", typeof(List<string>))]
        [SwaggerResponse(429, "Rate limit exceeded", typeof(ErrorResponse))]
        public async Task<ActionResult<List<string>>> GetSymbols()
        {
            var symbols = await _marketDataService.GetSymbolsAsync();
            return Ok(symbols);
        }

        /// <summary>
        /// Get latest market data for a symbol
        /// </summary>
        /// <param name="symbol">Trading symbol (e.g., BTCUSDT)</param>
        /// <returns>Latest market data</returns>
        /// <response code="200">Returns latest market data</response>
        /// <response code="404">Symbol not found</response>
        /// <response code="429">Rate limit exceeded</response>
        /// <remarks>
        /// Sample request:
        ///
        ///     GET /api/MarketData/latest/BTCUSDT
        ///
        /// **Response includes:**
        /// - Current price (open, high, low, close)
        /// - Trading volume
        /// - Timestamp
        /// - Exchange source
        ///
        /// **Rate Limiting:**
        /// - Unauthenticated: 100 req/min
        /// - Authenticated: 1000 req/min
        ///
        /// Data is cached for 1 second to optimize performance.
        /// </remarks>
        [HttpGet("latest/{symbol}")]
        [AllowAnonymous]
        [SwaggerOperation(
            Summary = "Get latest market data",
            Description = "Returns real-time market data for the specified symbol",
            OperationId = "GetLatestMarketData"
        )]
        [SwaggerResponse(200, "Latest market data", typeof(MarketDataResponse))]
        [SwaggerResponse(404, "Symbol not found", typeof(ErrorResponse))]
        [SwaggerResponse(429, "Rate limit exceeded", typeof(ErrorResponse))]
        public async Task<ActionResult<MarketDataResponse>> GetLatest(
            [FromRoute, SwaggerParameter("Trading symbol", Required = true)]
            string symbol)
        {
            try
            {
                var data = await _marketDataService.GetLatestAsync(symbol);
                return Ok(data);
            }
            catch (SymbolNotFoundException ex)
            {
                return NotFound(new ErrorResponse
                {
                    Code = "SYMBOL_NOT_FOUND",
                    Message = ex.Message
                });
            }
        }
    }
}
```

---

## 10. Customizing Swagger UI

Update `Program.cs`:

```csharp
app.UseSwaggerUI(options =>
{
    options.SwaggerEndpoint("/swagger/v1/swagger.json", "AlgoTrendy API v1");
    options.RoutePrefix = "swagger";

    // Customize UI
    options.DocExpansion(Swashbuckle.AspNetCore.SwaggerUI.DocExpansion.List);
    options.DefaultModelsExpandDepth(-1); // Hide schemas by default
    options.DisplayRequestDuration();
    options.EnableTryItOutByDefault();
    options.EnableDeepLinking();
    options.ShowExtensions();

    // Custom CSS
    options.InjectStylesheet("/swagger-ui/custom.css");

    // Custom title
    options.DocumentTitle = "AlgoTrendy API Documentation";
});
```

---

## Testing Your Enhancements

1. **Build the project:**
   ```bash
   dotnet build
   ```

2. **Run the API:**
   ```bash
   cd backend/AlgoTrendy.API
   dotnet run
   ```

3. **Open Swagger UI:**
   ```
   http://localhost:5002/swagger
   ```

4. **Verify:**
   - All endpoints have descriptions
   - Examples are shown for requests/responses
   - Authentication is documented
   - Error responses are clear
   - Rate limits are explained

---

## Next Steps

- [ ] Add XML comments to all controllers
- [ ] Add examples to all DTOs
- [ ] Document all error responses
- [ ] Add operation summaries
- [ ] Test "Try it out" functionality
- [ ] Review and update based on user feedback

---

For more information, see:
- [Swashbuckle Documentation](https://github.com/domaindrivendev/Swashbuckle.AspNetCore)
- [OpenAPI Specification](https://swagger.io/specification/)
