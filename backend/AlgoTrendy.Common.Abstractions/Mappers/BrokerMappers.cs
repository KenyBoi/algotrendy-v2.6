using AlgoTrendy.Core.Enums;

namespace AlgoTrendy.Common.Abstractions.Mappers;

/// <summary>
/// Standardized type mappers for converting between AlgoTrendy Core enums and broker-specific types.
/// Eliminates ~80 lines of duplicate mapping code across brokers.
/// </summary>
public static class BrokerMappers
{
    /// <summary>
    /// Generic mapper for OrderSide to broker-specific representations.
    /// </summary>
    public static class OrderSideMapper
    {
        /// <summary>
        /// Maps to uppercase string format (BUY/SELL) - used by Binance, NinjaTrader, etc.
        /// </summary>
        public static string ToUppercaseString(OrderSide side)
        {
            return side switch
            {
                OrderSide.Buy => "BUY",
                OrderSide.Sell => "SELL",
                _ => throw new ArgumentException($"Unsupported order side: {side}", nameof(side))
            };
        }

        /// <summary>
        /// Maps to titlecase string format (Buy/Sell) - used by TradeStation, etc.
        /// </summary>
        public static string ToTitlecaseString(OrderSide side)
        {
            return side switch
            {
                OrderSide.Buy => "Buy",
                OrderSide.Sell => "Sell",
                _ => throw new ArgumentException($"Unsupported order side: {side}", nameof(side))
            };
        }

        /// <summary>
        /// Maps from uppercase string to OrderSide.
        /// </summary>
        public static OrderSide FromUppercaseString(string side)
        {
            return side?.ToUpperInvariant() switch
            {
                "BUY" => OrderSide.Buy,
                "SELL" => OrderSide.Sell,
                _ => throw new ArgumentException($"Unsupported order side string: {side}", nameof(side))
            };
        }

        /// <summary>
        /// Maps from titlecase string to OrderSide.
        /// </summary>
        public static OrderSide FromTitlecaseString(string side)
        {
            return side switch
            {
                "Buy" => OrderSide.Buy,
                "Sell" => OrderSide.Sell,
                _ => throw new ArgumentException($"Unsupported order side string: {side}", nameof(side))
            };
        }
    }

    /// <summary>
    /// Generic mapper for OrderType to broker-specific representations.
    /// </summary>
    public static class OrderTypeMapper
    {
        /// <summary>
        /// Maps to uppercase string format (MARKET/LIMIT/STOP_LOSS/TAKE_PROFIT).
        /// Used by Binance, NinjaTrader.
        /// </summary>
        public static string ToUppercaseString(OrderType type, bool useUnderscore = false)
        {
            return type switch
            {
                OrderType.Market => "MARKET",
                OrderType.Limit => "LIMIT",
                OrderType.StopLoss => useUnderscore ? "STOP_LOSS" : "STOP LOSS",
                OrderType.StopLossLimit => useUnderscore ? "STOP_LOSS_LIMIT" : "STOP LOSS LIMIT",
                OrderType.TakeProfit => useUnderscore ? "TAKE_PROFIT" : "TAKE PROFIT",
                OrderType.TakeProfitLimit => useUnderscore ? "TAKE_PROFIT_LIMIT" : "TAKE PROFIT LIMIT",
                _ => throw new ArgumentException($"Unsupported order type: {type}", nameof(type))
            };
        }

        /// <summary>
        /// Maps to titlecase string format (Market/Limit/StopLoss/TakeProfit).
        /// Used by TradeStation.
        /// </summary>
        public static string ToTitlecaseString(OrderType type)
        {
            return type switch
            {
                OrderType.Market => "Market",
                OrderType.Limit => "Limit",
                OrderType.StopLoss => "StopLoss",
                OrderType.StopLossLimit => "StopLossLimit",
                OrderType.TakeProfit => "TakeProfit",
                OrderType.TakeProfitLimit => "TakeProfitLimit",
                _ => throw new ArgumentException($"Unsupported order type: {type}", nameof(type))
            };
        }

        /// <summary>
        /// Maps from uppercase string to OrderType.
        /// </summary>
        public static OrderType FromUppercaseString(string type)
        {
            return type?.ToUpperInvariant().Replace("_", " ") switch
            {
                "MARKET" => OrderType.Market,
                "LIMIT" => OrderType.Limit,
                "STOP LOSS" or "STOP-LOSS" => OrderType.StopLoss,
                "STOP LOSS LIMIT" or "STOP-LOSS-LIMIT" => OrderType.StopLossLimit,
                "TAKE PROFIT" or "TAKE-PROFIT" => OrderType.TakeProfit,
                "TAKE PROFIT LIMIT" or "TAKE-PROFIT-LIMIT" => OrderType.TakeProfitLimit,
                _ => throw new ArgumentException($"Unsupported order type string: {type}", nameof(type))
            };
        }
    }

