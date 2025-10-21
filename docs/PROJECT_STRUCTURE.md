# AlgoTrendy v2.6 - Project Structure Guide

**Last Updated**: October 21, 2025
**Status**: ✅ Organized & Production Ready

---

## Overview

This document describes the complete project structure following computer science best practices. All code, documentation, and resources are organized by function and lifecycle stage.

---

## Top-Level Directory Structure

```
/root/AlgoTrendy_v2.6/
│
├── 📁 Core Application
│   ├── backend/              # C# .NET Backend API
│   ├── frontend/             # React TypeScript Frontend
│   ├── MEM/                  # Memory-Enhanced Machine Learning AI System
│   └── database/             # Database migrations & schemas
│
├── 📁 Strategies & Trading
│   └── strategies/           # Trading Strategies (organized by lifecycle)
│       ├── development/
│       ├── backtested/
│       ├── production/
│       └── archive/
│
├── 📁 Services & Infrastructure
│   ├── services/             # Microservices
│   │   ├── backtesting/
│   │   ├── risk-management/
│   │   ├── ml-service/
│   │   ├── api-gateway/
│   │   ├── data-service/
│   │   └── trading-service/
│   └── infrastructure/       # Infrastructure as Code
│       ├── ansible/
│       ├── docker/
│       ├── kubernetes/
│       ├── terraform/
│       ├── monitoring/
│       ├── certbot/
│       └── ssl/
│
├── 📁 Data & Assets
│   └── data/                 # Centralized Data Storage
│       ├── strategy_registry/
│       ├── questdb/
│       ├── ml_models/
│       ├── mem_knowledge/
│       └── benchmarks/
│
├── 📁 Documentation
│   └── docs/                 # ALL PROJECT DOCUMENTATION
│       ├── architecture/
│       ├── api/
│       ├── deployment/
│       ├── developer/
│       ├── planning/
│       ├── reports/
│       ├── security/
│       ├── testing/
│       ├── user-guides/
│       ├── ai-context/
│       ├── prompts/
│       ├── troubleshooting/
│       └── frontend/
│
├── 📁 Tools & Utilities
│   ├── scripts/              # Utility scripts
│   ├── devtools/             # Development tools
│   │   └── version-upgrade/
│   └── integrations/         # Third-party integrations
│
├── 📁 Archives & Backups
│   ├── archive/              # Archived/deprecated code
│   ├── backups/              # Backups
│   └── logs/                 # Runtime logs
│
└── 📁 Configuration
    ├── .github/              # GitHub Actions CI/CD
    ├── .vscode/              # VS Code config
    ├── .claude/              # Claude Code config
    ├── .qodo/                # Qodo config
    └── .dev/                 # Development temp files
```

---

## Detailed Directory Descriptions

### Core Application

#### `/backend/`
**C# .NET 8 Backend API**

Key components:
- `AlgoTrendy.API/` - Main API project
- `AlgoTrendy.Core/` - Business logic & services
  - `Services/StrategyRegistry/` - Strategy registry system
- `AlgoTrendy.TradingEngine/` - Trading execution engine
- `AlgoTrendy.Infrastructure/` - Data access & external services

See: [Backend Architecture](/docs/architecture/ARCHITECTURE.md)

---

#### `/frontend/`
**React TypeScript Frontend**

Modern trading dashboard with:
- Real-time portfolio tracking
- Strategy management
- Risk monitoring
- Trade execution

See: [Frontend Documentation](/docs/frontend/)

---

#### `/MEM/`
**Memory-Enhanced Machine Learning System**

AI-powered trading enhancements:
- 78% ML prediction accuracy
- Persistent memory across trades
- Real-time pattern recognition
- Strategy enhancement layer

See: [MEM Documentation](/MEM/README.md)

---

#### `/database/`
**Database Migrations & Schemas**

- Liquibase migrations
- SQL schemas
- Seed data
- Migration scripts

---

### Strategies & Trading

#### `/strategies/`
**Organized by Lifecycle Stage**

```
strategies/
├── development/
│   └── strategy_research_2025_q4/     # Current research project
│       ├── strategy_1_volatility_managed_momentum/
│       ├── strategy_2_pairs_trading/
│       ├── strategy_3_carry_trade/
│       └── reports/
├── backtested/                         # Validated strategies
├── production/                         # Live trading
└── archive/                            # Deprecated
```

**Lifecycle**: development → backtested → production

See: [Strategies README](/strategies/README.md)

---

### Services & Infrastructure

#### `/services/`
**Microservices Architecture**

- `backtesting/` - Python backtesting service
- `risk-management/` - Multi-broker risk management
- `ml-service/` - Machine learning service
- `api-gateway/` - API gateway
- `data-service/` - Data aggregation
- `trading-service/` - Trade execution

See: [Services Documentation](/services/README.md)

---

#### `/infrastructure/`
**Infrastructure as Code**

- `ansible/` - Configuration management
- `docker/` - Docker configurations
- `kubernetes/` - K8s deployments
- `terraform/` - Cloud infrastructure
- `monitoring/` - Prometheus, Grafana
- `certbot/` - SSL certificates
- `ssl/` - SSL configurations

See: [Deployment Guide](/docs/deployment/DEPLOYMENT_GUIDE.md)

---

### Data & Assets

#### `/data/`
**Centralized Data Storage**

```
data/
├── strategy_registry/      # Strategy metadata & performance
│   ├── metadata/
│   └── performance/
├── questdb/                # QuestDB time-series data
├── ml_models/              # Trained ML models
├── mem_knowledge/          # MEM persistent memory
└── benchmarks/             # Performance benchmarks
```

