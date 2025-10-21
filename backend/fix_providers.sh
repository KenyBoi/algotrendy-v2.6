#!/bin/bash
# Fix all data provider build errors

cd /root/AlgoTrendy_v2.6/backend/AlgoTrendy.DataChannels/Providers

echo "Fixing TiingoProvider.cs..."
# Fix line 140: Add Source
sed -i '140s/Volume = vol/Volume = vol,\n                Source = "tiingo"/' TiingoProvider.cs

# Fix line 163: Add Source
sed -i '163s/Volume = vol/Volume = vol,\n                    Source = "tiingo"/' TiingoProvider.cs

# Fix line 172: Remove AdjustedClose
sed -i '/AdjustedClose =/d' TiingoProvider.cs

# Fix line 241: Add Source
sed -i '241s/QuoteVolume = quoteVol/QuoteVolume = quoteVol,\n                    Source = "tiingo"/' TiingoProvider.cs

# Fix line 260: Add Source
sed -i '260s/QuoteVolume = quoteVol/QuoteVolume = quoteVol,\n                    Source = "tiingo"/' TiingoProvider.cs

echo "Fixing EODHDProvider.cs..."
# Fix line 105: Add Source
sed -i '105s/Volume = vol/Volume = vol,\n                Source = "eodhd"/' EODHDProvider.cs

# Fix line 161: Add Source
sed -i '161s/Volume = vol/Volume = vol,\n                Source = "eodhd"/' EODHDProvider.cs

# Fix line 172: Remove AdjustedClose
sed -i '/AdjustedClose =/d' EODHDProvider.cs

# Fix logging errors (lines 117-118)
sed -i 's/_logger\.LogError(ex, "Failed to fetch historical data for/\_logger.LogError(ex, "Failed to fetch historical data for/g' EODHDProvider.cs

echo "Fixing PolygonProvider.cs..."
# Fix line 108: Add Source
sed -i '108s/Volume = vol/Volume = vol,\n                Source = "polygon"/' PolygonProvider.cs

# Fix line 164: Add Source
sed -i '164s/Volume = vol/Volume = vol,\n                Source = "polygon"/' PolygonProvider.cs

# Fix line 117 & 173: Remove AdjustedClose
sed -i '/AdjustedClose =/d' PolygonProvider.cs

# Fix logging errors
sed -i 's/_logger\.LogError(ex, "Failed to fetch historical data for/\_logger.LogError(ex, "Failed to fetch historical data for/g' Polygon Provider.cs

echo "Fixing CoinGeckoProvider.cs..."
# Fix line 139: Add Source
sed -i '139s/TradesCount = null/TradesCount = null,\n                Source = "coingecko"/' CoinGeckoProvider.cs

# Fix line 223: Add Source
sed -i '223s/TradesCount = null/TradesCount = null,\n                Source = "coingecko"/' CoinGeckoProvider.cs

# Fix line 148 & 232: Remove AdjustedClose
sed -i '/AdjustedClose =/d' CoinGeckoProvider.cs

echo "Fixing TwelveDataProvider.cs..."
# Fix line 112: Add Source
sed -i '112s/Volume = vol/Volume = vol,\n                Source = "twelvedata"/' TwelveDataProvider.cs

# Fix line 166: Add Source
sed -i '166s/Volume = vol/Volume = vol,\n                Source = "twelvedata"/' TwelveDataProvider.cs

# Fix line 121 & 176: Remove AdjustedClose
sed -i '/AdjustedClose =/d' TwelveDataProvider.cs

# Fix logging errors
sed -i 's/_logger\.LogError(ex, "Failed to fetch historical data for/\_logger.LogError(ex, "Failed to fetch historical data for/g' TwelveDataProvider.cs

echo "Done! Attempting build..."
cd /root/AlgoTrendy_v2.6/backend/AlgoTrendy.API
dotnet build
