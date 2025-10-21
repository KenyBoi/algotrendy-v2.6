---
description: Quick setup - Haiku + autonomous + AI context
---

# AI Development Quick Start

This command will configure your development environment for optimal AI-assisted development on AlgoTrendy.

## Configuration Steps

1. **Switch to Haiku 4.5** - Faster, cost-effective model for routine tasks
2. **Enable Autonomous Mode** - Full permissions (except file deletion) for efficient workflow
3. **Load AI Context** - Comprehensive project understanding

## Executing Configuration

Please execute the following slash commands in sequence:

/set-haiku
/gofree

## AI Context Summary

I have read and understand the AlgoTrendy v2.6 project context:

**Project Overview:**
- **AlgoTrendy v2.6** - Enterprise algorithmic trading platform
- **Tech Stack:** .NET 8.0 + QuestDB + React/TypeScript
- **Status:** Production Ready (98/100) | Security: 98.5/100 | Tests: 75% (306/407)
- **Location:** `/root/AlgoTrendy_v2.6`

**Key Components:**
- 5 Active Brokers: Binance, Bybit, Coinbase, IBKR, TradeStation
- 9+ Free Market Data Providers (saving $61K+/year)
- MEM AI System with 50+ professional indicators
- Institutional backtesting via QuantConnect
- Enterprise security with JWT + MFA/TOTP

**Architecture:**
```
backend/
├── AlgoTrendy.API/          # REST API + Controllers
├── AlgoTrendy.Core/         # Domain Models & Interfaces
├── AlgoTrendy.TradingEngine/ # Trading Logic & Brokers
├── AlgoTrendy.DataChannels/ # Market Data Channels
├── AlgoTrendy.Backtesting/  # Backtesting Engines
└── AlgoTrendy.Tests/        # xUnit Tests (306/407 passing)
```

**Important Files:**
- Full AI Context: `/root/AlgoTrendy_v2.6/docs/architecture/AI_CONTEXT.md` (732 lines)
- Quick Start: `/root/AlgoTrendy_v2.6/docs/ai-context/README.md`
- Main README: `/root/AlgoTrendy_v2.6/README.md`

**Disabled Features Needing Attention:**
1. CustomBacktestEngine - Needs accuracy validation
2. KrakenBroker - Package API mismatch, needs REST implementation

**Development Guidelines:**
- Always read files before editing
- Follow async/await patterns
- Add XML documentation
- Update tests for changes
- Check DI registration in Program.cs
- Run security scans before commits
- Use environment variables for secrets

## Ready to Work

Configuration complete! I'm now running on Haiku 4.5 in autonomous mode with full project context loaded.

**Quick Reference Commands:**
```bash
# Build project
cd /root/AlgoTrendy_v2.6/backend && dotnet build

# Run tests
dotnet test

# Run API
cd /root/AlgoTrendy_v2.6/backend/AlgoTrendy.API && dotnet run

# Security scan
cd /root/AlgoTrendy_v2.6/file_mgmt_code && ./scan-security.sh
```

What would you like me to help you with today?
