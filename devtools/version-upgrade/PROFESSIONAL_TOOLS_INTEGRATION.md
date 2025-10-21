# Professional Open-Source Version Management Tools Integration

**Purpose:** Integrate industry-standard open-source tools with AlgoTrendy's custom upgrade framework
**Created:** October 18, 2025
**Stack:** .NET 8 + Python 3.11+ + Next.js 15 + QuestDB + PostgreSQL

---

## 🎯 Integration Strategy

We're adding **professional open-source tools** alongside our existing custom tools:

```
┌─────────────────────────────────────────────────────────────────┐
│  CUSTOM TOOLS (Existing)         │  OPEN-SOURCE TOOLS (New)    │
├───────────────────────────────────┼─────────────────────────────┤
│  code_migration_analyzer.py       │  Renovate (Dependencies)    │
│  duplication_checker.py           │  EF Core (DB Migrations)    │
│  optimization_analyzer.py         │  GitVersion (Versioning)    │
│  project_maintenance.py           │  auto-changelog (Changelogs)│
│                                   │  SonarQube (Code Quality)   │
└─────────────────────────────────────────────────────────────────┘
```

**Benefits:**
- ✅ Automated dependency updates
- ✅ Database schema version control
- ✅ Automatic semantic versioning
- ✅ Generated changelogs
- ✅ Continuous code quality monitoring

---

## 📦 Tool #1: Renovate (Dependency Management)

**Purpose:** Automatically update NuGet, npm, pip packages
**Why:** Keeps dependencies secure and current without manual intervention

### Installation

```bash
# Install Renovate CLI globally
npm install -g renovate

# Or use Docker
docker pull renovate/renovate
```

### Configuration

Create `renovate.json` in project root:

```json
{
  "$schema": "https://docs.renovatebot.com/renovate-schema.json",
  "extends": [
    "config:base"
  ],
  "packageRules": [
    {
      "description": "Group .NET dependencies",
      "matchDatasources": ["nuget"],
      "groupName": ".NET dependencies",
      "schedule": ["before 3am on Monday"]
    },
    {
      "description": "Group Python dependencies",
      "matchDatasources": ["pypi"],
      "groupName": "Python dependencies",
      "schedule": ["before 3am on Monday"]
    },
    {
      "description": "Group JavaScript dependencies",
      "matchDatasources": ["npm"],
      "groupName": "JavaScript dependencies",
      "schedule": ["before 3am on Monday"]
    },
    {
      "description": "Auto-merge minor/patch updates",
      "matchUpdateTypes": ["minor", "patch"],
      "automerge": true,
      "automergeType": "pr",
      "requiredStatusChecks": null
    }
  ],
  "vulnerabilityAlerts": {
    "labels": ["security"],
    "assignees": ["@team"]
  },
  "prConcurrentLimit": 5,
  "prHourlyLimit": 2,
  "timezone": "America/Chicago"
}
```

### Usage

```bash
# Run locally
renovate --platform=local --token=unused

# Run on GitHub repo
renovate --platform=github --token=ghp_xxx username/algotrendy

# Check what would be updated (dry-run)
renovate --dry-run=full
```

**What it does:**
- Scans `*.csproj`, `requirements.txt`, `package.json`
- Creates PRs for outdated packages
- Groups related updates
- Auto-merges safe updates

---

## 🗄️ Tool #2: Entity Framework Core Migrations (Database)

**Purpose:** Version-controlled database schema changes
**Why:** Track and apply database changes safely across environments

### Installation

```bash
# Already included in .NET 8 SDK
cd /root/AlgoTrendy_v2.6/backend/AlgoTrendy.Infrastructure

# Install EF Core tools globally
dotnet tool install --global dotnet-ef
```

### Setup

Add to `AlgoTrendy.Infrastructure.csproj`:

```xml
<ItemGroup>
  <PackageReference Include="Microsoft.EntityFrameworkCore.Design" Version="8.0.0" />
  <PackageReference Include="Npgsql.EntityFrameworkCore.PostgreSQL" Version="8.0.0" />
</ItemGroup>
```

### Create Initial Migration

```bash
# Create first migration
dotnet ef migrations add InitialCreate \
  --project AlgoTrendy.Infrastructure \
  --startup-project AlgoTrendy.API \
  --context ApplicationDbContext \
  --output-dir Migrations

# Apply migration
dotnet ef database update \
  --project AlgoTrendy.Infrastructure \
  --startup-project AlgoTrendy.API
```

### Common Commands

