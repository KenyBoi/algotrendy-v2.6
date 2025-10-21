# Changelog - AlgoTrendy Platform

All notable changes to the AlgoTrendy Trading Platform will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

### Added
- Automated release infrastructure with semantic-release
- Module versioning with GitVersion
- GitHub Actions workflows for CI/CD
- Auto-sync workflow for parallel architecture (Phase 3)

## [2.6.0] - 2025-10-21

### Added
- Complete modular architecture with 8 independent modules
- Multi-broker support (Binance, Bybit, Coinbase, TradeStation, NinjaTrader, Interactive Brokers)
- Real-time market data channels (Alpaca, CoinGecko, EODHD, Polygon, Tiingo, TwelveData)
- Backtesting engines (QuantConnect, BacktestingPy, Custom)
- ML training integration
- QuestDB time-series database integration
- Azure Key Vault secrets management
- JWT authentication and authorization
- SignalR WebSocket support for real-time data
- Comprehensive API documentation with Swagger/OpenAPI
- Security enhancements and dependency updates

### Security
- Updated RestSharp to 112.0.0 (fixing CVE vulnerabilities)
- Implemented rate limiting middleware
- Added CORS security policies
- Enhanced credential management with Azure Key Vault

### Documentation
- Comprehensive architecture documentation
- Parallel architecture strategy guide
- Version management tooling guide
- Multi-AI build plan
- Deployment guides
- Security setup guides

## Module Changelogs

For detailed changes to individual modules, see:
- [AlgoTrendy.Core](./backend/AlgoTrendy.Core/CHANGELOG.md)
- [AlgoTrendy.TradingEngine](./backend/AlgoTrendy.TradingEngine/CHANGELOG.md)
- [AlgoTrendy.DataChannels](./backend/AlgoTrendy.DataChannels/CHANGELOG.md)
- [AlgoTrendy.Backtesting](./backend/AlgoTrendy.Backtesting/CHANGELOG.md)
- [AlgoTrendy.Infrastructure](./backend/AlgoTrendy.Infrastructure/CHANGELOG.md)
- [AlgoTrendy.API](./backend/AlgoTrendy.API/CHANGELOG.md)
- [AlgoTrendy.Common.Abstractions](./backend/AlgoTrendy.Common.Abstractions/CHANGELOG.md)
- [AlgoTrendy.Tests](./backend/AlgoTrendy.Tests/CHANGELOG.md)
