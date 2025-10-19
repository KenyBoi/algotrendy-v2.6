using AlgoTrendy.Core.Enums;
using AlgoTrendy.Core.Models;
using Microsoft.OpenApi.Any;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace AlgoTrendy.API.Swagger;

/// <summary>
/// Provides example schemas for Swagger documentation
/// </summary>
public class SwaggerSchemaExamples : ISchemaFilter
{
    public void Apply(OpenApiSchema schema, SchemaFilterContext context)
    {
        if (context.Type == typeof(OrderRequest))
        {
            schema.Example = CreateOrderRequestExample();
        }
        else if (context.Type == typeof(Order))
        {
            schema.Example = CreateOrderExample();
        }
        else if (context.Type == typeof(MarketData))
        {
            schema.Example = CreateMarketDataExample();
        }
        else if (context.Type == typeof(Position))
        {
            schema.Example = CreatePositionExample();
        }
        else if (context.Type == typeof(OrderSide))
        {
            schema.Enum = new List<IOpenApiAny>
            {
                new OpenApiString("Buy"),
                new OpenApiString("Sell")
            };
        }
        else if (context.Type == typeof(OrderType))
        {
            schema.Enum = new List<IOpenApiAny>
            {
                new OpenApiString("Market"),
                new OpenApiString("Limit"),
                new OpenApiString("StopLoss"),
                new OpenApiString("StopLimit"),
                new OpenApiString("TakeProfit")
            };
        }
        else if (context.Type == typeof(OrderStatus))
        {
            schema.Enum = new List<IOpenApiAny>
            {
                new OpenApiString("Pending"),
                new OpenApiString("Open"),
                new OpenApiString("PartiallyFilled"),
                new OpenApiString("Filled"),
                new OpenApiString("Cancelled"),
                new OpenApiString("Rejected"),
                new OpenApiString("Expired")
            };
        }
    }

    private static OpenApiObject CreateOrderRequestExample()
    {
        return new OpenApiObject
        {
            ["clientOrderId"] = new OpenApiString("AT_1697123456789_abc123"),
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
    }

    private static OpenApiObject CreateOrderExample()
    {
        var now = DateTime.UtcNow;
        return new OpenApiObject
        {
            ["orderId"] = new OpenApiString("550e8400-e29b-41d4-a716-446655440000"),
            ["clientOrderId"] = new OpenApiString("AT_1697123456789_abc123"),
            ["exchangeOrderId"] = new OpenApiString("12345678901"),
            ["symbol"] = new OpenApiString("BTCUSDT"),
            ["exchange"] = new OpenApiString("binance"),
            ["side"] = new OpenApiString("Buy"),
            ["type"] = new OpenApiString("Market"),
            ["status"] = new OpenApiString("Filled"),
            ["quantity"] = new OpenApiDouble(0.001),
            ["filledQuantity"] = new OpenApiDouble(0.001),
            ["price"] = new OpenApiNull(),
            ["stopPrice"] = new OpenApiNull(),
            ["averageFillPrice"] = new OpenApiDouble(43250.50),
            ["strategyId"] = new OpenApiString("momentum_v1"),
            ["createdAt"] = new OpenApiString(now.AddMinutes(-5).ToString("o")),
            ["updatedAt"] = new OpenApiString(now.ToString("o")),
            ["submittedAt"] = new OpenApiString(now.AddMinutes(-4).ToString("o")),
            ["closedAt"] = new OpenApiString(now.ToString("o")),
            ["metadata"] = new OpenApiString("{\"source\":\"web\",\"version\":\"2.6.0\"}")
        };
    }

    private static OpenApiObject CreateMarketDataExample()
    {
        return new OpenApiObject
        {
            ["symbol"] = new OpenApiString("BTCUSDT"),
            ["source"] = new OpenApiString("binance"),
            ["timestamp"] = new OpenApiString(DateTime.UtcNow.ToString("o")),
            ["open"] = new OpenApiDouble(43250.50),
            ["high"] = new OpenApiDouble(43500.00),
            ["low"] = new OpenApiDouble(43100.00),
            ["close"] = new OpenApiDouble(43450.00),
            ["volume"] = new OpenApiDouble(125.5),
            ["quoteVolume"] = new OpenApiDouble(5443125.25),
            ["tradesCount"] = new OpenApiInteger(1250)
        };
    }

    private static OpenApiObject CreatePositionExample()
    {
        return new OpenApiObject
        {
            ["symbol"] = new OpenApiString("BTCUSDT"),
            ["exchange"] = new OpenApiString("binance"),
            ["side"] = new OpenApiString("Long"),
            ["quantity"] = new OpenApiDouble(0.5),
            ["entryPrice"] = new OpenApiDouble(42000.00),
            ["currentPrice"] = new OpenApiDouble(43250.50),
            ["unrealizedPnL"] = new OpenApiDouble(625.25),
            ["realizedPnL"] = new OpenApiDouble(150.00),
            ["leverage"] = new OpenApiDouble(3.0),
            ["marginUsed"] = new OpenApiDouble(7000.00),
            ["liquidationPrice"] = new OpenApiDouble(38500.00),
            ["timestamp"] = new OpenApiString(DateTime.UtcNow.ToString("o"))
        };
    }
}
