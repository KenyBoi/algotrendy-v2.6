# AlgoTrendy v2.6 - Software Architecture & Module Map

**Date:** 2025-10-21
**Purpose:** Complete architectural mapping for modular versioning strategy

---

## Table of Contents
- [Current Architecture Overview](#current-architecture-overview)
- [Module Breakdown](#module-breakdown)
- [Industry Standard Versioning](#industry-standard-versioning)
- [Recommended Modular Strategy](#recommended-modular-strategy)
- [Migration Path](#migration-path)

---

## Current Architecture Overview

### Architecture Style
**Current:** Modular Monolith with Clean Architecture
- Single deployable unit (API)
- Organized into logical modules/projects
- Shared database (QuestDB)
- Clear separation of concerns

### Project Structure
```
AlgoTrendy_v2.6/
├── backend/                    # .NET 8 Backend (Modular Monolith)
│   ├── AlgoTrendy.API/        # API Layer (Entry Point)
│   ├── AlgoTrendy.Core/       # Domain Models & Interfaces
│   ├── AlgoTrendy.TradingEngine/    # Trading Logic Module
│   ├── AlgoTrendy.DataChannels/     # Market Data Module
│   ├── AlgoTrendy.Backtesting/      # Backtesting Module
│   ├── AlgoTrendy.Infrastructure/   # Data Access & External Services
│   ├── AlgoTrendy.Common.Abstractions/ # Shared Abstractions
│   └── AlgoTrendy.Tests/      # Test Suite
├── backtesting-py-service/    # Python Backtesting Service
├── frontend/                  # React/TypeScript UI
├── MEM/                       # AI Memory System
├── ml-service/                # ML Training Service (Python)
└── database/                  # Database migrations & scripts
```

---

## Module Breakdown

### Backend Modules (C# .NET 8)

#### 1. **Core Module** (`AlgoTrendy.Core`)
**Version:** Independent
**Purpose:** Domain models, interfaces, enums
**Dependencies:** None (foundation)
**Stability:** High (changes rarely)

**Contents:**
- Models (Order, Position, MarketData, Trade)
- Interfaces (IBroker, IStrategy, IDataProvider)
- Enums (OrderStatus, OrderType, OrderSide)

**Versioning Strategy:** SemVer (1.0.0, 1.1.0, 2.0.0)

---

#### 2. **Trading Engine Module** (`AlgoTrendy.TradingEngine`)
**Version:** Independent
**Purpose:** Order execution, position management, risk
**Dependencies:** Core
**Stability:** Medium

**Contents:**
- Brokers (Binance, Bybit, Interactive Brokers, etc.)
- Order management
- Position tracking
- Risk management

**Versioning Strategy:** SemVer (1.0.0, 1.1.0, 2.0.0)

---

#### 3. **Data Channels Module** (`AlgoTrendy.DataChannels`)
**Version:** Independent
**Purpose:** Market data ingestion from exchanges
**Dependencies:** Core
**Stability:** Medium

**Contents:**
- REST data providers (Binance, OKX, Polygon, etc.)
- WebSocket streams
- Data orchestration

**Versioning Strategy:** SemVer (1.0.0, 1.1.0, 2.0.0)

---

#### 4. **Backtesting Module** (`AlgoTrendy.Backtesting`)
**Version:** Independent
**Purpose:** Historical strategy testing
**Dependencies:** Core, TradingEngine
**Stability:** Medium

**Contents:**
- Triple-engine system (Custom, QuantConnect, Local LEAN)
- Backtest execution
- Performance metrics

**Versioning Strategy:** SemVer (1.0.0, 1.1.0, 2.0.0)

---

#### 5. **Infrastructure Module** (`AlgoTrendy.Infrastructure`)
**Version:** Independent
**Purpose:** Data persistence, external services
**Dependencies:** Core
**Stability:** Low (changes frequently)

**Contents:**
- Repositories (QuestDB, TimescaleDB)
- External API clients
- Caching, logging

**Versioning Strategy:** SemVer (1.0.0, 1.1.0, 2.0.0)

---

#### 6. **API Module** (`AlgoTrendy.API`)
**Version:** Independent
**Purpose:** REST API & SignalR endpoints
**Dependencies:** All modules
**Stability:** Medium

**Contents:**
- Controllers (Trading, Orders, MarketData, etc.)
- Middleware
- Swagger/OpenAPI

**Versioning Strategy:** API Versioning (v1, v2, v3)

---

### Service Modules (Separate Deployables)

#### 7. **Backtesting-Py Service**
**Language:** Python
**Version:** Independent
**Purpose:** Python-based backtesting engine
**Communication:** REST API

**Versioning Strategy:** SemVer (1.0.0, 1.1.0, 2.0.0)

---

#### 8. **ML Training Service**
**Language:** Python
**Version:** Independent
**Purpose:** Machine learning model training
**Communication:** REST API

**Versioning Strategy:** SemVer (1.0.0, 1.1.0, 2.0.0)

---

#### 9. **Frontend Module**
**Language:** React/TypeScript
**Version:** Independent
**Purpose:** Web UI dashboard
**Communication:** REST API + SignalR

**Versioning Strategy:** SemVer (1.0.0, 1.1.0, 2.0.0)

---

## Industry Standard Versioning

### 1. **Semantic Versioning (SemVer)**
**Format:** MAJOR.MINOR.PATCH (e.g., 2.6.1)

```
MAJOR: Breaking changes (API incompatibility)
MINOR: New features (backward compatible)
PATCH: Bug fixes (backward compatible)
```

**Used By:** npm, NuGet, Maven, Go modules

---

### 2. **Calendar Versioning (CalVer)**
**Format:** YYYY.MM.MICRO (e.g., 2025.10.1)

**Used By:** Ubuntu, Python (pip), AWS

---

### 3. **Module/Package Versioning**
**Approach:** Each module has independent version

**Examples:**
- NuGet packages: `AlgoTrendy.Core v1.2.0`, `AlgoTrendy.TradingEngine v1.5.3`
- npm packages: `@algotrendy/core v2.1.0`
- Maven: `com.algotrendy:trading-engine:1.4.2`

**Used By:** Microservices, multi-repo systems

---

### 4. **API Versioning**
**Approach:** Version API endpoints separately

**Methods:**
- **URI Versioning:** `/api/v1/orders`, `/api/v2/orders`
- **Header Versioning:** `API-Version: 2.0`
- **Query Param:** `/api/orders?version=2`

**Used By:** REST APIs (Stripe, GitHub, AWS)

---

## Recommended Modular Strategy for AlgoTrendy

### **Hybrid Approach: Modular Monolith + Independent Services**

#### Phase 1: Current State (Modular Monolith)
```
AlgoTrendy.API v2.6.0
├── AlgoTrendy.Core v1.0.0
├── AlgoTrendy.TradingEngine v1.0.0
├── AlgoTrendy.DataChannels v1.0.0
├── AlgoTrendy.Backtesting v1.0.0
└── AlgoTrendy.Infrastructure v1.0.0
```

**Deployment:** Single unit (one Docker container)
**Versioning:** Umbrella version (v2.6.0) + module versions

---

#### Phase 2: Extract High-Change Modules (Recommended Next Step)
```
Services:
├── AlgoTrendy.API v2.6.0
│   ├── Core v1.0.0
│   ├── TradingEngine v1.0.0
│   └── Infrastructure v1.0.0
├── DataChannels Service v1.0.0 (separate)
├── Backtesting Service v1.0.0 (separate)
├── ML Service v1.0.0 (separate - already done!)
└── Frontend v1.0.0 (separate)
```

**Deployment:** 5 services (Docker Compose / Kubernetes)
**Versioning:** Each service independently versioned

---

#### Phase 3: Full Microservices (Future)
```
Services:
├── Trading Service v1.0.0
├── Market Data Service v1.0.0
├── Backtesting Service v1.0.0
├── ML Service v1.0.0
├── API Gateway v1.0.0
└── Frontend v1.0.0
```

**Deployment:** 6+ services (Kubernetes recommended)
**Versioning:** Complete independence

---

## Module Versioning Strategy

### **Recommended: NuGet Package Approach**

Each C# module becomes a NuGet package:

```bash
# Core module
AlgoTrendy.Core v1.2.0

# Trading module depends on Core
AlgoTrendy.TradingEngine v1.5.3
  └── requires: AlgoTrendy.Core >= 1.0.0

# API depends on all
AlgoTrendy.API v2.6.0
  ├── requires: AlgoTrendy.Core >= 1.2.0
  ├── requires: AlgoTrendy.TradingEngine >= 1.5.0
  ├── requires: AlgoTrendy.DataChannels >= 1.3.0
  └── requires: AlgoTrendy.Backtesting >= 1.1.0
```

### **Benefits:**
1. **Independent versioning** - Update TradingEngine without touching Core
2. **Clear dependencies** - NuGet manages version constraints
3. **Rollback capability** - Downgrade specific modules
4. **CI/CD friendly** - Build only changed modules
5. **Multiple versions** - Different projects use different versions

### **Implementation:**

#### 1. Add Version to Each .csproj
```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net8.0</TargetFramework>
    <Version>1.2.0</Version>
    <PackageId>AlgoTrendy.Core</PackageId>
    <Authors>AlgoTrendy Team</Authors>
    <Description>Core domain models and interfaces</Description>
  </PropertyGroup>
</Project>
```

#### 2. Create NuGet Packages
```bash
dotnet pack AlgoTrendy.Core/AlgoTrendy.Core.csproj -c Release
# Creates: AlgoTrendy.Core.1.2.0.nupkg
```

#### 3. Version Dependencies
```xml
<ItemGroup>
  <PackageReference Include="AlgoTrendy.Core" Version="1.2.0" />
</ItemGroup>
```

---

## Versioning Decision Matrix

| Scenario | Monolith Version | Module Version | Service Version |
|----------|------------------|----------------|-----------------|
| Bug fix in Trading | 2.6.1 | TradingEngine 1.0.1 | - |
| New broker added | 2.7.0 | TradingEngine 1.1.0 | - |
| Breaking API change | 3.0.0 | API 3.0.0 | - |
| New ML algorithm | 2.6.0 | - | ML Service 1.2.0 |
| UI redesign | 2.6.0 | - | Frontend 2.0.0 |

---

## Migration Path (Recommended Steps)

### **Step 1: Add Module Versioning (Now)**
- Add `<Version>` to all .csproj files
- Start tracking module changes independently
- Create CHANGELOG.md per module

**Time:** 2-4 hours
**Impact:** Low (documentation only)

### **Step 2: Implement NuGet Packaging (Next Sprint)**
- Configure NuGet package generation
- Set up private NuGet feed (GitHub Packages or Azure Artifacts)
- Convert projects to use NuGet references

**Time:** 1-2 days
**Impact:** Medium (build process changes)

### **Step 3: Extract Data Channels Service (Phase 8)**
- Move DataChannels to separate service
- Add REST API for data ingestion
- Update API to consume DataChannels service

**Time:** 1-2 weeks
**Impact:** High (architecture change)

### **Step 4: Extract Backtesting Service (Phase 9)**
- Already have backtesting-py-service
- Extract C# backtesting to separate service
- Unified backtesting API

**Time:** 1-2 weeks
**Impact:** High

---

## Recommended Immediate Actions

1. **Add version properties to all .csproj files**
2. **Create per-module CHANGELOG.md files**
3. **Document dependencies between modules**
4. **Set up automated version bumping (CI/CD)**
5. **Create version compatibility matrix**

Would you like me to:
- Implement Step 1 (add versioning to .csproj files)?
- Create module dependency diagram?
- Set up automated changelog generation?

---

## References

- [Semantic Versioning Specification](https://semver.org/)
- [Microsoft NuGet Versioning](https://learn.microsoft.com/en-us/nuget/concepts/package-versioning)
- [Microservices Versioning Strategies](https://martinfowler.com/articles/microservice-trade-offs.html#distribution)
- [API Versioning Best Practices](https://restfulapi.net/versioning/)
