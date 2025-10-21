using Microsoft.AspNetCore.Mvc.ApiExplorer;
using Microsoft.OpenApi.Any;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;
using System;
using System.Linq;

namespace AlgoTrendy.API.Swagger;

/// <summary>
/// Swagger operation filter to add default values and examples
/// </summary>
public class SwaggerDefaultValues : IOperationFilter
{
    public void Apply(OpenApiOperation operation, OperationFilterContext context)
    {
        var apiDescription = context.ApiDescription;

        // Set deprecated flag (check for Obsolete attribute on action method)
        operation.Deprecated |= context.MethodInfo
            .GetCustomAttributes(true)
            .OfType<ObsoleteAttribute>()
            .Any();

        // Add response content types
        foreach (var responseType in context.ApiDescription.SupportedResponseTypes)
        {
            var responseKey = responseType.IsDefaultResponse
                ? "default"
                : responseType.StatusCode.ToString();

            var response = operation.Responses[responseKey];

            foreach (var contentType in response.Content.Keys)
            {
                if (!responseType.ApiResponseFormats.Any(x => x.MediaType == contentType))
                {
                    response.Content.Remove(contentType);
                }
            }
        }

        // Add examples to request parameters
        if (operation.Parameters == null)
            return;

        foreach (var parameter in operation.Parameters)
        {
            var description = apiDescription.ParameterDescriptions
                .FirstOrDefault(p => p.Name == parameter.Name);

            if (description != null)
            {
                // Set default value
                if (description.DefaultValue != null)
                {
                    parameter.Schema.Default = new OpenApiString(description.DefaultValue.ToString());
                }

                // Mark as required if needed
                parameter.Required |= description.IsRequired;

                // Add examples based on parameter name
                AddParameterExamples(parameter);
            }
        }

        // Add request body examples
        AddRequestBodyExamples(operation, context);

        // Add response examples
        AddResponseExamples(operation, context);
    }

    private static void AddParameterExamples(OpenApiParameter parameter)
    {
        switch (parameter.Name.ToLowerInvariant())
        {
            case "symbol":
                parameter.Example = new OpenApiString("BTCUSDT");
                parameter.Description = parameter.Description ?? "Trading symbol (e.g., BTCUSDT, ETHUSDT)";
                break;

            case "exchange":
                parameter.Example = new OpenApiString("binance");
                parameter.Description = parameter.Description ?? "Exchange name (binance, okx, coinbase, kraken)";
                break;

            case "orderid":
                parameter.Example = new OpenApiString("550e8400-e29b-41d4-a716-446655440000");
                parameter.Description = parameter.Description ?? "Internal order ID (UUID format)";
                break;

            case "currency":
                parameter.Example = new OpenApiString("USDT");
                parameter.Description = parameter.Description ?? "Currency symbol (USDT, BTC, ETH, etc.)";
                break;

            case "interval":
                parameter.Example = new OpenApiString("1h");
                parameter.Description = parameter.Description ?? "Time interval (1h, 1d, 1w)";
                break;

            case "starttime":
                parameter.Example = new OpenApiString(DateTime.UtcNow.AddDays(-7).ToString("o"));
                parameter.Description = parameter.Description ?? "Start time in ISO 8601 format (UTC)";
                break;

            case "endtime":
                parameter.Example = new OpenApiString(DateTime.UtcNow.ToString("o"));
                parameter.Description = parameter.Description ?? "End time in ISO 8601 format (UTC)";
                break;

            case "symbols":
                parameter.Example = new OpenApiString("BTCUSDT,ETHUSDT,SOLUSDT");
                parameter.Description = parameter.Description ?? "Comma-separated list of trading symbols";
                break;
        }
    }

    private static void AddRequestBodyExamples(OpenApiOperation operation, OperationFilterContext context)
    {
        if (operation.RequestBody == null)
            return;

        // Get the controller and action name
        var controllerName = context.MethodInfo.DeclaringType?.Name ?? "";
        var actionName = context.MethodInfo.Name;

        // Add examples based on controller and action
        if (controllerName.Contains("Trading") && actionName.Contains("PlaceOrder"))
        {
            AddOrderRequestExample(operation);
        }
        else if (controllerName.Contains("MarketData") && actionName.Contains("Insert"))
        {
            AddMarketDataExample(operation);
        }
    }

