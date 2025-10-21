# Documentation Automation Strategy

**Last Updated:** October 21, 2025
**Status:** ✅ **IMPLEMENTED** - Phase 1 automation is now active!

This guide outlines automated and semi-automated approaches to keeping AlgoTrendy documentation up-to-date.

---

## 🤖 Recommended Approach: Hybrid Automation

**Best Strategy:** Combine automated tools with human oversight

### Why Hybrid?
- ✅ **Automated:** Catches broken links, outdated code, API changes
- ✅ **Human:** Ensures quality, context, and accuracy
- ✅ **Efficient:** Saves 80% of manual work
- ✅ **Reliable:** Automated checks prevent errors

---

## 🛠️ Recommended Tools & Implementation

### 1. GitHub Actions - Automated Checks (FREE)

**What it does:**
- Checks for broken links
- Validates markdown syntax
- Ensures code examples compile
- Detects outdated dependencies

**Implementation:**

```yaml
# .github/workflows/docs-check.yml
name: Documentation Checks

on:
  push:
    paths:
      - 'docs/**'
      - '*.md'
      - 'backend/**/*.cs'
  pull_request:
    paths:
      - 'docs/**'
      - '*.md'

jobs:
  check-links:
    name: Check Documentation Links
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v4
      
      - name: Check Markdown Links
        uses: gaurav-nelson/github-action-markdown-link-check@v1
        with:
          use-quiet-mode: 'yes'
          config-file: '.github/markdown-link-check-config.json'
      
      - name: Check Dead Links
        uses: lycheeverse/lychee-action@v1
        with:
          args: --verbose --no-progress '**/*.md'
          fail: true

  lint-markdown:
    name: Lint Markdown Files
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v4
      
      - name: Markdown Lint
        uses: DavidAnson/markdownlint-cli2-action@v14
        with:
          globs: '**/*.md'

  validate-code-examples:
    name: Validate Code Examples
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v4
      
      - name: Setup .NET
        uses: actions/setup-dotnet@v4
        with:
          dotnet-version: '8.0.x'
      
      - name: Extract and Test C# Examples
        run: |
          # Extract C# code blocks from markdown
          python scripts/extract-code-examples.py
          # Compile them
          dotnet build extracted-examples/
      
      - name: Setup Python
        uses: actions/setup-python@v5
        with:
          python-version: '3.11'
      
      - name: Test Python Examples
        run: |
          pip install requests aiohttp
          python scripts/test-python-examples.py

  check-api-docs:
    name: Check API Documentation Sync
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v4
      
      - name: Generate OpenAPI Spec
        run: |
          cd backend
          dotnet tool install --global SwashbuckleCLI
          dotnet build
          swagger tofile --output openapi.json AlgoTrendy.API/bin/Debug/net8.0/AlgoTrendy.API.dll v1
      
      - name: Compare with Documented API
        run: |
          # Check if API_USAGE_EXAMPLES.md matches actual API
          python scripts/validate-api-docs.py

  check-outdated-content:
    name: Check for Outdated Content
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v4
        with:
          fetch-depth: 0  # Full history
      
      - name: Find Stale Documentation
        run: |
          # Find docs not updated in 6+ months
          python scripts/find-stale-docs.py
      
      - name: Comment on PR if Issues Found
        uses: actions/github-script@v7
        if: github.event_name == 'pull_request'
        with:
          script: |
            github.rest.issues.createComment({
              issue_number: context.issue.number,
              owner: context.repo.owner,
              repo: context.repo.repo,
              body: '⚠️ Some documentation may be outdated. Please review.'
            })
```

**Configuration File:**

```json
// .github/markdown-link-check-config.json
{
  "ignorePatterns": [
    {
      "pattern": "^http://localhost"
    },
    {
      "pattern": "^https://github.com.*#.*"
    }
  ],
  "timeout": "10s",
  "retryOn429": true,
  "retryCount": 3,
  "aliveStatusCodes": [200, 206, 301, 302]
}
```