**Note**: All services access data from this centralized location.

---

### Documentation

#### `/docs/`
**Complete Project Documentation**

Organized by category:

- **`architecture/`** - System architecture, designs, AI context
- **`api/`** - API documentation, Postman collections, Swagger
- **`deployment/`** - Deployment guides, Docker setup
- **`developer/`** - Developer onboarding, coding standards
- **`planning/`** - Research, roadmaps, decisions
- **`reports/`** - Session summaries, validation reports, analysis
- **`security/`** - Security documentation, compliance
- **`testing/`** - Test documentation, strategies
- **`user-guides/`** - User documentation, quick starts
- **`frontend/`** - Frontend-specific documentation
- **`ai-context/`** - AI/ML context and training data
- **`prompts/`** - AI prompts and templates
- **`troubleshooting/`** - Troubleshooting guides

See: [Documentation Index](/docs/README.md)

---

### Tools & Utilities

#### `/scripts/`
Utility scripts for various operations

#### `/devtools/`
Development tools and utilities
- `version-upgrade/` - Version upgrade tools

#### `/integrations/`
Third-party integrations (brokers, data providers)

---

### Archives & Backups

#### `/archive/`
**Archived/Deprecated Code**

Contains:
- `strategyGrpDev02_archived_20251021/` - Old strategy folder
- `file_mgmt_code_archived_20251021/` - File management utilities
- `tfvc_projects_archived_20251021/` - TFVC projects
- `planning_archived_20251021/` - Old planning docs
- `reports_archived_20251021/` - Old reports
- `legacy_reference/` - Legacy reference code

#### `/backups/`
System backups

#### `/logs/`
Runtime logs

---

## Configuration Files

### Root Directory Files

**Essential Only** (following best practices):

#### Configuration
- `.env` - Environment variables (gitignored)
- `.env.example` - Example environment config
- `.env.production.template` - Production config template
- `.gitignore` - Git ignore rules
- `.gitattributes` - Git attributes
- `.editorconfig` - Editor configuration
- `.clauderc` - Claude Code config
- `.markdownlint.json` - Markdown linting rules
- `.releaserc.json` - Semantic release config
- `GitVersion.yml` - Git version configuration
- `renovate.json` - Dependency updates

#### Docker
- `docker-compose.yml` - Development docker compose
- `docker-compose.prod.yml` - Production docker compose
- `docker-compose.modular.yml` - Modular architecture compose
- `nginx.conf` - Nginx configuration

#### Package Management
- `package.json` - Node.js dependencies
- `requirements.txt` - Python dependencies

#### Documentation
- `README.md` - Main project README
- `CHANGELOG.md` - Project changelog
- `CONTRIBUTING.md` - Contribution guidelines

---

## File Organization Principles

### 1. Separation of Concerns
- Code, data, docs, and infrastructure are clearly separated
- Each top-level folder has a specific purpose

### 2. Lifecycle-Based Organization
- Strategies organized by stage: dev → backtest → prod
- Easy to promote code through pipeline

### 3. Centralized Data
- All data in `/data/` directory
- Shared access across services
- Easy backup and migration

### 4. Comprehensive Documentation
- All docs in `/docs/` with clear categories
- Easy to find and maintain
- Version controlled with code

### 5. Clean Root Directory
- Minimal files at root level
- Only essential configs and docs
- Easy to navigate

---

## Navigation Tips

### Finding Things Quickly

**Looking for...**

| What | Where |
|------|-------|
| Strategy code | `/strategies/development/` |
| Strategy registry | `/backend/AlgoTrendy.Core/Services/StrategyRegistry/` |
| API documentation | `/docs/api/` |
| Deployment guide | `/docs/deployment/` |
| Architecture docs | `/docs/architecture/` |
| Test reports | `/docs/reports/validation/` |
| Planning docs | `/docs/planning/` |
| MEM system | `/MEM/` |
| Services | `/services/` |
| Infrastructure | `/infrastructure/` |
| Archived code | `/archive/` |

---

## Best Practices

### When Adding New Files

1. **Determine Category**: Is it code, docs, data, or infrastructure?
2. **Follow Structure**: Place in appropriate folder
3. **Create README**: If starting new folder, add README
4. **Update Docs**: Update relevant documentation
5. **Version Control**: Commit with clear message

### When Moving Files

1. **Update References**: Search for imports/paths
2. **Test After Move**: Ensure builds still work
3. **Update Docker**: Check volume mounts
4. **Update Docs**: Reflect changes in documentation

### When Archiving

1. **Copy First**: Copy to `/archive/` before removing
2. **Add Date**: Use format `_archived_YYYYMMDD`
3. **Document**: Note what was archived and why
4. **Verify**: Ensure nothing depends on archived code

---

## Related Documentation

- [Strategies Organization](/strategies/README.md)
- [Strategy Registry](/backend/AlgoTrendy.Core/Services/StrategyRegistry/README.md)
- [Documentation Index](/docs/README.md)
- [MEM System](/MEM/README.md)
- [Deployment Guide](/docs/deployment/DEPLOYMENT_GUIDE.md)

---

## Maintenance

This structure should be maintained as the project grows:

- ✅ Keep root directory clean (<25 files)
- ✅ All new docs go in `/docs/` with proper category
- ✅ Strategies follow lifecycle progression
- ✅ Services stay in `/services/`
- ✅ Infrastructure code in `/infrastructure/`
- ✅ Regular archiving of deprecated code

---

**Last Reviewed**: October 21, 2025
**Maintained by**: Development Team
**Status**: ✅ Production Ready
