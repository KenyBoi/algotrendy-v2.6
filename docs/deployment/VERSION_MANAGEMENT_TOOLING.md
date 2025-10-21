# Version Management Tooling for Parallel Architecture
**Date:** 2025-10-21
**Goal:** Automate version management for both v2.6 monolith and v3.0 modular branches

---

## Tools You Already Have

### 1. Changelog Config (✅ Installed)
**File:** `.github/changelog-config.json`
**Purpose:** Automated CHANGELOG.md generation from commit messages
**Usage:** Works with GitHub Actions to generate release notes

### 2. Conventional Commits (✅ In Use)
**Pattern:** `feat:`, `fix:`, `docs:`, `chore:`
**Purpose:** Structured commit messages for automation

---

## Recommended Tools for Parallel Architecture

### Tool Stack

| Tool | Purpose | Install Time | Cost |
|------|---------|--------------|------|
| **semantic-release** | Automated versioning & releases | 30 min | Free |
| **conventional-changelog** | Generate changelogs | 15 min | Free |
| **lerna** | Multi-package/module versioning | 20 min | Free |
| **husky** | Git hooks (pre-commit checks) | 10 min | Free |
| **commitlint** | Enforce commit message format | 10 min | Free |

---

## Option 1: semantic-release (Recommended)

### What it Does
- **Analyzes commits** to determine version bump (major/minor/patch)
- **Generates CHANGELOG.md** automatically
- **Creates git tags** (v2.6.1, v3.0.0)
- **Publishes releases** to GitHub/npm/NuGet
- **Works per branch** - perfect for parallel strategy!

### Setup for Parallel Architecture

#### Install (GitHub Actions)
```yaml
# .github/workflows/release.yml
name: Release

on:
  push:
    branches:
      - main      # v2.6.x releases
      - modular   # v3.0.x releases

jobs:
  release:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v3
        with:
          fetch-depth: 0

      - name: Setup Node.js
        uses: actions/setup-node@v3
        with:
          node-version: 20

      - name: Install semantic-release
        run: |
          npm install -g \
            semantic-release@latest \
            @semantic-release/changelog \
            @semantic-release/git \
            @semantic-release/github

      - name: Run semantic-release
        env:
          GITHUB_TOKEN: ${{ secrets.GITHUB_TOKEN }}
        run: npx semantic-release
```

#### Configuration
```json
// .releaserc.json
{
  "branches": [
    "+([0-9])?(.{+([0-9]),x}).x",  // Support version branches
    "main",                          // v2.6.x releases
    {
      "name": "modular",             // v3.0.x releases
      "prerelease": "beta"           // Tag as beta until production-ready
    }
  ],
  "plugins": [
    "@semantic-release/commit-analyzer",
    "@semantic-release/release-notes-generator",
    "@semantic-release/changelog",
    "@semantic-release/github",
    "@semantic-release/git"
  ]
}
```

#### Result
- **Commit on main:** `feat: add new broker` → v2.6.1 released
- **Commit on modular:** `feat: add new broker` → v3.0.0-beta.1 released
- **Automatic changelogs:** Both branches get their own CHANGELOG.md

---

## Option 2: Lerna (Multi-Module Versioning)

### What it Does
- **Manages multiple packages** in a monorepo
- **Independent versioning** per module
- **Batch operations** across all modules
- **Smart dependency management**

### Setup
```bash
npm install -g lerna

# Initialize
lerna init
```

#### Configuration
```json
// lerna.json
{
  "version": "independent",     // Each module has own version
  "npmClient": "npm",
  "packages": [
    "backend/AlgoTrendy.Core",
    "backend/AlgoTrendy.TradingEngine",
    "backend/AlgoTrendy.DataChannels",
    "backend/AlgoTrendy.Backtesting"
  ],
  "command": {
    "version": {
      "conventionalCommits": true,
      "createRelease": "github",
      "message": "chore(release): publish %s"
    }
  }
}
```

#### Usage
```bash
# Bump versions for all changed modules
lerna version

# Example output:
#   AlgoTrendy.Core: 1.0.0 → 1.0.1
#   AlgoTrendy.TradingEngine: 1.2.0 → 1.3.0
#   (no change in other modules)

# Publish to NuGet
lerna exec -- dotnet pack
```

---

## Option 3: GitVersion (For .NET)

### What it Does
- **Calculates version** from git history
- **Works with GitFlow/GitHub Flow**
- **Integrates with .NET builds**
- **Generates AssemblyInfo automatically**

### Setup
```bash
# Install globally
dotnet tool install --global GitVersion.Tool

# Or per-project
dotnet add package GitVersion.MsBuild
```

#### Configuration
```yaml
# GitVersion.yml
mode: ContinuousDelivery
branches:
  main:
    tag: ''
    increment: Patch
    regex: ^main$
  modular:
    tag: 'beta'
    increment: Minor
    regex: ^modular$
increment: Inherit
major-version-bump-message: '\+semver:\s?(breaking|major)'
minor-version-bump-message: '\+semver:\s?(feature|minor)'
patch-version-bump-message: '\+semver:\s?(fix|patch)'
```

#### Usage
```bash
# Calculate version
gitversion

# Output:
# {
#   "Major": 2,
#   "Minor": 6,
#   "Patch": 1,
#   "SemVer": "2.6.1"
# }

# In CI/CD
dotnet build /p:Version=$(gitversion /showvariable SemVer)
```

---

## Integrated Workflow for Parallel Architecture

### Combining All Tools