---

### 2. Dependabot for Documentation Dependencies (FREE)

Already configured! Dependabot can also track documentation-related dependencies.

**Enhance `.github/dependabot.yml`:**

```yaml
# Documentation-related updates
- package-ecosystem: "github-actions"
  directory: "/"
  schedule:
    interval: "weekly"
  labels:
    - "dependencies"
    - "github-actions"
    - "documentation"
```

---

### 3. OpenAPI/Swagger Auto-Generation (Already Implemented!)

**What we have:**
- Swagger UI auto-generates from code
- SwaggerGen creates OpenAPI spec
- Examples in code sync automatically

**Enhancement:** Auto-generate client SDKs

```yaml
# .github/workflows/generate-clients.yml
name: Generate API Clients

on:
  push:
    branches: [main]
    paths:
      - 'backend/**/*.cs'

jobs:
  generate-clients:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v4
      
      - name: Setup .NET
        uses: actions/setup-dotnet@v4
        with:
          dotnet-version: '8.0.x'
      
      - name: Generate OpenAPI Spec
        run: |
          cd backend
          dotnet build
          dotnet swagger tofile --output ../openapi.json AlgoTrendy.API/bin/Debug/net8.0/AlgoTrendy.API.dll v1
      
      - name: Generate Python Client
        uses: openapi-generators/openapitools-generator-action@v1
        with:
          generator: python
          openapi-file: openapi.json
          config-file: .openapi-generator/python-config.json
          generator-tag: v7.0.1
      
      - name: Generate TypeScript Client
        uses: openapi-generators/openapitools-generator-action@v1
        with:
          generator: typescript-axios
          openapi-file: openapi.json
          config-file: .openapi-generator/typescript-config.json
      
      - name: Create PR with Updated Clients
        uses: peter-evans/create-pull-request@v5
        with:
          commit-message: 'docs: Update auto-generated API clients'
          title: 'Auto-generated API Client Update'
          body: 'This PR contains auto-generated API client updates based on latest API changes.'
          branch: auto-update-clients
```

---

### 4. Documentation Linter - Vale (RECOMMENDED)

**Vale** is the industry standard for style consistency.

**Installation:**

```bash
# Install Vale
brew install vale  # macOS
# or
sudo apt-get install vale  # Linux
```

**Configuration:**

```yaml
# .vale.ini
StylesPath = .vale/styles

MinAlertLevel = suggestion

[*.md]
BasedOnStyles = Microsoft, write-good
```

**GitHub Action:**

```yaml
# Add to .github/workflows/docs-check.yml
- name: Vale Linter
  uses: errata-ai/vale-action@v2
  with:
    files: docs
  env:
    GITHUB_TOKEN: ${{secrets.GITHUB_TOKEN}}
```

---

### 5. DocBot - AI-Powered Documentation Assistant (OPTIONAL)

**Best Options:**

#### Option A: GitHub Copilot for Docs (Paid - $10/month)
- Auto-suggests documentation updates
- Generates code examples
- Updates docs when code changes

#### Option B: Mintlify (FREE for open-source)
- https://mintlify.com
- Auto-generates docs from code
- AI-powered search
- Automatic updates

#### Option C: Docusaurus with Search (FREE)
- https://docusaurus.io
- Static site generator
- Algolia DocSearch (free for open-source)
- Versioning built-in

**Recommendation:** Start with GitHub Actions (free), add Mintlify if going open-source.

---

### 6. Custom Documentation Bot (DIY Approach)

**Create a bot that:**
1. Watches for code changes
2. Detects API modifications
3. Opens PRs with suggested doc updates