    /// <summary>
    /// Generic mapper for OrderStatus to broker-specific representations.
    /// </summary>
    public static class OrderStatusMapper
    {
        /// <summary>
        /// Maps to uppercase string format (NEW/FILLED/CANCELED/etc).
        /// Used by Binance.
        /// </summary>
        public static string ToUppercaseString(OrderStatus status)
        {
            return status switch
            {
                OrderStatus.Pending => "NEW",
                OrderStatus.PartiallyFilled => "PARTIALLY_FILLED",
                OrderStatus.Filled => "FILLED",
                OrderStatus.Cancelled => "CANCELED",
                OrderStatus.Rejected => "REJECTED",
                OrderStatus.Expired => "EXPIRED",
                _ => throw new ArgumentException($"Unsupported order status: {status}", nameof(status))
            };
        }

        /// <summary>
        /// Maps from uppercase string to OrderStatus.
        /// </summary>
        public static OrderStatus FromUppercaseString(string status)
        {
            return status?.ToUpperInvariant() switch
            {
                "NEW" or "PENDING" or "SUBMITTED" => OrderStatus.Pending,
                "PARTIALLY_FILLED" or "PARTIAL" => OrderStatus.PartiallyFilled,
                "FILLED" or "COMPLETED" => OrderStatus.Filled,
                "CANCELED" or "CANCELLED" => OrderStatus.Cancelled,
                "REJECTED" => OrderStatus.Rejected,
                "EXPIRED" => OrderStatus.Expired,
                _ => throw new ArgumentException($"Unsupported order status string: {status}", nameof(status))
            };
        }

        /// <summary>
        /// Maps to titlecase string format (Pending/Filled/Cancelled/etc).
        /// Used by some brokers.
        /// </summary>
        public static string ToTitlecaseString(OrderStatus status)
        {
            return status switch
            {
                OrderStatus.Pending => "Pending",
                OrderStatus.PartiallyFilled => "PartiallyFilled",
                OrderStatus.Filled => "Filled",
                OrderStatus.Cancelled => "Cancelled",
                OrderStatus.Rejected => "Rejected",
                OrderStatus.Expired => "Expired",
                _ => throw new ArgumentException($"Unsupported order status: {status}", nameof(status))
            };
        }
    }

    /// <summary>
    /// Helper for time-in-force conversions.
    /// </summary>
    public static class TimeInForceMapper
    {
        public static string ToUppercaseString(string? timeInForce)
        {
            return timeInForce?.ToUpperInvariant() switch
            {
                "GTC" or "GOODTILCANCELED" => "GTC",
                "IOC" or "IMMEDIATEORCANCEL" => "IOC",
                "FOK" or "FILLORKILL" => "FOK",
                "GTX" or "GOODTILCROSSING" => "GTX",
                null or "" => "GTC", // Default
                _ => timeInForce.ToUpperInvariant()
            };
        }
    }

    /// <summary>
    /// Broker-specific symbol format converters.
    /// </summary>
    public static class SymbolMapper
    {
        /// <summary>
        /// Converts symbol to Binance format (e.g., "BTC/USD" -> "BTCUSD").
        /// </summary>
        public static string ToBinanceFormat(string symbol)
        {
            return symbol.Replace("/", "").Replace("-", "").ToUpperInvariant();
        }

        /// <summary>
        /// Converts Binance format to standard format (e.g., "BTCUSD" -> "BTC/USD").
        /// Attempts to split common quote currencies.
        /// </summary>
        public static string FromBinanceFormat(string binanceSymbol)
        {
            // Common quote currencies
            var quotes = new[] { "USDT", "USD", "BUSD", "EUR", "BTC", "ETH", "BNB" };

            foreach (var quote in quotes)
            {
                if (binanceSymbol.EndsWith(quote))
                {
                    var baseAsset = binanceSymbol[..^quote.Length];
                    return $"{baseAsset}/{quote}";
                }
            }

            // If no match, return as-is
            return binanceSymbol;
        }

        /// <summary>
        /// Converts symbol to Bybit format (e.g., "BTC/USD" -> "BTCUSD").
        /// </summary>
        public static string ToBybitFormat(string symbol)
        {
            return symbol.Replace("/", "").Replace("-", "").ToUpperInvariant();
        }

        /// <summary>
        /// Normalizes symbol to standard format (BASE/QUOTE).
        /// </summary>
        public static string ToStandardFormat(string symbol)
        {
            // Remove any existing separators
            var clean = symbol.Replace("/", "").Replace("-", "").Replace("_", "");

            // Try to detect and split common patterns
            if (clean.EndsWith("USDT"))
                return $"{clean[..^4]}/USDT";
            if (clean.EndsWith("USD"))
                return $"{clean[..^3]}/USD";
            if (clean.EndsWith("BUSD"))
                return $"{clean[..^4]}/BUSD";
            if (clean.EndsWith("BTC"))
                return $"{clean[..^3]}/BTC";
            if (clean.EndsWith("ETH"))
                return $"{clean[..^3]}/ETH";

            // If no pattern matches, return cleaned version
            return clean;
        }
    }
}