```yaml
# .github/workflows/parallel-release.yml
name: Parallel Release Workflow

on:
  push:
    branches: [main, modular]

jobs:
  # Job 1: Determine versions
  version:
    runs-on: ubuntu-latest
    outputs:
      version: ${{ steps.gitversion.outputs.semVer }}
      branch: ${{ steps.branch.outputs.name }}
    steps:
      - uses: actions/checkout@v3
        with:
          fetch-depth: 0

      - name: Get branch name
        id: branch
        run: echo "name=${GITHUB_REF#refs/heads/}" >> $GITHUB_OUTPUT

      - name: Install GitVersion
        uses: gittools/actions/gitversion/setup@v0
        with:
          versionSpec: '5.x'

      - name: Determine Version
        id: gitversion
        uses: gittools/actions/gitversion/execute@v0

      - name: Display Version
        run: echo "Version is ${{ steps.gitversion.outputs.semVer }}"

  # Job 2: Build and test
  build:
    needs: version
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v3

      - name: Build with version
        run: |
          dotnet build \
            /p:Version=${{ needs.version.outputs.version }}

      - name: Run tests
        run: dotnet test

  # Job 3: Create release
  release:
    needs: [version, build]
    runs-on: ubuntu-latest
    if: github.event_name == 'push'
    steps:
      - uses: actions/checkout@v3
        with:
          fetch-depth: 0

      - name: Create GitHub Release
        uses: ncipollo/release-action@v1
        with:
          tag: v${{ needs.version.outputs.version }}
          name: |
            ${{ needs.version.outputs.branch == 'main' && 'AlgoTrendy v' || 'AlgoTrendy Modular v' }}${{ needs.version.outputs.version }}
          body: |
            ## Branch: ${{ needs.version.outputs.branch }}

            Auto-generated release from commit ${{ github.sha }}

            See CHANGELOG.md for details.
          prerelease: ${{ needs.version.outputs.branch == 'modular' }}
          token: ${{ secrets.GITHUB_TOKEN }}

  # Job 4: Sync modular branch (only from main)
  sync:
    needs: [release]
    if: github.ref == 'refs/heads/main'
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v3
        with:
          fetch-depth: 0

      - name: Sync to modular branch
        run: |
          git config user.name "github-actions[bot]"
          git config user.email "github-actions[bot]@users.noreply.github.com"
          git fetch origin modular:modular
          git checkout modular
          git merge origin/main -m "chore: sync from main v${{ needs.version.outputs.version }}"
          git push origin modular
```

---

## Module-Level Versioning (Per .csproj)

### Update All Project Files

```xml
<!-- backend/AlgoTrendy.Core/AlgoTrendy.Core.csproj -->
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net8.0</TargetFramework>

    <!-- Version from GitVersion -->
    <Version>$(GitVersion_SemVer)</Version>
    <AssemblyVersion>$(GitVersion_AssemblySemVer)</AssemblyVersion>
    <FileVersion>$(GitVersion_AssemblySemFileVer)</FileVersion>
    <InformationalVersion>$(GitVersion_InformationalVersion)</InformationalVersion>

    <!-- NuGet Package Info -->
    <PackageId>AlgoTrendy.Core</PackageId>
    <Authors>AlgoTrendy Team</Authors>
    <Description>Core domain models and interfaces for AlgoTrendy trading platform</Description>
    <PackageProjectUrl>https://github.com/AlgoTrendy/v2.6</PackageProjectUrl>
    <RepositoryUrl>https://github.com/AlgoTrendy/v2.6</RepositoryUrl>
    <PackageTags>trading;crypto;stocks;algotrading</PackageTags>
    <GeneratePackageOnBuild>true</GeneratePackageOnBuild>
  </PropertyGroup>
</Project>
```

---

## Recommended Implementation Plan

### Phase 1: Basic Automation (2 hours)
1. ✅ Install GitVersion (30 min)
2. ✅ Configure GitVersion.yml (30 min)
3. ✅ Update .csproj files with version properties (30 min)
4. ✅ Test version calculation (30 min)

### Phase 2: Automated Releases (2 hours)
1. ✅ Install semantic-release (30 min)
2. ✅ Configure .releaserc.json (30 min)
3. ✅ Create GitHub Actions workflow (45 min)
4. ✅ Test releases on both branches (15 min)

### Phase 3: Module Versioning (3 hours)
1. ✅ Set up Lerna or similar (1 hour)
2. ✅ Configure independent module versions (1 hour)
3. ✅ Create NuGet packages (1 hour)

### Phase 4: Full Integration (3 hours)
1. ✅ Combine all tools in unified workflow (2 hours)
2. ✅ Test end-to-end (1 hour)

**Total Time: 10 hours for complete automation**

---

## Quick Start (Fastest Path)

### Just use GitVersion
```bash
# Install
dotnet tool install --global GitVersion.Tool

# Run
gitversion

# Add to CI/CD
echo "VERSION=$(gitversion /showvariable SemVer)" >> $GITHUB_ENV
```

**Boom!** Automatic versioning in 5 minutes.

---

## Benefits for Your Parallel Strategy

| Feature | Without Tools | With Tools |
|---------|--------------|------------|
| Version calculation | Manual | ✅ Automatic |
| CHANGELOG.md | Manual | ✅ Auto-generated |
| Release creation | Manual | ✅ Automated |
| Branch sync | Manual script | ✅ Automated |
| Version conflicts | Common | ✅ Prevented |
| Rollback | Complex | ✅ Easy (git tags) |

---

## Next Steps

What would you like to implement first?

1. **GitVersion** (5 min) - Quickest win
2. **semantic-release** (30 min) - Full automation
3. **Lerna** (1 hour) - Module versioning
4. **Complete workflow** (3 hours) - Everything integrated

Let me know and I'll help you set it up!
