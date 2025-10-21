# AlgoTrendy Modular Architecture - Quick Start Guide

**Goal**: Practical, actionable steps to start modularizing AlgoTrendy

---

## Quick Reference

### Industry Standard Answer

**Q: What's the industry standard for module organization?**

**A: Modular Microservices (Hybrid Approach)**

Most successful trading platforms and FinTech companies use:

1. **Netflix**: Full microservices (600+ services)
2. **Coinbase**: Service-oriented with domain-driven design
3. **Robinhood**: Modular monolith evolving to microservices
4. **Interactive Brokers**: SOA with independent trading modules
5. **QuantConnect**: Modular architecture with pluggable strategies

**Recommendation for AlgoTrendy**: **Modular Microservices**
- Start as modular monolith
- Independent module versioning from Day 1
- Extract services incrementally based on need

---

## Module vs. Microservice Decision Tree

```
Do you need independent scaling?
├── Yes → Extract as Microservice
│   ├── Market Data (high throughput)
│   ├── ML Predictions (GPU requirements)
│   └── Analytics (CPU intensive)
│
└── No → Keep as Module
    ├── Core (shared by all)
    ├── Portfolio (business logic)
    └── API Gateway (orchestration)
```

---

## Versioning Quick Reference

### Semantic Versioning Cheat Sheet

```
v1.2.3
  │ │ └─ PATCH: Bug fixes only (1.2.3 → 1.2.4)
  │ └─── MINOR: New features, backward-compatible (1.2.3 → 1.3.0)
  └───── MAJOR: Breaking changes (1.2.3 → 2.0.0)
```

### When to Bump Version

| Change Type | Example | Version Change |
|-------------|---------|----------------|
| Bug fix | Fix null reference error | 1.2.3 → 1.2.4 |
| New feature | Add Bybit support | 1.2.4 → 1.3.0 |
| Deprecation | Mark old API as deprecated | 1.3.0 → 1.4.0 |
| Breaking change | Remove old API | 1.4.0 → 2.0.0 |
| Refactoring (internal) | Improve performance | 1.4.0 → 1.4.1 |

### Conventional Commits

```bash
# Feature (bumps MINOR)
git commit -m "feat(market-data): add Bybit websocket support"

# Bug fix (bumps PATCH)
git commit -m "fix(trading): resolve order timeout issue"

# Breaking change (bumps MAJOR)
git commit -m "feat(core)!: change Position interface

BREAKING CHANGE: Position.Id is now string instead of Guid"

# Chore (no version bump)
git commit -m "chore: update dependencies"
```

---

## Step-by-Step Migration (Phase 1)

### Step 1: Create Module Structure (Day 1)

```bash
cd /root/AlgoTrendy_v2.6/backend

# Create new modular structure
mkdir -p modules/{Core,MarketData,TradingEngine,Backtesting,Portfolio,Analytics}

# Move existing projects (don't delete originals yet)
cp -r AlgoTrendy.Core/* modules/Core/
cp -r AlgoTrendy.DataChannels/* modules/MarketData/
cp -r AlgoTrendy.TradingEngine/* modules/TradingEngine/
cp -r AlgoTrendy.Backtesting/* modules/Backtesting/
cp -r AlgoTrendy.Infrastructure/* modules/Portfolio/  # Split later
```

### Step 2: Add Version Files (Day 1)

Create `version.json` in each module:

```bash
# modules/MarketData/version.json
{
  "version": "1.0.0",
  "compatibleWith": {
    "core": "^3.0.0"
  },
  "apiVersion": "v1"
}
```

Create `Directory.Build.props` in each module:

```xml
<!-- modules/MarketData/Directory.Build.props -->
<Project>
  <PropertyGroup>
    <ModuleName>MarketData</ModuleName>
    <ModuleVersion>1.0.0</ModuleVersion>
    <CoreCompatibility>3.0.0</CoreCompatibility>
  </PropertyGroup>
</Project>
```

