using AlgoTrendy.Core.Enums;

namespace AlgoTrendy.Core.Services;

/// <summary>
/// Service for formatting trading symbols according to broker-specific requirements
/// Different brokers use different symbol formats for the same asset
/// </summary>
public class SymbolFormatterService
{
    public SymbolFormatterService()
    {
    }

    /// <summary>
    /// Format a symbol for a specific broker and asset type
    /// </summary>
    /// <param name="symbol">Original symbol (e.g., "BTCUSDT", "AAPL", "ES")</param>
    /// <param name="assetType">Type of asset</param>
    /// <param name="brokerName">Target broker name</param>
    /// <returns>Formatted symbol for the broker</returns>
    public string FormatForBroker(string symbol, AssetType assetType, string brokerName)
    {
        if (string.IsNullOrWhiteSpace(symbol))
            throw new ArgumentException("Symbol cannot be null or empty", nameof(symbol));

        if (string.IsNullOrWhiteSpace(brokerName))
            throw new ArgumentException("Broker name cannot be null or empty", nameof(brokerName));

        var broker = brokerName.ToLowerInvariant();

        try
        {
            var formatted = assetType switch
            {
                AssetType.Cryptocurrency => FormatCryptoSymbol(symbol, broker),
                AssetType.Stock => FormatStockSymbol(symbol, broker),
                AssetType.Futures => FormatFuturesSymbol(symbol, broker),
                AssetType.Options => FormatOptionsSymbol(symbol, broker),
                AssetType.ETF => FormatETFSymbol(symbol, broker),
                AssetType.Forex => FormatForexSymbol(symbol, broker),
                _ => symbol
            };

            return formatted;
        }
        catch (Exception)
        {
            // If formatting fails, return original symbol
            return symbol;
        }
    }

    /// <summary>
    /// Detect asset type from symbol format
    /// </summary>
    public AssetType DetectAssetType(string symbol)
    {
        if (string.IsNullOrWhiteSpace(symbol))
            return AssetType.Cryptocurrency; // Default

        var upperSymbol = symbol.ToUpperInvariant();

        // Crypto patterns
        if (upperSymbol.EndsWith("USDT") || upperSymbol.EndsWith("USDC") ||
            upperSymbol.EndsWith("BTC") || upperSymbol.EndsWith("ETH") ||
            upperSymbol.Contains("-USDT") || upperSymbol.Contains("-USD"))
        {
            // Check if it's a futures contract
            if (upperSymbol.Contains("PERP") || upperSymbol.Contains("_USD_") ||
                upperSymbol.EndsWith("USD") && !upperSymbol.EndsWith("USDT"))
                return AssetType.Futures;

            return AssetType.Cryptocurrency;
        }

        // Options patterns (have expiration and strike)
        if (upperSymbol.Length > 10 &&
            (upperSymbol.Contains("C") || upperSymbol.Contains("P")) &&
            char.IsDigit(upperSymbol[upperSymbol.Length - 1]))
        {
            return AssetType.Options;
        }

        // Futures patterns
        if (upperSymbol.Length == 2 || // ES, NQ
            upperSymbol.EndsWith("_F") || // Futures suffix
            upperSymbol.Contains(" ")) // "ES 12-25" format
        {
            return AssetType.Futures;
        }

        // ETF patterns (common ETFs)
        var commonETFs = new[] { "SPY", "QQQ", "IWM", "GLD", "SLV", "TLT", "VXX", "UVXY" };
        if (commonETFs.Contains(upperSymbol))
        {
            return AssetType.ETF;
        }

        // Forex patterns
        if (upperSymbol.Length == 6 && upperSymbol.All(char.IsLetter))
        {
            // EUR/USD -> EURUSD format
            return AssetType.Forex;
        }

        // Default to stock for standard ticker symbols
        return AssetType.Stock;
    }

    #region Private Formatting Methods

    private string FormatCryptoSymbol(string symbol, string broker)
    {
        return broker switch
        {
            "binance" => symbol, // BTCUSDT
            "bybit" => symbol,   // BTCUSDT
            "okx" => symbol.Contains("-") ? symbol : symbol.Replace("USDT", "-USDT"), // BTC-USDT
            "coinbase" => symbol.Contains("-") ? symbol : symbol.Replace("USDT", "-USD"), // BTC-USD
            "kraken" => symbol.Replace("USDT", "USD"), // BTCUSD
            _ => symbol
        };
    }

    private string FormatStockSymbol(string symbol, string broker)
    {
        return broker switch
        {
            "tradestation" => symbol, // AAPL
            "interactivebrokers" => symbol, // AAPL
            "alpaca" => symbol, // AAPL
            _ => symbol
        };
    }