```bash
# Add new migration
dotnet ef migrations add AddUserSettings

# Apply migrations
dotnet ef database update

# Rollback to specific migration
dotnet ef database update MigrationName

# Generate SQL script (for production)
dotnet ef migrations script --idempotent --output migration.sql

# Remove last migration (if not applied)
dotnet ef migrations remove
```

### Production Workflow

```bash
# 1. Development: Create migration
dotnet ef migrations add FeatureX

# 2. Staging: Generate SQL
dotnet ef migrations script --idempotent -o deploy/migrations/v2.6.1.sql

# 3. Production: Review + Apply SQL manually
psql -U algotrendy -d algotrendy_production -f deploy/migrations/v2.6.1.sql

# Or auto-apply on startup (Program.cs):
# using (var scope = app.Services.CreateScope())
# {
#     var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
#     context.Database.Migrate(); // Applies pending migrations
# }
```

---

## 🏷️ Tool #3: GitVersion (Semantic Versioning)

**Purpose:** Automatic version number calculation from Git history
**Why:** Eliminate manual version bumping, ensure SemVer compliance

### Installation

```bash
# Install as .NET tool
dotnet tool install --global GitVersion.Tool

# Or use Docker
docker pull gittools/gitversion:latest
```

### Configuration

Create `GitVersion.yml` in project root:

```yaml
mode: Mainline
branches:
  main:
    regex: ^main$
    tag: ''
    increment: Minor
    prevent-increment-of-merged-branch-version: false
  feature:
    regex: ^feature/
    tag: beta
    increment: Minor
  hotfix:
    regex: ^hotfix/
    tag: ''
    increment: Patch
major-version-bump-message: '\+semver:\s?(breaking|major)'
minor-version-bump-message: '\+semver:\s?(feature|minor)'
patch-version-bump-message: '\+semver:\s?(fix|patch)'
commit-message-incrementing: Enabled
```

### Usage

```bash
# Get current version
dotnet gitversion

# Output specific field
dotnet gitversion /showvariable SemVer
# Example output: 2.6.0

# Use in CI/CD
VERSION=$(dotnet gitversion /showvariable SemVer)
echo "Building version: $VERSION"

# Update .csproj files
dotnet gitversion /updateassemblyinfo
```

### Integration with Build

```bash
# build.sh
#!/bin/bash
VERSION=$(dotnet gitversion /showvariable SemVer)

# Build with version
dotnet build -c Release -p:Version=$VERSION

# Tag Docker image
docker build -t algotrendy-api:$VERSION .
docker tag algotrendy-api:$VERSION algotrendy-api:latest

echo "Built version: $VERSION"
```

### Commit Message Convention

```bash
# Patch bump (2.6.0 → 2.6.1)
git commit -m "fix: Resolve order execution bug +semver: patch"

# Minor bump (2.6.0 → 2.7.0)
git commit -m "feat: Add new broker integration +semver: minor"

# Major bump (2.6.0 → 3.0.0)
git commit -m "refactor: Migrate to .NET 9 +semver: major"
```

---

## 📝 Tool #4: auto-changelog (Changelog Generation)

**Purpose:** Auto-generate CHANGELOG.md from Git commits
**Why:** Keep users informed of changes without manual documentation

### Installation

```bash
npm install -g auto-changelog

# Or use npx (no install)
npx auto-changelog
```

### Configuration

Create `.auto-changelog` in project root:

```json
{
  "output": "CHANGELOG.md",
  "template": "keepachangelog",
  "unreleased": true,
  "commitLimit": false,
  "backfillLimit": false,
  "hideCredit": true,
  "includeBranch": ["main"],
  "replaceText": {
    "([A-Z]+-\\d+)": "[`$1`](https://jira.company.com/browse/$1)"
  },
  "packageFiles": [
    {
      "path": "backend/AlgoTrendy.API/AlgoTrendy.API.csproj",
      "transform": "Version=\"([0-9.]+)\""
    }
  ],
  "types": [
    {
      "type": "feat",
      "section": "Features",
      "hidden": false
    },
    {
      "type": "fix",
      "section": "Bug Fixes",
      "hidden": false
    },
    {
      "type": "perf",
      "section": "Performance Improvements",
      "hidden": false
    },
    {
      "type": "docs",
      "section": "Documentation",
      "hidden": false
    },
    {
      "type": "chore",
      "hidden": true
    },
    {
      "type": "style",
      "hidden": true
    },
    {
      "type": "test",
      "hidden": true
    }
  ]
}
```