### Step 3: Update .csproj Files (Day 2)

```xml
<!-- modules/MarketData/AlgoTrendy.MarketData.csproj -->
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net8.0</TargetFramework>
    <PackageId>AlgoTrendy.MarketData</PackageId>
    <Version>1.0.0</Version>
    <Description>Market data aggregation module</Description>
    <Authors>AlgoTrendy Team</Authors>
    <PackageTags>algotrendy;market-data;trading</PackageTags>
  </PropertyGroup>

  <ItemGroup>
    <!-- Core dependency with version range -->
    <ProjectReference Include="../Core/AlgoTrendy.Core.csproj" />
    <!-- Or use NuGet package -->
    <!-- <PackageReference Include="AlgoTrendy.Core" Version="3.*" /> -->
  </ItemGroup>
</Project>
```

### Step 4: Set Up Git Tags (Day 2)

```bash
# Tag each module with initial version
git tag core-v3.0.0
git tag market-data-v1.0.0
git tag trading-engine-v2.0.0
git tag backtesting-v1.0.0
git tag portfolio-v1.0.0
git tag analytics-v1.0.0

# Create platform bundle tag
git tag platform-v3.0.0

# Push tags
git push origin --tags
```

### Step 5: Update CI/CD (Day 3)

```yaml
# .github/workflows/build-modules.yml
name: Build Modules

on:
  push:
    branches: [main, develop]
  pull_request:
    branches: [main]

jobs:
  detect-changes:
    runs-on: ubuntu-latest
    outputs:
      core: ${{ steps.filter.outputs.core }}
      market-data: ${{ steps.filter.outputs.market-data }}
      trading: ${{ steps.filter.outputs.trading }}
    steps:
      - uses: actions/checkout@v4
      - uses: dorny/paths-filter@v2
        id: filter
        with:
          filters: |
            core:
              - 'modules/Core/**'
            market-data:
              - 'modules/MarketData/**'
            trading:
              - 'modules/TradingEngine/**'

  build-core:
    needs: detect-changes
    if: needs.detect-changes.outputs.core == 'true'
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v4
      - name: Build Core
        run: dotnet build modules/Core/
      - name: Test Core
        run: dotnet test modules/Core.Tests/

  build-market-data:
    needs: detect-changes
    if: needs.detect-changes.outputs.market-data == 'true'
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v4
      - name: Build Market Data
        run: dotnet build modules/MarketData/
      - name: Test Market Data
        run: dotnet test modules/MarketData.Tests/
```

---

## Example: Module Release Workflow

### Scenario: Adding Bybit Support to Market Data

#### Day 1: Implement Feature

```bash
# Create feature branch
git checkout -b feature/bybit-support

# Make changes
# ... implement Bybit connector ...

# Commit with conventional commit
git add modules/MarketData/
git commit -m "feat(market-data): add Bybit spot trading support

- Add BybitRestChannel
- Add BybitWebSocketChannel
- Add integration tests"

git push origin feature/bybit-support
```

#### Day 2: Create PR & Review

```markdown
**PR Title**: feat(market-data): Add Bybit Support

**Description**:
Adds Bybit exchange support for spot trading.

**Module**: Market Data
**Current Version**: 1.4.2
**New Version**: 1.5.0 (MINOR bump - new feature)

**Changes**:
- ✅ BybitRestChannel for REST API
- ✅ BybitWebSocketChannel for real-time data
- ✅ Integration tests
- ✅ Documentation updated

**Breaking Changes**: None

**Tested With**:
- Core v3.1.0
- API Gateway v3.1.5
```

#### Day 3: Merge & Release

