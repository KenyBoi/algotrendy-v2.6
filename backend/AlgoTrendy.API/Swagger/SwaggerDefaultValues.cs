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
}