    private static void AddOrderRequestExample(OpenApiOperation operation)
    {
        if (!operation.RequestBody.Content.TryGetValue("application/json", out var content))
            return;

        content.Example = new OpenApiObject
        {
            ["clientOrderId"] = new OpenApiString("AT_1697123456789_abc123def456"),
            ["symbol"] = new OpenApiString("BTCUSDT"),
            ["exchange"] = new OpenApiString("binance"),
            ["side"] = new OpenApiString("Buy"),
            ["type"] = new OpenApiString("Market"),
            ["quantity"] = new OpenApiDouble(0.001),
            ["price"] = new OpenApiNull(),
            ["stopPrice"] = new OpenApiNull(),
            ["strategyId"] = new OpenApiString("momentum_v1"),
            ["metadata"] = new OpenApiString("{\"source\":\"web\",\"version\":\"2.6.0\"}")
        };

        content.Examples = new Dictionary<string, Microsoft.OpenApi.Models.OpenApiExample>
        {
            ["market_buy"] = new()
            {
                Summary = "Market Buy Order",
                Description = "Simple market buy order with auto-generated ClientOrderId",
                Value = new OpenApiObject
                {
                    ["symbol"] = new OpenApiString("BTCUSDT"),
                    ["exchange"] = new OpenApiString("binance"),
                    ["side"] = new OpenApiString("Buy"),
                    ["type"] = new OpenApiString("Market"),
                    ["quantity"] = new OpenApiDouble(0.001)
                }
            },
            ["limit_sell"] = new()
            {
                Summary = "Limit Sell Order",
                Description = "Limit sell order with custom ClientOrderId for idempotency",
                Value = new OpenApiObject
                {
                    ["clientOrderId"] = new OpenApiString("AT_1697123456789_custom123"),
                    ["symbol"] = new OpenApiString("ETHUSDT"),
                    ["exchange"] = new OpenApiString("binance"),
                    ["side"] = new OpenApiString("Sell"),
                    ["type"] = new OpenApiString("Limit"),
                    ["quantity"] = new OpenApiDouble(1.5),
                    ["price"] = new OpenApiDouble(3000.00)
                }
            },
            ["stop_loss"] = new()
            {
                Summary = "Stop-Loss Order",
                Description = "Stop-loss order with trigger price",
                Value = new OpenApiObject
                {
                    ["symbol"] = new OpenApiString("BTCUSDT"),
                    ["exchange"] = new OpenApiString("binance"),
                    ["side"] = new OpenApiString("Sell"),
                    ["type"] = new OpenApiString("StopLoss"),
                    ["quantity"] = new OpenApiDouble(0.5),
                    ["stopPrice"] = new OpenApiDouble(42000.00)
                }
            }
        };
    }

    private static void AddMarketDataExample(OpenApiOperation operation)
    {
        if (!operation.RequestBody.Content.TryGetValue("application/json", out var content))
            return;

        content.Example = new OpenApiObject
        {
            ["symbol"] = new OpenApiString("BTCUSDT"),
            ["exchange"] = new OpenApiString("binance"),
            ["timestamp"] = new OpenApiString(DateTime.UtcNow.ToString("o")),
            ["open"] = new OpenApiDouble(43250.50),
            ["high"] = new OpenApiDouble(43500.00),
            ["low"] = new OpenApiDouble(43100.00),
            ["close"] = new OpenApiDouble(43450.00),
            ["volume"] = new OpenApiDouble(125.5),
            ["quoteVolume"] = new OpenApiDouble(5443125.25)
        };
    }

    private static void AddResponseExamples(OpenApiOperation operation, OperationFilterContext context)
    {
        // Add common error response examples
        AddErrorResponseExamples(operation);

        // Add success response examples based on controller
        var controllerName = context.MethodInfo.DeclaringType?.Name ?? "";
        var actionName = context.MethodInfo.Name;

        if (controllerName.Contains("Order") || controllerName.Contains("Trading"))
        {
            AddOrderResponseExamples(operation, actionName);
        }
        else if (controllerName.Contains("MarketData"))
        {
            AddMarketDataResponseExamples(operation, actionName);
        }
        else if (controllerName.Contains("Backtest"))
        {
            AddBacktestResponseExamples(operation, actionName);
        }
    }

    private static void AddErrorResponseExamples(OpenApiOperation operation)
    {
        // Add 400 Bad Request example
        if (operation.Responses.TryGetValue("400", out var badRequestResponse))
        {
            if (badRequestResponse.Content.TryGetValue("application/json", out var content))
            {
                content.Example = new OpenApiObject
                {
                    ["error"] = new OpenApiString("Validation failed"),
                    ["details"] = new OpenApiArray
                    {
                        new OpenApiObject
                        {
                            ["field"] = new OpenApiString("quantity"),
                            ["message"] = new OpenApiString("Quantity must be greater than 0")
                        }
                    },
                    ["timestamp"] = new OpenApiString(DateTime.UtcNow.ToString("o"))
                };
            }
        }

        // Add 404 Not Found example
        if (operation.Responses.TryGetValue("404", out var notFoundResponse))
        {
            if (notFoundResponse.Content.TryGetValue("application/json", out var content))
            {
                content.Example = new OpenApiObject
                {
                    ["error"] = new OpenApiString("Resource not found"),
                    ["message"] = new OpenApiString("Order with ID '550e8400-e29b-41d4-a716-446655440000' was not found"),
                    ["timestamp"] = new OpenApiString(DateTime.UtcNow.ToString("o"))
                };
            }
        }

        // Add 429 Rate Limit example
        if (operation.Responses.TryGetValue("429", out var rateLimitResponse))
        {
            if (rateLimitResponse.Content.TryGetValue("application/json", out var content))
            {
                content.Example = new OpenApiObject
                {
                    ["error"] = new OpenApiString("Rate limit exceeded"),
                    ["message"] = new OpenApiString("Too many requests. Please try again in 60 seconds."),
                    ["retryAfter"] = new OpenApiInteger(60),
                    ["timestamp"] = new OpenApiString(DateTime.UtcNow.ToString("o"))
                };
            }
        }

        // Add 500 Internal Server Error example
        if (operation.Responses.TryGetValue("500", out var serverErrorResponse))
        {
            if (serverErrorResponse.Content.TryGetValue("application/json", out var content))
            {
                content.Example = new OpenApiObject
                {
                    ["error"] = new OpenApiString("Internal server error"),
                    ["message"] = new OpenApiString("An unexpected error occurred. Please try again later."),
                    ["requestId"] = new OpenApiString("550e8400-e29b-41d4-a716-446655440000"),
                    ["timestamp"] = new OpenApiString(DateTime.UtcNow.ToString("o"))
                };
            }
        }
    }