```bash
# Merge to main
git checkout main
git pull
git merge feature/bybit-support

# Auto-versioning via GitVersion detects "feat:" commit
# New version: 1.5.0

# Create tag
git tag market-data-v1.5.0 -m "Market Data v1.5.0: Add Bybit support"
git push origin market-data-v1.5.0

# CI/CD automatically:
# 1. Builds module
# 2. Runs tests
# 3. Creates NuGet package (AlgoTrendy.MarketData.1.5.0.nupkg)
# 4. Builds Docker image (algotrendy/market-data:1.5.0)
# 5. Publishes to package registry
```

#### Day 4: Update Dependencies

```bash
# Other modules update their dependency
# portfolio module's .csproj
<PackageReference Include="AlgoTrendy.MarketData" Version="1.5.0" />

# Or use version range to auto-update
<PackageReference Include="AlgoTrendy.MarketData" Version="1.*" />
```

---

## Example: Platform Release

### Scenario: Quarterly Platform Release v3.2.0

```bash
# 1. Check module versions
cat > platform-v3.2.0-manifest.json <<EOF
{
  "platform": "3.2.0",
  "modules": {
    "core": "3.2.0",
    "market-data": "1.5.2",
    "trading-engine": "2.1.5",
    "backtesting": "1.2.3",
    "portfolio": "1.3.1",
    "analytics": "1.2.0",
    "ml-predictions": "0.9.8",
    "api-gateway": "3.2.0",
    "web-ui": "2.5.0"
  }
}
EOF

# 2. Generate release notes
git cliff --tag platform-v3.2.0 > RELEASE_NOTES.md

# 3. Create platform tag
git tag platform-v3.2.0 -F RELEASE_NOTES.md

# 4. CI/CD builds "all-in-one" Docker image
docker build -t algotrendy/platform:3.2.0 .

# 5. Update production docker-compose.yml
services:
  algotrendy:
    image: algotrendy/platform:3.2.0
    # OR use individual module images:
    # image: algotrendy/market-data:1.5.2
```

---

## Tools Setup

### Install Git Version

```bash
# Install GitVersion globally
dotnet tool install --global GitVersion.Tool

# Create gitversion.yml at repo root
cat > gitversion.yml <<EOF
mode: Mainline
branches:
  main:
    mode: ContinuousDelivery
    increment: Patch
  develop:
    mode: ContinuousDeployment
  feature:
    increment: Minor
  hotfix:
    increment: Patch
tag-prefix: '[a-z-]+-v'
major-version-bump-message: 'BREAKING CHANGE'
minor-version-bump-message: '^(feat|feature)'
patch-version-bump-message: '^(fix|perf|refactor)'
EOF

# Get current version
gitversion /showvariable SemVer
```

### Install Conventional Commits Hook

```bash
# Install commitlint
npm install -g @commitlint/cli @commitlint/config-conventional

# Create config
cat > .commitlintrc.json <<EOF
{
  "extends": ["@commitlint/config-conventional"],
  "rules": {
    "scope-enum": [
      2,
      "always",
      ["core", "market-data", "trading", "backtesting", "portfolio", "analytics"]
    ]
  }
}
EOF

# Add git hook
cat > .git/hooks/commit-msg <<'EOF'
#!/bin/sh
npx commitlint --edit "$1"
EOF
chmod +x .git/hooks/commit-msg
```

### Install Changelog Generator

```bash
# Install git-cliff
cargo install git-cliff

# Or use pre-built binary
wget https://github.com/orhun/git-cliff/releases/download/v1.4.0/git-cliff-1.4.0-x86_64-unknown-linux-gnu.tar.gz
tar -xzf git-cliff-*.tar.gz
sudo mv git-cliff /usr/local/bin/

# Create cliff.toml
cat > cliff.toml <<EOF
[changelog]
header = """
# Changelog\n
All notable changes to this project will be documented in this file.\n
"""
body = """
{% for group, commits in commits | group_by(attribute="group") %}
    ### {{ group | upper_first }}
    {% for commit in commits %}
        - {{ commit.message | upper_first }} ({{ commit.id | truncate(length=7, end="") }})
    {% endfor %}
{% endfor %}
"""

[git]
conventional_commits = true
filter_unconventional = true
EOF

# Generate changelog
git cliff --tag market-data-v1.5.0 --output modules/MarketData/CHANGELOG.md
```