    private string FormatFuturesSymbol(string symbol, string broker)
    {
        return broker switch
        {
            // Crypto futures
            "bybit" when symbol.Contains("PERP") => symbol, // BTC_USD_PERP
            "bybit" when symbol.EndsWith("USD") => symbol, // BTCUSD
            "binance" when symbol.Contains("PERP") => symbol.Replace("_USD_PERP", "USDT"), // BTCUSDT

            // Traditional futures
            "ninjatrader" => FormatForNinjaTrader(symbol), // ES 12-25
            "interactivebrokers" => symbol, // ES

            _ => symbol
        };
    }

    private string FormatOptionsSymbol(string symbol, string broker)
    {
        return broker switch
        {
            "interactivebrokers" => symbol, // OCC format
            "tradestation" => symbol, // Standard format
            _ => symbol
        };
    }

    private string FormatETFSymbol(string symbol, string broker)
    {
        // ETFs trade like stocks
        return FormatStockSymbol(symbol, broker);
    }

    private string FormatForexSymbol(string symbol, string broker)
    {
        return broker switch
        {
            "interactivebrokers" => symbol.Contains("/") ? symbol : InsertSlash(symbol), // EUR/USD
            "oanda" => symbol.Replace("/", "_"), // EUR_USD
            _ => symbol
        };
    }

    private string FormatForNinjaTrader(string symbol)
    {
        // NinjaTrader format: "ES 12-25" (symbol space month-year)
        // If already formatted, return as-is
        if (symbol.Contains(" "))
            return symbol;

        // For now, return as-is and let NinjaTrader handle it
        // In production, you'd need to add expiration date logic
        return symbol;
    }

    private string InsertSlash(string forexPair)
    {
        // EURUSD -> EUR/USD
        if (forexPair.Length == 6 && forexPair.All(char.IsLetter))
        {
            return $"{forexPair.Substring(0, 3)}/{forexPair.Substring(3, 3)}";
        }
        return forexPair;
    }

    #endregion

    /// <summary>
    /// Reverse format - convert broker-specific symbol back to standard format
    /// </summary>
    public string NormalizeSymbol(string brokerSymbol, AssetType assetType, string brokerName)
    {
        if (string.IsNullOrWhiteSpace(brokerSymbol))
            return brokerSymbol;

        var broker = brokerName.ToLowerInvariant();

        return assetType switch
        {
            AssetType.Cryptocurrency when broker == "okx" => brokerSymbol.Replace("-", ""),
            AssetType.Cryptocurrency when broker == "coinbase" => brokerSymbol.Replace("-USD", "USDT"),
            AssetType.Cryptocurrency when broker == "kraken" => brokerSymbol.Replace("USD", "USDT"),
            AssetType.Forex when broker == "interactivebrokers" => brokerSymbol.Replace("/", ""),
            AssetType.Forex when broker == "oanda" => brokerSymbol.Replace("_", ""),
            _ => brokerSymbol
        };
    }

    /// <summary>
    /// Get the base asset from a symbol (e.g., "BTCUSDT" -> "BTC")
    /// </summary>
    public string GetBaseAsset(string symbol, AssetType assetType)
    {
        if (string.IsNullOrWhiteSpace(symbol))
            return symbol;

        return assetType switch
        {
            AssetType.Cryptocurrency when symbol.EndsWith("USDT") => symbol.Replace("USDT", ""),
            AssetType.Cryptocurrency when symbol.EndsWith("USDC") => symbol.Replace("USDC", ""),
            AssetType.Cryptocurrency when symbol.Contains("-") => symbol.Split('-')[0],
            AssetType.Forex when symbol.Length == 6 => symbol.Substring(0, 3),
            _ => symbol
        };
    }

    /// <summary>
    /// Get the quote asset from a symbol (e.g., "BTCUSDT" -> "USDT")
    /// </summary>
    public string GetQuoteAsset(string symbol, AssetType assetType)
    {
        if (string.IsNullOrWhiteSpace(symbol))
            return symbol;

        return assetType switch
        {
            AssetType.Cryptocurrency when symbol.EndsWith("USDT") => "USDT",
            AssetType.Cryptocurrency when symbol.EndsWith("USDC") => "USDC",
            AssetType.Cryptocurrency when symbol.Contains("-") => symbol.Split('-')[1],
            AssetType.Forex when symbol.Length == 6 => symbol.Substring(3, 3),
            AssetType.Stock => "USD",
            AssetType.ETF => "USD",
            _ => "USD"
        };
    }
}
