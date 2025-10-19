# Open Source Software Quality & Analysis Tools

**Generated:** 2025-10-18
**Purpose:** Tools to detect disconnections, missing files, broken dependencies, and general problems in software projects

---

## 📊 Table of Contents

1. [Dependency Scanners & Analyzers](#dependency-scanners--analyzers)
2. [Static Code Analysis Tools](#static-code-analysis-tools)
3. [Import & Module Checkers](#import--module-checkers)
4. [Circular Dependency Detectors](#circular-dependency-detectors)
5. [Project Health & Quality Metrics](#project-health--quality-metrics)
6. [Recommendations for AlgoTrendy](#recommendations-for-algotrendy)

---

## 1. Dependency Scanners & Analyzers

### 🏆 OWASP Dependency-Check
**Purpose:** Software Composition Analysis (SCA) - identifies project dependencies and known vulnerabilities

**Features:**
- Multi-language support (Java, .NET, Python, Ruby, Node.js, etc.)
- Command-line utility with plugins for Maven, Gradle, etc.
- Auto-updates from NIST NVD database
- Generates detailed reports

**Installation:**
```bash
# Via npm
npm install -g @owasp/dependency-check

# Via Docker
docker pull owasp/dependency-check

# Via Homebrew (macOS)
brew install dependency-check
```

**Usage:**
```bash
# Scan current directory
dependency-check --scan . --format HTML --out ./reports

# Scan Python project
dependency-check --scan . --enableExperimental --format JSON
```

**Website:** https://owasp.org/www-project-dependency-check/

---

### OWASP Dependency-Track
**Purpose:** Continuous vulnerability tracking platform with web UI

**Features:**
- Web-based dashboard
- Continuous monitoring over time
- Integration with CI/CD pipelines
- RESTful API
- SBOM (Software Bill of Materials) support

**Installation:**
```bash
# Via Docker Compose
curl -LO https://dependencytrack.org/docker-compose.yml
docker-compose up -d
```

**Access:** http://localhost:8080

**Website:** https://dependencytrack.org/

---

### FOSSLight Dependency Scanner
**Purpose:** Multi-package manager dependency analysis

**Features:**
- Auto-detects manifest files
- Supports multiple package managers
- OSS license compliance
- Generates comprehensive reports

**Installation:**
```bash
pip install fosslight_dependency
```

**Usage:**
```bash
fosslight_dependency -p /path/to/project
```

**Website:** https://fosslight.org/

---

### Snyk (Free for Open Source)
**Purpose:** Developer-first security scanning

**Features:**
- Real-time vulnerability detection
- Fix suggestions and automated PRs
- IDE integration (VS Code, IntelliJ)
- Container & IaC scanning

**Installation:**
```bash
npm install -g snyk
snyk auth
```

**Usage:**
```bash
# Test for vulnerabilities
snyk test

# Monitor project
snyk monitor
```

**Website:** https://snyk.io/

---

## 2. Static Code Analysis Tools

### 🏆 SonarQube (Community Edition)
**Purpose:** Comprehensive code quality & security analysis

**Features:**
- 29+ programming languages
- Detects bugs, vulnerabilities, code smells
- Technical debt tracking
- Quality gates for CI/CD
- Web-based dashboard

**Installation:**
```bash
# Via Docker
docker run -d --name sonarqube -p 9000:9000 sonarqube:lts-community
```

**Usage:**
```bash
# Scan with sonar-scanner
sonar-scanner \
  -Dsonar.projectKey=algotrendy \
  -Dsonar.sources=. \
  -Dsonar.host.url=http://localhost:9000
```

**Website:** https://www.sonarqube.org/

---

### MegaLinter
**Purpose:** All-in-one linter for consistency checking

**Features:**
- 100+ linters for 52 languages
- Analyzes code, IaC, configs, scripts
- GitHub/GitLab CI integration
- Auto-fixes common issues
- Detailed reports

**Installation:**
```bash
# Via npx
npx mega-linter-runner

# Via Docker
docker run -v /path/to/repo:/tmp/lint oxsecurity/megalinter:latest
```

**GitHub:** https://github.com/oxsecurity/megalinter

---

### CodeQL (GitHub)
**Purpose:** Semantic code analysis for security

**Features:**
- Deep code analysis
- Custom queries
- Supports C/C++, C#, Java, JavaScript/TypeScript, Python, Go, Ruby
- Free for open source on GitHub

**Setup:**
```yaml
# .github/workflows/codeql.yml
name: "CodeQL"
on: [push, pull_request]
jobs:
  analyze:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v3
      - uses: github/codeql-action/init@v2
      - uses: github/codeql-action/analyze@v2
```

**Website:** https://codeql.github.com/

---

### Pylint (Python)
**Purpose:** Python code analysis

**Features:**
- PEP 8 compliance checking
- Detects errors, duplicated code
- Refactoring suggestions
- Highly configurable

**Installation:**
```bash
pip install pylint
```

**Usage:**
```bash
# Check Python files
pylint algotrendy_v2.5/**/*.py

# Generate report
pylint --output-format=json algotrendy > pylint-report.json
```

**Website:** https://pylint.org/

---

## 3. Import & Module Checkers

### findimports (Python)
**Purpose:** Extract and analyze Python module dependencies

**Features:**
- Finds all imports in Python code
- Detects unused imports
- Generates dependency graphs
- Reports missing modules

**Installation:**
```bash
pip install findimports
```

**Usage:**
```bash
# Check imports
findimports algotrendy_v2.5/

# Show unused imports
findimports --unused algotrendy_v2.5/
```

**PyPI:** https://pypi.org/project/findimports/

---

### Vulture (Python)
**Purpose:** Find dead code and unused imports

**Features:**
- Detects unused functions, variables, imports
- Minimal false positives
- Fast execution
- Confidence scores

**Installation:**
```bash
pip install vulture
```

**Usage:**
```bash
# Find dead code
vulture algotrendy_v2.5/

# With minimum confidence
vulture --min-confidence 80 algotrendy_v2.5/
```

**GitHub:** https://github.com/jendrikseipp/vulture

---

### importchecker (Python)
**Purpose:** Find unused imports in Python modules

**Installation:**
```bash
pip install importchecker
```

**Usage:**
```bash
importchecker algotrendy_v2.5/
```

**GitHub:** https://github.com/zopefoundation/importchecker

---

### pyflakes (Python)
**Purpose:** Lightweight Python checker

**Features:**
- No configuration needed
- Fast execution
- Detects unused imports, variables
- Part of flake8

**Installation:**
```bash
pip install pyflakes
```

**Usage:**
```bash
pyflakes algotrendy_v2.5/
```

**Website:** https://github.com/PyCQA/pyflakes

---

## 4. Circular Dependency Detectors

### 🏆 madge (JavaScript/TypeScript)
**Purpose:** Create dependency graphs and detect circular dependencies

**Features:**
- Supports CommonJS, AMD, ES6 modules
- Works with TypeScript
- Visual graph generation (SVG, PNG, DOT)
- Circular dependency detection
- CSS preprocessor support (Sass, Stylus, Less)

**Installation:**
```bash
npm install -g madge
```

**Usage:**
```bash
# Detect circular dependencies
madge --circular src/

# Generate visual graph
madge --image graph.svg src/

# TypeScript project
madge --circular --extensions ts ./

# Export as JSON
madge --json src/ > dependencies.json
```

**GitHub:** https://github.com/pahen/madge

**Example Output:**
```
Circular dependencies:
/src/moduleA.js > /src/moduleB.js
/src/moduleB.js > /src/moduleA.js
```

---

### Skott (Next-gen madge)
**Purpose:** Modern alternative to madge with enhanced features

**Features:**
- Better performance
- Enhanced visualization
- Plugin system
- More detailed analysis

**Installation:**
```bash
npm install -g skott
```

**Usage:**
```bash
skott --circular
skott --graph
```

**Website:** https://github.com/antoine-coulon/skott

---

### dpdm (JavaScript/TypeScript)
**Purpose:** Detect circular dependencies in modules

**Installation:**
```bash
npm install -g dpdm
```

**Usage:**
```bash
# Check for circular deps
dpdm --circular src/index.js

# Generate tree view
dpdm src/index.js
```

**GitHub:** https://github.com/acrazing/dpdm

---

## 5. Project Health & Quality Metrics

### CHAOSS GrimoireLab
**Purpose:** Open source community health analytics

**Features:**
- Comprehensive metrics dashboard
- Git, GitHub, GitLab, Jira integration
- Community activity tracking
- Visualization with Kibana
- Data collection and analysis

**Installation:**
```bash
# Via Docker Compose
git clone https://github.com/chaoss/grimoirelab
cd grimoirelab/docker-compose
docker-compose up -d
```

**Website:** https://chaoss.community/

---

### Cauldron
**Purpose:** Centralized OSS community metrics

**Features:**
- No deployment required
- Web-based interface
- Scalable data collection
- Project comparison

**Website:** https://cauldron.io/

---

### Augur
**Purpose:** Community health metrics and analytics

**Features:**
- Risk metrics
- Value metrics
- Activity metrics
- RESTful API

**Installation:**
```bash
git clone https://github.com/chaoss/augur.git
cd augur
make install
```

**GitHub:** https://github.com/chaoss/augur

---

## 6. Recommendations for AlgoTrendy

### Essential Tools to Use

#### For Dependency Management
```bash
# 1. Install OWASP Dependency-Check
npm install -g @owasp/dependency-check

# Scan v2.5
cd /root/algotrendy_v2.5
dependency-check --scan . --format HTML --out ./security-reports

# Scan v2.6
cd /root/AlgoTrendy_v2.6
dependency-check --scan . --format HTML --out ./security-reports
```

#### For Python Code Quality
```bash
# Install analysis tools
pip install pylint pyflakes vulture findimports

# Check for issues
pylint algotrendy_v2.5/**/*.py
vulture algotrendy_v2.5/
findimports --unused algotrendy_v2.5/
```

#### For Circular Dependencies (if you have JS/TS)
```bash
# Install madge
npm install -g madge

# Check frontend code
cd /root/AlgoTrendy_v2.6/frontend
madge --circular --extensions ts,tsx src/
madge --image dependency-graph.svg src/
```

#### For Overall Code Quality
```bash
# Run SonarQube
docker run -d --name sonarqube -p 9000:9000 sonarqube:lts-community

# Scan project
sonar-scanner \
  -Dsonar.projectKey=algotrendy_v26 \
  -Dsonar.sources=. \
  -Dsonar.host.url=http://localhost:9000
```

---

### Quick Start Script

Create `/root/AlgoTrendy_v2.6/run_quality_checks.sh`:

```bash
#!/bin/bash

echo "🔍 Running Quality Checks on AlgoTrendy v2.6"
echo "============================================="

# Python checks
echo "
📊 1. Checking Python code quality..."
pip install -q pylint vulture findimports
pylint debt_mgmt_module/**/*.py > reports/pylint.txt
vulture debt_mgmt_module/ > reports/vulture.txt
findimports --unused debt_mgmt_module/ > reports/unused-imports.txt

# Dependency security scan
echo "
🔒 2. Scanning dependencies for vulnerabilities..."
dependency-check --scan . --format HTML --out ./reports

# JavaScript/TypeScript checks (if applicable)
if [ -d "frontend" ]; then
  echo "
🔄 3. Checking for circular dependencies..."
  madge --circular --extensions ts,tsx frontend/src/ > reports/circular-deps.txt
fi

# Summary
echo "
✅ Quality checks complete!"
echo "Reports saved to ./reports/"
echo "
📈 Review:"
echo "  - pylint.txt          - Python code issues"
echo "  - vulture.txt         - Dead code detection"
echo "  - unused-imports.txt  - Import analysis"
echo "  - dependency-check-report.html - Security vulnerabilities"
echo "  - circular-deps.txt   - Circular dependencies"
```

---

### Integration with CI/CD

**GitHub Actions Example:**

Create `.github/workflows/quality-check.yml`:

```yaml
name: Quality Check

on: [push, pull_request]

jobs:
  python-quality:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v3

      - name: Set up Python
        uses: actions/setup-python@v4
        with:
          python-version: '3.11'

      - name: Install tools
        run: |
          pip install pylint vulture findimports

      - name: Run Pylint
        run: pylint **/*.py

      - name: Check for dead code
        run: vulture .

      - name: Check imports
        run: findimports --unused .

  dependency-scan:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v3

      - name: Run OWASP Dependency-Check
        uses: dependency-check/Dependency-Check_Action@main
        with:
          project: 'AlgoTrendy'
          path: '.'
          format: 'HTML'

      - name: Upload Results
        uses: actions/upload-artifact@v3
        with:
          name: dependency-check-report
          path: reports/

  sonarqube:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v3

      - name: SonarQube Scan
        uses: sonarsource/sonarqube-scan-action@master
        env:
          SONAR_TOKEN: ${{ secrets.SONAR_TOKEN }}
          SONAR_HOST_URL: ${{ secrets.SONAR_HOST_URL }}
```

---

## Summary Table

| Tool | Type | Language | Best For | Free? |
|------|------|----------|----------|-------|
| **OWASP Dependency-Check** | Security | Multi | Vulnerability scanning | ✅ Yes |
| **SonarQube** | Quality | Multi | Overall code quality | ✅ Community |
| **Pylint** | Linting | Python | Python code standards | ✅ Yes |
| **madge** | Analysis | JS/TS | Circular dependencies | ✅ Yes |
| **Vulture** | Analysis | Python | Dead code detection | ✅ Yes |
| **MegaLinter** | Linting | Multi | All-in-one linting | ✅ Yes |
| **CodeQL** | Security | Multi | Security analysis | ✅ OSS |
| **Snyk** | Security | Multi | Dev-friendly scanning | ✅ Free tier |
| **GrimoireLab** | Metrics | N/A | Project health | ✅ Yes |

---

## Next Steps for AlgoTrendy

1. **Install essential tools:**
   ```bash
   pip install pylint vulture findimports
   npm install -g madge @owasp/dependency-check
   ```

2. **Run initial scan:**
   ```bash
   bash run_quality_checks.sh
   ```

3. **Review reports:**
   - Fix critical security vulnerabilities
   - Remove dead code
   - Fix circular dependencies
   - Address code quality issues

4. **Set up CI/CD:**
   - Add quality checks to GitHub Actions
   - Block PRs with critical issues
   - Track metrics over time

5. **Regular monitoring:**
   - Weekly dependency scans
   - Monthly code quality reviews
   - Quarterly health metrics

---

**Document Version:** 1.0
**Last Updated:** 2025-10-18
**For Project:** AlgoTrendy v2.6