---

## FAQ

### Q: Should every module have its own Git repository?

**A: No, use a monorepo.**

Pros of monorepo:
- ✅ Easier refactoring across modules
- ✅ Atomic commits across modules
- ✅ Single CI/CD pipeline
- ✅ Simplified dependency management

Use multiple repos only if:
- Different teams with no overlap
- Different release cadences (unlikely for trading platform)
- Different security requirements

**Recommendation**: Keep all modules in one repo with module-specific tags.

---

### Q: How do I version breaking changes?

**A: Increment MAJOR version and support both versions.**

```csharp
// Example: Position interface changes in Core v4.0.0

// Old interface (Core v3.x)
public interface IPosition
{
    Guid Id { get; }
}

// New interface (Core v4.x)
public interface IPosition
{
    string PositionId { get; }  // BREAKING: Changed from Guid to string
}

// Migration strategy: Support both for 2 versions
public class Position : IPosition
{
    [Obsolete("Use PositionId instead. Will be removed in v5.0.0")]
    public Guid Id => Guid.Parse(PositionId);

    public string PositionId { get; set; }
}
```

**Version Timeline**:
- v3.5.0: Introduce new `PositionId`, deprecate `Id`
- v4.0.0: Both properties exist, `Id` marked obsolete
- v5.0.0: Remove `Id`, only `PositionId` remains

---

### Q: How do I test module compatibility?

**A: Use integration tests with version matrix.**

```csharp
// modules/MarketData.Tests/Compatibility/CoreCompatibilityTests.cs

[Theory]
[InlineData("3.0.0")]
[InlineData("3.1.0")]
[InlineData("3.2.0")]
public async Task MarketData_Should_Work_With_Core(string coreVersion)
{
    // Arrange
    var marketDataService = new MarketDataService();
    var corePosition = CreatePositionFromCoreVersion(coreVersion);

    // Act
    var data = await marketDataService.GetDataForPosition(corePosition);

    // Assert
    Assert.NotNull(data);
}
```

---

### Q: When should I extract a module to a standalone service?

**A: When you have operational independence needs.**

Extract when:
1. **Scaling**: Module needs independent scaling (e.g., market data handles 1000 req/sec, others handle 10 req/sec)
2. **Technology**: Module needs different tech stack (Python ML vs .NET trading)
3. **Deployment**: Module needs independent deployment schedule
4. **Team**: Different team owns the module
5. **Resource**: Module has unique resource needs (GPU for ML)

Don't extract when:
- Just for "microservices" sake
- Module is rarely changed
- Module has tight coupling to others
- Team is small (<10 people)

---

### Q: What's the difference between module version and API version?

**A: Module version is SemVer, API version is endpoint versioning.**

```
Module Version: market-data v1.5.2 (package/container version)
API Version: /api/v1/market-data (HTTP endpoint version)

Module v1.5.2 might support API v1 and v2 simultaneously.
```

Example:
```csharp
[ApiVersion("1.0")]
[ApiVersion("2.0")]
[Route("api/v{version:apiVersion}/market-data")]
public class MarketDataController : ControllerBase
{
    // Module version: 1.5.2
    // Supports API v1 and v2
}
```

---

## Next Steps

1. ✅ Read `MODULAR_ARCHITECTURE_STRATEGY.md` (comprehensive guide)
2. 🚀 Start Phase 1: Reorganize code into modules (Week 1)
3. 📦 Set up module versioning (Week 2)
4. 🔧 Install tools (GitVersion, commitlint, git-cliff) (Week 2)
5. 🧪 Create first module release (Week 3)
6. 📊 Measure and iterate (Ongoing)

**Start Small**: Begin with just Market Data module as a pilot.

---

**Questions?** Review the full strategy document or create an issue in the repo.