    private static void AddOrderResponseExamples(OpenApiOperation operation, string actionName)
    {
        if (operation.Responses.TryGetValue("200", out var successResponse))
        {
            if (successResponse.Content.TryGetValue("application/json", out var content))
            {
                if (actionName.Contains("Place") || actionName.Contains("Submit"))
                {
                    content.Example = new OpenApiObject
                    {
                        ["orderId"] = new OpenApiString("550e8400-e29b-41d4-a716-446655440000"),
                        ["clientOrderId"] = new OpenApiString("AT_1697123456789_abc123"),
                        ["exchangeOrderId"] = new OpenApiString("12345678901"),
                        ["status"] = new OpenApiString("Filled"),
                        ["message"] = new OpenApiString("Order placed successfully"),
                        ["timestamp"] = new OpenApiString(DateTime.UtcNow.ToString("o"))
                    };
                }
                else if (actionName.Contains("Get") || actionName.Contains("Retrieve"))
                {
                    content.Example = new OpenApiArray
                    {
                        new OpenApiObject
                        {
                            ["orderId"] = new OpenApiString("550e8400-e29b-41d4-a716-446655440000"),
                            ["symbol"] = new OpenApiString("BTCUSDT"),
                            ["side"] = new OpenApiString("Buy"),
                            ["type"] = new OpenApiString("Market"),
                            ["status"] = new OpenApiString("Filled"),
                            ["quantity"] = new OpenApiDouble(0.001),
                            ["filledQuantity"] = new OpenApiDouble(0.001),
                            ["averageFillPrice"] = new OpenApiDouble(43250.50),
                            ["createdAt"] = new OpenApiString(DateTime.UtcNow.AddMinutes(-5).ToString("o"))
                        }
                    };
                }
            }
        }
    }

    private static void AddMarketDataResponseExamples(OpenApiOperation operation, string actionName)
    {
        if (operation.Responses.TryGetValue("200", out var successResponse))
        {
            if (successResponse.Content.TryGetValue("application/json", out var content))
            {
                content.Example = new OpenApiArray
                {
                    new OpenApiObject
                    {
                        ["symbol"] = new OpenApiString("BTCUSDT"),
                        ["timestamp"] = new OpenApiString(DateTime.UtcNow.ToString("o")),
                        ["open"] = new OpenApiDouble(43250.50),
                        ["high"] = new OpenApiDouble(43500.00),
                        ["low"] = new OpenApiDouble(43100.00),
                        ["close"] = new OpenApiDouble(43450.00),
                        ["volume"] = new OpenApiDouble(125.5),
                        ["quoteVolume"] = new OpenApiDouble(5443125.25)
                    }
                };
            }
        }
    }

    private static void AddBacktestResponseExamples(OpenApiOperation operation, string actionName)
    {
        if (operation.Responses.TryGetValue("200", out var successResponse))
        {
            if (successResponse.Content.TryGetValue("application/json", out var content))
            {
                content.Example = new OpenApiObject
                {
                    ["backtestId"] = new OpenApiString("550e8400-e29b-41d4-a716-446655440000"),
                    ["status"] = new OpenApiString("Completed"),
                    ["startDate"] = new OpenApiString("2024-01-01T00:00:00Z"),
                    ["endDate"] = new OpenApiString("2024-12-31T23:59:59Z"),
                    ["initialCapital"] = new OpenApiDouble(10000.00),
                    ["finalCapital"] = new OpenApiDouble(12500.00),
                    ["totalReturn"] = new OpenApiDouble(0.25),
                    ["sharpeRatio"] = new OpenApiDouble(1.85),
                    ["maxDrawdown"] = new OpenApiDouble(-0.08),
                    ["winRate"] = new OpenApiDouble(0.62),
                    ["totalTrades"] = new OpenApiInteger(150),
                    ["createdAt"] = new OpenApiString(DateTime.UtcNow.ToString("o"))
                };
            }
        }
    }
}