```python
# scripts/doc-bot.py
#!/usr/bin/env python3
"""
Documentation Bot - Automated documentation update suggestions
"""

import os
import re
from pathlib import Path
from github import Github

def detect_api_changes(diff_content):
    """Detect API endpoint changes in C# controllers"""
    changes = []
    
    # Look for new HttpGet, HttpPost, etc.
    endpoint_pattern = r'\[Http(Get|Post|Put|Delete|Patch)\("([^"]+)"\)\]'
    matches = re.findall(endpoint_pattern, diff_content)
    
    for method, route in matches:
        changes.append({
            'type': 'new_endpoint',
            'method': method,
            'route': route
        })
    
    return changes

def suggest_doc_updates(changes):
    """Suggest documentation updates based on code changes"""
    suggestions = []
    
    for change in changes:
        if change['type'] == 'new_endpoint':
            suggestions.append({
                'file': 'docs/API_USAGE_EXAMPLES.md',
                'action': 'add',
                'content': f"Document new {change['method']} endpoint: {change['route']}"
            })
    
    return suggestions

def create_documentation_pr(suggestions):
    """Create a PR with documentation update suggestions"""
    g = Github(os.getenv('GITHUB_TOKEN'))
    repo = g.get_repo('KenyBoi/algotrendy-v2.6')
    
    # Create branch
    base_branch = repo.get_branch('main')
    ref = f"refs/heads/auto-docs-{int(time.time())}"
    repo.create_git_ref(ref=ref, sha=base_branch.commit.sha)
    
    # Create PR
    pr_body = "## Auto-detected Documentation Updates\n\n"
    for suggestion in suggestions:
        pr_body += f"- [ ] {suggestion['action']} to `{suggestion['file']}`: {suggestion['content']}\n"
    
    pr = repo.create_pull(
        title="🤖 Suggested Documentation Updates",
        body=pr_body,
        head=ref.replace('refs/heads/', ''),
        base='main'
    )
    
    return pr

if __name__ == '__main__':
    # This would be triggered by GitHub Actions
    pass
```

---

## 📋 Recommended Implementation Plan

### Phase 1: Immediate ✅ **COMPLETE**
1. ✅ Add link checker GitHub Action - `.github/workflows/docs-check.yml`
2. ✅ Add markdown linter - Integrated in workflow
3. ✅ Create `.markdownlint.json` config - Root level configuration
4. ✅ Create link checker config - `.github/markdown-link-check-config.json`
5. ✅ Add stale documentation detection - Integrated in workflow

**What's Now Active:**
- 🔍 Automatic broken link detection on every PR
- 📝 Markdown linting for consistent formatting
- 📅 Weekly automated checks (Sunday midnight UTC)
- ⏰ Stale documentation alerts (6+ months old)
- 💬 Automatic PR comments for issues

### Phase 2: Short-term (Next 2 Weeks)
4. ⬜ Implement code example validation
5. ⬜ Add Vale style linter
6. ⬜ Create doc-bot script

### Phase 3: Medium-term (Next Month)
7. ⬜ Set up auto-generated clients
8. ⬜ Add API sync validation
9. ⬜ Implement API documentation validation

### Phase 4: Long-term (Next Quarter)
10. ⬜ Evaluate Mintlify or Docusaurus
11. ⬜ Implement AI-powered search
12. ⬜ Create interactive documentation

---

## 🎯 Automation Coverage

| Task | Automation Level | Tool |
|------|------------------|------|
| **Broken Links** | 100% Automated | GitHub Actions |
| **Markdown Lint** | 100% Automated | markdownlint |
| **Code Examples** | 80% Automated | Custom validator |
| **API Changes** | 70% Automated | OpenAPI diff |
| **Style Guide** | 90% Automated | Vale |
| **Outdated Content** | 60% Automated | Git-based checker |
| **Quality Review** | 20% Automated | Human required |

---

## 💰 Cost Analysis

### Free Options (Recommended)
- **GitHub Actions:** FREE (2000 min/month)
- **Dependabot:** FREE
- **markdownlint:** FREE
- **Vale:** FREE (open-source)
- **OpenAPI Generator:** FREE
- **Custom Scripts:** FREE

