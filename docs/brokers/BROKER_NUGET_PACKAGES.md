# Required NuGet Packages for New Brokers

Run these commands to install the required NuGet packages for the 5 new broker integrations:

## 1. Alpaca (Stocks/Options)
```bash
cd /root/AlgoTrendy_v2.6/backend/AlgoTrendy.TradingEngine
dotnet add package Alpaca.Markets --version 7.1.2
```

## 2. Kraken (Crypto)
```bash
dotnet add package Kraken.Net --version 5.0.0
```

## 3. OKX (Crypto)
```bash
dotnet add package OKX.Net --version 2.0.0
```

## 4. Coinbase Advanced Trade (Crypto)
```bash
dotnet add package CoinbaseAdvanced.Net --version 1.0.0
```

## 5. Crypto.com Exchange (Crypto)
```bash
dotnet add package CryptoCom.Net --version 1.0.0
```

## Alternative: Install All at Once
```bash
cd /root/AlgoTrendy_v2.6/backend/AlgoTrendy.TradingEngine

dotnet add package Alpaca.Markets --version 7.1.2 && \
dotnet add package Kraken.Net --version 5.0.0 && \
dotnet add package OKX.Net --version 2.0.0 && \
dotnet add package CoinbaseAdvanced.Net --version 1.0.0 && \
dotnet add package CryptoCom.Net --version 1.0.0
```

## Verification
After installation, verify packages are added:
```bash
dotnet list package
```

## Notes
- All packages use CryptoExchange.Net as a base library (already included with Binance.Net)
- Alpaca.Markets is the official Alpaca SDK for .NET
- Version numbers may need updating to latest stable releases
- Some packages (CoinbaseAdvanced.Net, CryptoCom.Net) may need alternative libraries if these don't exist

## Alternative Libraries (if needed)

If the above packages don't exist or have issues:

### Coinbase:
- `Coinbase.Pro` or `CoinbasePro.Net`
- Manual REST API implementation

### Crypto.com:
- Manual REST API implementation using HttpClient
- Crypto.com official API documentation: https://exchange-docs.crypto.com/

Run `dotnet restore` after adding packages.
