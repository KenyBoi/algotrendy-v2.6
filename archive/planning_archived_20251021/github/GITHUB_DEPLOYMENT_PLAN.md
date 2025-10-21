# GitHub Deployment Plan - AlgoTrendy v2.6

**Status:** 📋 Ready for Execution
**Repository Type:** 🔒 **PRIVATE** (Proprietary Code)
**Deployment Date:** TBD
**Estimated Time:** 2-3 hours (initial setup + configuration)

---

## Table of Contents

1. [Pre-Deployment Checklist](#pre-deployment-checklist)
2. [Phase 1: Repository Creation](#phase-1-repository-creation)
3. [Phase 2: Initial Push](#phase-2-initial-push)
4. [Phase 3: Security Configuration](#phase-3-security-configuration)
5. [Phase 4: Branch Protection](#phase-4-branch-protection)
6. [Phase 5: GitHub Tools Setup](#phase-5-github-tools-setup)
7. [Post-Deployment Verification](#post-deployment-verification)
8. [Rollback Plan](#rollback-plan)

---

## Pre-Deployment Checklist

### ✅ Security Audit

**1. Verify No Secrets in Tracked Files**

```bash
# Check for common secret patterns
cd /root/AlgoTrendy_v2.6
git grep -E "(sk_live_|pk_live_|api_key.*=.*[A-Za-z0-9]{20,})" || echo "✅ No live secrets found"
git grep -E "(password.*=.*[^GET_FROM|USE_USER])" || echo "✅ No hardcoded passwords"
git grep -E "(secret.*=.*[^GET_FROM|USE_USER])" || echo "✅ No hardcoded secrets"

# Verify appsettings.json only has placeholders
grep -E "(ApiKey|ApiSecret)" backend/AlgoTrendy.API/appsettings.json
# ✅ Should only show: "GET_FREE_KEY_FROM" or "USE_USER_SECRETS"
```

**2. Verify .gitignore Protection**

```bash
# Check .gitignore covers secrets
cat .gitignore | grep -E "(\.env|appsettings\..*\.json|secrets|\.key)"

# ✅ Should show:
# .env
# .env.local
# appsettings.*.json
# secrets/
# *.key
# vault_keys/
```

**3. Check for Accidentally Tracked Secret Files**

```bash
# List all tracked files
git ls-files | grep -E "(\.env$|appsettings\.(Production|Development)\.json|\.key$|secrets/)"

# ✅ Should return NOTHING (or only appsettings.json base file)
```

**4. Review Recent Commits for Secrets**

```bash
# Check last 10 commits for sensitive data
git log -10 --all --oneline

# Manually review each commit if needed:
git show COMMIT_HASH
```

**✅ PRE-DEPLOYMENT SECURITY CHECKLIST:**

- [ ] No live API keys in any tracked files
- [ ] No passwords in any tracked files
- [ ] .gitignore properly configured
- [ ] appsettings.json only has placeholders
- [ ] No .env files tracked
- [ ] No appsettings.Production.json tracked
- [ ] Reviewed recent commits for leaks
- [ ] Removed any sensitive comments in code

---

## Phase 1: Repository Creation

### Step 1.1: Create GitHub Account (if needed)

**If you don't have a GitHub account:**
1. Go to https://github.com/join
2. Choose a professional username (e.g., `algotrendy`, `yourname-trading`)
3. Use a professional email
4. ✅ Enable 2FA immediately (Settings → Password and authentication)

**Recommended username strategies:**
- `algotrendy` - Brand-focused
- `yourname` - Personal brand
- `yourcompany` - If you have a company entity

### Step 1.2: Create Private Repository

**URL:** https://github.com/new

**Settings:**
```
Repository name:        AlgoTrendy
                        (or: AlgoTrendy_v2.6, AlgoTrendy-Platform)

Description:            Multi-Asset Algorithmic Trading Platform
                        FREE Tier Data Infrastructure | 4 Providers |
                        87K+ Daily API Calls | $0/month

Visibility:             🔒 PRIVATE ⚠️ CRITICAL!

Initialize:
  [ ] Add a README file          ← UNCHECK (you already have one)
  [ ] Add .gitignore             ← UNCHECK (you already have one)
  [ ] Choose a license           ← SKIP (proprietary)
```

**Click:** "Create repository"

### Step 1.3: Note Your Repository URL

After creation, GitHub shows:

```
Quick setup — if you've done this kind of thing before

https://github.com/YOUR_USERNAME/AlgoTrendy.git
```

**Copy this URL** - you'll need it in Phase 2.

---

## Phase 2: Initial Push

### Step 2.1: Configure Git User (if not already done)

```bash
# Verify current config
git config user.name
git config user.email

# If needed, update:
git config user.name "Your Name"
git config user.email "your.email@example.com"
```

### Step 2.2: Add GitHub Remote

```bash
cd /root/AlgoTrendy_v2.6

# Add GitHub as remote origin
git remote add origin https://github.com/YOUR_USERNAME/AlgoTrendy.git

# Verify remote was added
git remote -v
# Should show:
# origin  https://github.com/YOUR_USERNAME/AlgoTrendy.git (fetch)
# origin  https://github.com/YOUR_USERNAME/AlgoTrendy.git (push)
```

**If you make a mistake:**
```bash
# Remove remote
git remote remove origin

# Re-add with correct URL
git remote add origin https://github.com/YOUR_USERNAME/AlgoTrendy.git
```

### Step 2.3: Authenticate to GitHub

**Option A: Personal Access Token (Recommended)**

1. Go to https://github.com/settings/tokens
2. Click "Generate new token" → "Generate new token (classic)"
3. Settings:
   ```
   Note:              AlgoTrendy Local Development
   Expiration:        90 days (or longer)

   Scopes:
   ☑ repo             (Full control of private repositories)
     ☑ repo:status
     ☑ repo_deployment
     ☑ public_repo
     ☑ repo:invite
     ☑ security_events

   ☑ workflow         (Update GitHub Actions workflows)
   ```
4. Click "Generate token"
5. **COPY THE TOKEN** (you won't see it again!)
6. Save it temporarily in a secure note

**Option B: GitHub CLI (Easier)**

```bash
# Install GitHub CLI (if not installed)
curl -fsSL https://cli.github.com/packages/githubcli-archive-keyring.gpg | sudo dd of=/usr/share/keyrings/githubcli-archive-keyring.gpg
echo "deb [arch=$(dpkg --print-architecture) signed-by=/usr/share/keyrings/githubcli-archive-keyring.gpg] https://cli.github.com/packages stable main" | sudo tee /etc/apt/sources.list.d/github-cli.list > /dev/null
sudo apt update
sudo apt install gh

# Authenticate
gh auth login
# Choose: GitHub.com → HTTPS → Yes (authenticate Git) → Login with browser
```

### Step 2.4: Review What Will Be Pushed

```bash
# Check current branch
git branch --show-current

# View commit history
git log --oneline -20

# Check status
git status

# See all branches
git branch -a
```

**Current branches in your repo:**
- `fix/cleanup-orphaned-files` (current - has Phase 2 work)
- Potentially others

### Step 2.5: Push Current Branch

```bash
# Push current branch to GitHub
git push -u origin fix/cleanup-orphaned-files

# When prompted for credentials:
# Username: YOUR_GITHUB_USERNAME
# Password: YOUR_PERSONAL_ACCESS_TOKEN (paste the token from Step 2.3)
```

**Expected output:**
```
Enumerating objects: 1842, done.
Counting objects: 100% (1842/1842), done.
Delta compression using up to 4 threads
Compressing objects: 100% (856/856), done.
Writing objects: 100% (1842/1842), 2.45 MiB | 3.12 MiB/s, done.
Total 1842 (delta 876), reused 1735 (delta 801), pack-reused 0
remote: Resolving deltas: 100% (876/876), done.
To https://github.com/YOUR_USERNAME/AlgoTrendy.git
 * [new branch]      fix/cleanup-orphaned-files -> fix/cleanup-orphaned-files
Branch 'fix/cleanup-orphaned-files' set up to track remote branch 'fix/cleanup-orphaned-files' from 'origin'.
```

### Step 2.6: Push Main Branch (if exists)

```bash
# Check if main branch exists
git branch -a | grep main

# If main exists, push it
git checkout main
git push -u origin main

# Return to your working branch
git checkout fix/cleanup-orphaned-files
```

### Step 2.7: Push All Branches (Optional)

```bash
# Push ALL local branches to GitHub
git push --all origin

# Push all tags (if any)
git push --tags origin
```

### Step 2.8: Verify Push Success

**Check GitHub:**
1. Go to https://github.com/YOUR_USERNAME/AlgoTrendy
2. ✅ You should see your code, commits, branches
3. ✅ Verify "Private" badge appears next to repo name
4. ✅ Check recent commits match your local `git log`

---

## Phase 3: Security Configuration

### Step 3.1: Enable Secret Scanning

**Path:** Repository → Settings → Code security and analysis

```
Secret scanning
  ☑ Enable (FREE for private repos)

  What it does:
  - Scans commits for accidentally committed secrets
  - Detects 200+ token types (API keys, AWS keys, etc.)
  - Alerts you immediately if secrets found

  ☑ Push protection (prevents pushes with secrets)
```

**Click:** "Enable" for both

### Step 3.2: Enable Dependabot Alerts

**Path:** Settings → Code security and analysis

```
Dependabot alerts
  ☑ Enable

  What it does:
  - Alerts you to known vulnerabilities in dependencies
  - Covers NuGet, npm, pip packages
  - Email notifications for new vulnerabilities

Dependabot security updates
  ☑ Enable

  What it does:
  - Automatically creates PRs to fix vulnerabilities
  - Updates vulnerable dependencies
  - You review and merge
```

**Click:** "Enable Dependabot alerts" and "Enable Dependabot security updates"

### Step 3.3: Enable Dependabot Version Updates (Optional)

**Create file:** `.github/dependabot.yml`

```yaml
version: 2
updates:
  # NuGet dependencies
  - package-ecosystem: "nuget"
    directory: "/backend"
    schedule:
      interval: "weekly"
      day: "monday"
      time: "09:00"
    open-pull-requests-limit: 10
    labels:
      - "dependencies"
      - "nuget"

  # npm dependencies (if you add frontend)
  - package-ecosystem: "npm"
    directory: "/frontend"
    schedule:
      interval: "weekly"
      day: "monday"
      time: "09:00"
    open-pull-requests-limit: 5
    labels:
      - "dependencies"
      - "npm"

  # Python dependencies (yfinance service)
  - package-ecosystem: "pip"
    directory: "/backend/AlgoTrendy.DataChannels/PythonServices"
    schedule:
      interval: "weekly"
      day: "monday"
      time: "09:00"
    open-pull-requests-limit: 5
    labels:
      - "dependencies"
      - "python"

  # GitHub Actions
  - package-ecosystem: "github-actions"
    directory: "/"
    schedule:
      interval: "weekly"
      day: "monday"
      time: "09:00"
    open-pull-requests-limit: 5
    labels:
      - "dependencies"
      - "github-actions"
```

**Commit and push:**
```bash
git add .github/dependabot.yml
git commit -m "chore: Configure Dependabot for dependency updates"
git push
```

### Step 3.4: Enable Code Scanning (CodeQL)

**Path:** Settings → Code security and analysis → Code scanning

```
CodeQL analysis
  ☑ Set up → Default

  Languages detected:
  - C#
  - JavaScript/TypeScript (if frontend)
  - Python

  Scan schedule:
  - On push to default branch
  - On pull requests
  - Weekly scheduled scan
```

**Click:** "Enable CodeQL"

This creates `.github/workflows/codeql.yml` automatically.

### Step 3.5: Review Security Settings

**Path:** Settings → Security → Security Overview

**Verify all enabled:**
- ✅ Secret scanning
- ✅ Push protection
- ✅ Dependabot alerts
- ✅ Dependabot security updates
- ✅ Code scanning (CodeQL)

---

## Phase 4: Branch Protection

### Step 4.1: Create Main Branch (if not exists)

```bash
# If you don't have a main branch yet, create one
git checkout -b main
git push -u origin main
```

### Step 4.2: Set Default Branch

**Path:** Settings → General → Default branch

```
Default branch: main (or fix/cleanup-orphaned-files if main doesn't exist yet)
```

**Click:** "Update" if changing

### Step 4.3: Configure Branch Protection Rules

**Path:** Settings → Branches → Add branch protection rule

**Rule 1: Protect `main` branch**

```
Branch name pattern: main

☑ Require a pull request before merging
  ☑ Require approvals: 1
  ☐ Dismiss stale pull request approvals when new commits are pushed
  ☐ Require review from Code Owners

☑ Require status checks to pass before merging
  ☑ Require branches to be up to date before merging

  Status checks (add when you have CI/CD):
  - build
  - test
  - codeql

☑ Require conversation resolution before merging

☑ Require signed commits (optional, for extra security)

☑ Require linear history

☑ Do not allow bypassing the above settings
  ⚠️ Even admins must follow rules

☐ Allow force pushes (keep UNCHECKED)
☐ Allow deletions (keep UNCHECKED)
```

**Click:** "Create" or "Save changes"

**Rule 2: Protect `develop` branch (if you use GitFlow)**

Repeat above with same settings for `develop` branch.

---

## Phase 5: GitHub Tools Setup

### Step 5.1: Set Up GitHub Actions (CI/CD)

**Create:** `.github/workflows/dotnet-build-test.yml`

```yaml
name: .NET Build and Test

on:
  push:
    branches: [ main, develop, fix/** ]
  pull_request:
    branches: [ main, develop ]

env:
  DOTNET_VERSION: '8.0.x'

jobs:
  build-and-test:
    runs-on: ubuntu-latest

    steps:
    - name: Checkout code
      uses: actions/checkout@v4

    - name: Setup .NET
      uses: actions/setup-dotnet@v4
      with:
        dotnet-version: ${{ env.DOTNET_VERSION }}

    - name: Restore dependencies
      run: dotnet restore
      working-directory: ./backend

    - name: Build
      run: dotnet build --no-restore --configuration Release
      working-directory: ./backend

    - name: Run Unit Tests
      run: dotnet test --no-build --configuration Release --verbosity normal --filter "Category=Unit"
      working-directory: ./backend

    - name: Run Integration Tests (without external APIs)
      run: dotnet test --no-build --configuration Release --verbosity normal --filter "Category=Integration&Category!=ExternalAPI"
      working-directory: ./backend
      env:
        # Integration tests that don't require real API keys
        SKIP_API_TESTS: true

    - name: Upload test results
      if: always()
      uses: actions/upload-artifact@v4
      with:
        name: test-results
        path: backend/**/TestResults/*.trx
```

**Commit and push:**
```bash
git add .github/workflows/dotnet-build-test.yml
git commit -m "ci: Add .NET build and test workflow"
git push
```

### Step 5.2: Add Build Status Badge to README

**Edit:** `README.md`

Add at the top:
```markdown
# AlgoTrendy v2.6

![Build Status](https://github.com/YOUR_USERNAME/AlgoTrendy/workflows/.NET%20Build%20and%20Test/badge.svg)
![License](https://img.shields.io/badge/license-Private-red)
![.NET](https://img.shields.io/badge/.NET-8.0-purple)
![Score](https://img.shields.io/badge/production%20readiness-73%2F100-yellow)
![Cost](https://img.shields.io/badge/data%20cost-%240%2Fmonth-brightgreen)
```

### Step 5.3: Configure GitHub Secrets for CI/CD

**Path:** Settings → Secrets and variables → Actions → New repository secret

**Add secrets for integration tests (optional):**

```
Secret name: ALPHA_VANTAGE_API_KEY
Secret value: [your Alpha Vantage API key]

Secret name: FINNHUB_API_KEY
Secret value: [your Finnhub API key]

Secret name: FMP_API_KEY
Secret value: [your FMP API key]
```

**Update workflow to use secrets:**
```yaml
- name: Run Integration Tests (with APIs)
  run: dotnet test --configuration Release --filter "Category=Integration&Category=ExternalAPI"
  env:
    AlphaVantage__ApiKey: ${{ secrets.ALPHA_VANTAGE_API_KEY }}
    Finnhub__ApiKey: ${{ secrets.FINNHUB_API_KEY }}
    FinancialModelingPrep__ApiKey: ${{ secrets.FMP_API_KEY }}
```

### Step 5.4: Enable GitHub Projects

**Path:** Settings → Features

```
☑ Issues
☑ Projects
☐ Wikis (optional)
☐ Discussions (optional for Q&A)
```

**Create Project Board:**
1. Projects tab → "New project"
2. Choose "Board" template
3. Name: "AlgoTrendy Development Roadmap"
4. Columns:
   - 📋 Backlog
   - 🎯 Ready
   - 🚧 In Progress
   - 🧪 Testing
   - ✅ Done

### Step 5.5: Set Up Issue Templates

**Create:** `.github/ISSUE_TEMPLATE/bug_report.md`

```markdown
---
name: Bug Report
about: Report a bug in AlgoTrendy
title: '[BUG] '
labels: bug
assignees: ''
---

**Describe the bug**
A clear description of what the bug is.

**To Reproduce**
Steps to reproduce:
1. Go to '...'
2. Click on '...'
3. See error

**Expected behavior**
What you expected to happen.

**Environment:**
 - OS: [e.g., Ubuntu 22.04]
 - .NET Version: [e.g., 8.0]
 - Broker: [e.g., Binance Testnet]

**Additional context**
Add any other context about the problem here.
```

**Create:** `.github/ISSUE_TEMPLATE/feature_request.md`

```markdown
---
name: Feature Request
about: Suggest a new feature for AlgoTrendy
title: '[FEATURE] '
labels: enhancement
assignees: ''
---

**Feature Description**
A clear description of the feature you'd like.

**Use Case**
Explain the problem this feature would solve.

**Proposed Solution**
How you envision this feature working.

**Alternatives Considered**
Other approaches you've thought about.

**Additional context**
Add any other context or screenshots.
```

### Step 5.6: Create Pull Request Template

**Create:** `.github/PULL_REQUEST_TEMPLATE.md`

```markdown
## Description
<!-- Brief description of changes -->

## Type of Change
- [ ] Bug fix (non-breaking change which fixes an issue)
- [ ] New feature (non-breaking change which adds functionality)
- [ ] Breaking change (fix or feature that would cause existing functionality to not work as expected)
- [ ] Documentation update
- [ ] Performance improvement
- [ ] Refactoring

## Testing
<!-- How has this been tested? -->
- [ ] Unit tests added/updated
- [ ] Integration tests added/updated
- [ ] Manual testing completed
- [ ] All tests passing locally

## Checklist
- [ ] Code follows project style guidelines
- [ ] Self-review completed
- [ ] Comments added for complex logic
- [ ] Documentation updated
- [ ] No new warnings generated
- [ ] No secrets committed

## Related Issues
<!-- Link related issues: Closes #123 -->

## Screenshots (if applicable)
<!-- Add screenshots for UI changes -->

## Additional Notes
<!-- Any additional information -->
```

---

## Post-Deployment Verification

### Verification Checklist

**1. Repository Access**
```bash
# Clone from GitHub to verify
cd /tmp
git clone https://github.com/YOUR_USERNAME/AlgoTrendy.git
cd AlgoTrendy

# ✅ Should clone successfully
# ✅ Should prompt for credentials (private repo)
# ✅ All files should be present
```

**2. Security Settings**
- [ ] Repository shows "🔒 Private" badge
- [ ] Secret scanning enabled
- [ ] Dependabot enabled
- [ ] CodeQL enabled
- [ ] Branch protection on main/develop

**3. GitHub Actions**
- [ ] Workflows visible in Actions tab
- [ ] First workflow run triggered
- [ ] Build succeeds (or shows expected failures)

**4. Project Board**
- [ ] Project board created
- [ ] Columns configured
- [ ] Can create issues

**5. Documentation**
- [ ] README displays correctly
- [ ] Badges showing (even if some are failing)
- [ ] Links work

**6. Collaboration Features**
- [ ] Can create issues
- [ ] Can create pull requests
- [ ] Issue templates work
- [ ] PR template works

---

## Rollback Plan

### If Something Goes Wrong

**Scenario 1: Accidentally pushed secrets**

```bash
# 1. Immediately rotate the compromised secret
#    - Generate new API key from provider
#    - Update local .env file

# 2. Remove secret from git history
git filter-branch --force --index-filter \
  "git rm --cached --ignore-unmatch PATH/TO/FILE/WITH/SECRET" \
  --prune-empty --tag-name-filter cat -- --all

# 3. Force push (if repository just created)
git push origin --force --all

# 4. Contact GitHub support if needed
#    https://support.github.com
```

**Scenario 2: Need to make repository public later**

```bash
# Before making public, run comprehensive security audit:
git log --all --full-history --source -- \*.env
git log --all --full-history --source -- \*appsettings.*.json

# Make public via Settings → Danger Zone → Change visibility
```

**Scenario 3: Wrong repository settings**

- Repository settings can be changed anytime
- No need to delete and recreate
- Just update Settings as needed

**Scenario 4: Need to delete repository**

```bash
# Settings → Danger Zone → Delete this repository
# Type repository name to confirm
# ⚠️ This is PERMANENT and cannot be undone!

# Your local copy remains intact
# You can create new repository and re-push
```

---

## Next Steps After Deployment

### Week 1: Monitoring
- [ ] Monitor Dependabot PRs
- [ ] Review any CodeQL findings
- [ ] Check Actions workflow results
- [ ] Verify no secret scanning alerts

### Week 2: Enhancement
- [ ] Set up additional workflows (deployment, release)
- [ ] Create labels for issues
- [ ] Populate project board with Phase 3+ tasks
- [ ] Add collaborators (if any)

### Month 1: Optimization
- [ ] Review and optimize CI/CD workflows
- [ ] Set up branch strategies (GitFlow, trunk-based, etc.)
- [ ] Configure GitHub Packages for NuGet (optional)
- [ ] Set up GitHub Pages for documentation (optional)

---

## Resources

**GitHub Documentation:**
- Quickstart: https://docs.github.com/en/get-started/quickstart
- Actions: https://docs.github.com/en/actions
- Security: https://docs.github.com/en/code-security
- Projects: https://docs.github.com/en/issues/planning-and-tracking-with-projects

**AlgoTrendy Specific:**
- See `GITHUB_TOOLS_IMPLEMENTATION_ROADMAP.md` for tool setup priorities
- See `README.md` for project overview
- See `PHASE_2_IMPLEMENTATION_COMPLETE.md` for latest features

---

## Support

**If you encounter issues:**

1. **GitHub Status:** https://www.githubstatus.com/
2. **GitHub Community:** https://github.community/
3. **GitHub Support:** https://support.github.com/
4. **Stack Overflow:** https://stackoverflow.com/questions/tagged/github

---

**Deployment Plan Version:** 1.0
**Last Updated:** October 19, 2025
**Status:** ✅ Ready for Execution