### Usage

```bash
# Generate changelog
auto-changelog

# Generate for specific version
auto-changelog --starting-version 2.6.0

# Preview without writing
auto-changelog --stdout

# Use in release script
auto-changelog && git add CHANGELOG.md && git commit -m "docs: Update changelog"
```

### Example Output

```markdown
## [2.6.0] - 2025-10-18

### Features
- **trading-engine**: Add momentum and RSI strategies ([abc123](link))
- **broker**: Integrate Binance testnet support ([def456](link))

### Bug Fixes
- **api**: Fix order status synchronization ([ghi789](link))

### Performance Improvements
- **database**: Add caching to indicator calculations ([jkl012](link))

## [2.5.0] - 2025-10-15
...
```

---

## 🔍 Tool #5: SonarQube (Code Quality & Security)

**Purpose:** Continuous code quality and security analysis
**Why:** Catch bugs, vulnerabilities, code smells automatically

### Installation (Docker)

```bash
# Start SonarQube server
docker run -d --name sonarqube \
  -p 9090:9000 \
  -v sonarqube_data:/opt/sonarqube/data \
  -v sonarqube_logs:/opt/sonarqube/logs \
  sonarqube:lts-community

# Wait for startup (check http://localhost:9090)
# Default credentials: admin/admin
```

### Scanner Installation

```bash
# Install SonarScanner .NET
dotnet tool install --global dotnet-sonarscanner

# Or download standalone scanner
wget https://binaries.sonarsource.com/Distribution/sonar-scanner-cli/sonar-scanner-cli-5.0.1.3006-linux.zip
unzip sonar-scanner-cli-5.0.1.3006-linux.zip -d /opt/
export PATH="/opt/sonar-scanner-5.0.1.3006-linux/bin:$PATH"
```

### Scan .NET Project

```bash
# Begin scan
dotnet sonarscanner begin \
  /k:"AlgoTrendy" \
  /d:sonar.host.url="http://localhost:9090" \
  /d:sonar.login="your-token-here"

# Build project
dotnet build backend/AlgoTrendy.sln

# End scan (uploads results)
dotnet sonarscanner end /d:sonar.login="your-token-here"
```

### Scan Python Code

```bash
# Create sonar-project.properties
cat > sonar-project.properties <<EOF
sonar.projectKey=AlgoTrendy-Python
sonar.sources=.
sonar.language=py
sonar.python.version=3.11
sonar.exclusions=**/*test*.py,**/__pycache__/**
EOF

# Run scanner
sonar-scanner \
  -Dsonar.host.url=http://localhost:9090 \
  -Dsonar.login=your-token-here
```

### Integration with CI/CD

```bash
# .github/workflows/quality.yml
name: Code Quality

on: [push, pull_request]

jobs:
  sonarqube:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v3

      - name: SonarQube Scan
        uses: SonarSource/sonarcloud-github-action@master
        env:
          SONAR_TOKEN: ${{ secrets.SONAR_TOKEN }}
```

---

## 🔄 Unified Workflow: Combining All Tools

### Daily Development

```bash
# 1. Check for dependency updates
renovate --dry-run

# 2. Run code quality scan
dotnet sonarscanner begin ...
dotnet build
dotnet sonarscanner end ...

# 3. Check current version
dotnet gitversion
```

### Before Release

```bash
#!/bin/bash
# release.sh - Automated release workflow

set -e

echo "🚀 AlgoTrendy Release Workflow"

# 1. Get version
VERSION=$(dotnet gitversion /showvariable SemVer)
echo "📦 Version: $VERSION"

# 2. Run tests
echo "🧪 Running tests..."
cd backend
dotnet test --no-build --verbosity normal
cd ..

# 3. Create database migration
echo "🗄️ Creating database migration..."
cd backend/AlgoTrendy.Infrastructure
dotnet ef migrations add "Release_v${VERSION}"
cd ../..

# 4. Update changelog
echo "📝 Updating changelog..."
auto-changelog

# 5. Build Docker image
echo "🐳 Building Docker image..."
docker build -f backend/Dockerfile -t algotrendy-api:${VERSION} .

# 6. Tag release
echo "🏷️ Tagging release..."
git tag -a "v${VERSION}" -m "Release v${VERSION}"

# 7. Push to remote
echo "📤 Pushing to remote..."
git push origin main --tags

echo "✅ Release v${VERSION} complete!"
```

### Production Deployment