**Total Cost:** $0/month ✨

### Paid Options (Optional)
- **GitHub Copilot:** $10/month (individual) / $19/month (business)
- **Mintlify Pro:** $120/month
- **Algolia DocSearch:** FREE for open-source, $1/month for hobby

**Recommendation:** Start with free tools, upgrade only if needed.

---

## 🚀 Quick Start Implementation

### Step 1: Add Link Checker

```bash
mkdir -p .github/workflows
cat > .github/workflows/docs-check.yml << 'EOF'
name: Documentation Checks

on:
  pull_request:
    paths:
      - '**.md'
  schedule:
    - cron: '0 0 * * 0'  # Weekly on Sunday

jobs:
  check-links:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v4
      - name: Check Links
        uses: gaurav-nelson/github-action-markdown-link-check@v1
        with:
          use-quiet-mode: 'yes'
EOF
```

### Step 2: Add Markdown Linter

```bash
cat > .markdownlint.json << 'EOF'
{
  "default": true,
  "MD013": false,
  "MD033": false,
  "MD041": false
}
EOF
```

### Step 3: Add to CI

```yaml
# Add to existing .github/workflows/ci.yml
- name: Lint Documentation
  run: |
    npm install -g markdownlint-cli
    markdownlint '**/*.md' --ignore node_modules
```

### Step 4: Test It

```bash
# Commit and push
git add .github/workflows/docs-check.yml .markdownlint.json
git commit -m "docs: Add automated documentation checks"
git push
```

---

## 📊 Measuring Success

### Metrics to Track
- **Broken Links:** Should be 0
- **Documentation Coverage:** >95%
- **Staleness:** No docs >6 months old
- **Response Time:** Issues addressed in <48 hours
- **Contributor Friction:** <5 min to update docs

### Dashboard (GitHub Actions)
All metrics visible in:
- GitHub Actions tab
- PR checks
- README badges

---

## 🎓 Best Practices

### For Developers
1. **Update docs with code changes** (same PR)
2. **Test code examples** before committing
3. **Run linter locally** before pushing
4. **Check CI results** before merging

### For Maintainers
1. **Review auto-generated PRs** weekly
2. **Monitor broken links** dashboard
3. **Update automation** as needed
4. **Respond to doc issues** quickly

### For Community
1. **Report outdated docs** via issues
2. **Suggest improvements** via PRs
3. **Test examples** and report problems
4. **Share use cases** for better examples

---

## 🔮 Future Enhancements

### AI-Powered (2026+)
- GPT-4 powered doc suggestions
- Automatic example generation
- Smart cross-referencing
- Natural language search

### Advanced Automation
- Video tutorial generation
- Interactive playground
- Live API documentation
- Automatic translations

---

## ✅ Immediate Action Items

```bash
# 1. Add automation workflows
./scripts/setup-doc-automation.sh

# 2. Test locally
npm install -g markdownlint-cli
markdownlint '**/*.md'

# 3. Commit and enable
git add .github/workflows/
git commit -m "docs: Enable documentation automation"
git push
```

---

## 📚 Resources

### Tools
- **GitHub Actions:** https://docs.github.com/actions
- **markdownlint:** https://github.com/DavidAnson/markdownlint
- **Vale:** https://vale.sh
- **OpenAPI Generator:** https://openapi-generator.tech

### Guides
- **Write the Docs:** https://www.writethedocs.org/
- **Google Developer Docs Style:** https://developers.google.com/style
- **Microsoft Writing Style:** https://learn.microsoft.com/style-guide/

---

**Status:** Ready to implement  
**Effort:** 4-6 hours initial setup  
**Maintenance:** <1 hour/week  
**ROI:** Saves 10+ hours/month  

---

*Last Updated: October 21, 2025*