```bash
#!/bin/bash
# deploy.sh - Production deployment

VERSION=$(dotnet gitversion /showvariable SemVer)

# 1. Generate migration SQL
dotnet ef migrations script --idempotent -o deploy/migration_${VERSION}.sql

# 2. Pull latest image
docker pull algotrendy-api:${VERSION}

# 3. Apply database migrations
psql -U algotrendy -d algotrendy_production -f deploy/migration_${VERSION}.sql

# 4. Update containers
docker-compose -f docker-compose.prod.yml up -d

# 5. Health check
sleep 10
curl -f http://localhost:5002/health || exit 1

echo "✅ Deployed v${VERSION} successfully!"
```

---

## 📊 Comparison: Custom vs Open-Source Tools

| Task | Custom Tool | Open-Source Tool | Best Use |
|------|-------------|------------------|----------|
| **Code Migration** | code_migration_analyzer.py | - | ✅ Custom (AlgoTrendy-specific) |
| **Duplication** | duplication_checker.py | SonarQube | Both (Custom for quick checks, SonarQube for CI) |
| **Dependencies** | - | **Renovate** | ✅ Renovate (automated PRs) |
| **DB Migrations** | - | **EF Core Migrations** | ✅ EF Core (industry standard) |
| **Versioning** | Manual in csproj | **GitVersion** | ✅ GitVersion (automated) |
| **Changelog** | Manual writing | **auto-changelog** | ✅ auto-changelog (automated) |
| **Code Quality** | optimization_analyzer.py | **SonarQube** | Both (Custom for specific checks) |
| **Security** | - | **Renovate + SonarQube** | ✅ Open-source (comprehensive) |

**Recommendation:** Use **both**!
- Custom tools for AlgoTrendy-specific migration analysis
- Open-source tools for industry-standard automation

---

## 🎯 Implementation Checklist

### Phase 1: Quick Wins (1-2 hours)
- [ ] Install GitVersion and configure `GitVersion.yml`
- [ ] Install auto-changelog and generate first changelog
- [ ] Add EF Core migrations package to Infrastructure project
- [ ] Create initial database migration

### Phase 2: Dependency Management (1 hour)
- [ ] Create `renovate.json` configuration
- [ ] Run Renovate in dry-run mode
- [ ] Review proposed updates
- [ ] Approve and merge safe updates

### Phase 3: Code Quality (2-3 hours)
- [ ] Set up SonarQube in Docker
- [ ] Configure SonarScanner for .NET
- [ ] Run first scan and review results
- [ ] Fix critical issues identified

### Phase 4: Automation (1-2 hours)
- [ ] Create `release.sh` script
- [ ] Create `deploy.sh` script
- [ ] Test release workflow locally
- [ ] Document the new process

### Phase 5: CI/CD Integration (Optional, 2-4 hours)
- [ ] Add GitHub Actions workflow for Renovate
- [ ] Add workflow for SonarQube scans
- [ ] Add workflow for automated releases
- [ ] Configure branch protection rules

---

## 📚 Additional Resources

### Documentation
- **Renovate**: https://docs.renovatebot.com/
- **EF Core Migrations**: https://learn.microsoft.com/en-us/ef/core/managing-schemas/migrations/
- **GitVersion**: https://gitversion.net/docs/
- **auto-changelog**: https://github.com/cookpete/auto-changelog
- **SonarQube**: https://docs.sonarqube.org/

### AlgoTrendy-Specific
- Custom tools: `./tools/TOOLS_GUIDE.md`
- Upgrade framework: `./UPGRADE_FRAMEWORK.md`
- v2.5→v2.6 case study: `./docs/v2.5-v2.6_CASE_STUDY.md`

---

## 🤔 FAQ

**Q: Do I still need custom tools if using open-source ones?**
A: Yes! Custom tools analyze AlgoTrendy-specific migration patterns. Open-source tools handle generic tasks (dependencies, versioning).

**Q: Which tool should I use first?**
A: Start with GitVersion (easiest, immediate value). Then auto-changelog, then Renovate.

**Q: Is SonarQube free?**
A: Yes! Community edition is free and open-source. SonarCloud (hosted) offers free tier for public repos.

**Q: How does Renovate compare to Dependabot?**
A: Renovate is more configurable and supports more package managers. Dependabot is simpler and GitHub-native. Both are excellent.

**Q: Can I run these tools locally?**
A: Yes! All tools can run locally before committing to CI/CD automation.

---

**Status:** ✅ Integration guide complete
**Last Updated:** October 18, 2025
**Next Steps:** Choose tools to implement from checklist above
