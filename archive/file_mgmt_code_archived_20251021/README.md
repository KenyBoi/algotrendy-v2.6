# AlgoTrendy Security Tools

This directory contains security scanning tools and scripts for the AlgoTrendy project.

## 📋 Contents

1. **SECURITY_SCAN_REPORT.md** - Comprehensive security scan report with findings and remediation steps
2. **setup-security-tools.sh** - Installs Gitleaks and Semgrep
3. **scan-security.sh** - Runs security scans and generates reports
4. **setup-precommit-hooks.sh** - Sets up automated pre-commit security checks
5. **README.md** - This file

## 🚀 Quick Start

### Initial Setup (One-time)

```bash
# 1. Install the security tools
chmod +x *.sh
./setup-security-tools.sh

# 2. Set up pre-commit hooks (prevents committing secrets)
./setup-precommit-hooks.sh

# 3. Run your first security scan
./scan-security.sh
```

### Regular Usage

```bash
# Run security scan (recommended: weekly or before releases)
./scan-security.sh

# View latest report
ls -lt security-reports/
cat security-reports/SECURITY_SUMMARY_*.md
```

## 🛠️ Tools Included

### Gitleaks (Secret Detection)
- **Purpose:** Detects hardcoded credentials, API keys, tokens, passwords
- **Speed:** ~30 seconds
- **Version:** 8.18.2

**What it finds:**
- API keys and secrets
- Passwords
- Private keys (SSL/TLS certificates)
- AWS/Azure/GCP credentials
- Database connection strings
- JWT tokens

### Semgrep (Security Analysis)
- **Purpose:** Static Application Security Testing (SAST)
- **Speed:** ~2-5 minutes
- **Version:** 1.140.0

**What it finds:**
- Security vulnerabilities
- Code quality issues
- Docker security problems
- Insecure configurations
- OWASP Top 10 violations

## 📊 Understanding Reports

### Report Files

After running `./scan-security.sh`, you'll find:

```
security-reports/
├── gitleaks-report-TIMESTAMP.json      # Raw Gitleaks findings
├── gitleaks-summary-TIMESTAMP.txt      # Gitleaks console output
├── semgrep-report-TIMESTAMP.json       # Raw Semgrep findings
├── semgrep-summary-TIMESTAMP.txt       # Semgrep console output
└── SECURITY_SUMMARY_TIMESTAMP.md       # Combined summary report
```

### Severity Levels

| Level | Action Required | Timeline |
|-------|----------------|----------|
| CRITICAL | Immediate | Within 24 hours |
| ERROR | High priority | Within 1 week |
| WARNING | Medium priority | Within 1 month |
| INFO | Low priority | As time permits |

## 🔴 Critical Issues Found

**Current scan identified:**
- ❌ 10 hardcoded credentials in `.env` file
- ❌ 3 API keys in production configuration files
- ❌ 4 private SSL/TLS keys in repository
- ❌ 18 Docker security issues

**See SECURITY_SCAN_REPORT.md for detailed remediation steps.**

## ✅ Remediation Checklist

### Priority 1: IMMEDIATE

- [ ] Revoke all exposed API keys
  - [ ] Binance API keys
  - [ ] Databento API key
  - [ ] Finnhub API key
  - [ ] API keys in appsettings.json
- [ ] Regenerate SSL certificates for algotrendy.com
- [ ] Remove `.env` from git repository
- [ ] Add `.env` to `.gitignore`
- [ ] Clean git history (optional but recommended)

### Priority 2: HIGH

- [ ] Set up secret management
  - [ ] Development: ASP.NET Core User Secrets
  - [ ] Production: Azure Key Vault / AWS Secrets Manager
- [ ] Fix Docker security issues
  - [ ] Add USER directive to Dockerfiles
  - [ ] Run containers as non-root
- [ ] Replace ws:// with wss:// for WebSockets
- [ ] Update test credentials to use environment variables

### Priority 3: MEDIUM

- [ ] Add security scanning to CI/CD pipeline
- [ ] Create SECURITY.md documentation
- [ ] Set up automated security alerts
- [ ] Conduct security training for team

## 🔒 Best Practices

### Never Commit These:

- ❌ API keys or secrets
- ❌ Passwords or credentials
- ❌ Private keys or certificates
- ❌ Database connection strings with passwords
- ❌ `.env` files with real values

### Always Do This:

- ✅ Use environment variables
- ✅ Use secret management tools
- ✅ Create `.env.example` templates
- ✅ Add sensitive files to `.gitignore`
- ✅ Run security scans before committing
- ✅ Review pre-commit hook failures

## 📚 Secret Management

### For Development

```bash
# .NET Projects - Use User Secrets
cd backend/AlgoTrendy.API
dotnet user-secrets init
dotnet user-secrets set "Binance:ApiKey" "your-key-here"

# Python Projects - Use .env (gitignored)
cp .env.example .env
# Edit .env with your local credentials
```

### For Production

```bash
# Option 1: Environment Variables
export BINANCE_API_KEY="your-key"
docker-compose up -d

# Option 2: Azure Key Vault (recommended)
az keyvault secret set --vault-name "AlgoTrendyVault" \
  --name "BinanceApiKey" --value "your-key"

# Option 3: AWS Secrets Manager
aws secretsmanager create-secret \
  --name AlgoTrendy/Binance/ApiKey \
  --secret-string "your-key"
```

## 🔄 CI/CD Integration

### GitHub Actions Example

```yaml
name: Security Scan

on: [push, pull_request]

jobs:
  security:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v3

      - name: Run Gitleaks
        uses: gitleaks/gitleaks-action@v2
        env:
          GITHUB_TOKEN: ${{ secrets.GITHUB_TOKEN }}

      - name: Run Semgrep
        uses: returntocorp/semgrep-action@v1
```

### GitLab CI Example

```yaml
security_scan:
  stage: test
  script:
    - ./file_mgmt_code/scan-security.sh
  artifacts:
    reports:
      security-reports/
```

## 🆘 Troubleshooting

### Gitleaks Fails During Pre-commit

```bash
# View what was detected
gitleaks protect --verbose --redact --staged

# Fix the issue (remove secret, use environment variable)
# Then commit again
```

### Semgrep False Positives

```bash
# Add to .semgrepignore
echo "path/to/file" >> .semgrepignore

# Or use inline comments in code
// nosemgrep: rule-id
```

### Skip Hooks Temporarily (NOT RECOMMENDED)

```bash
git commit --no-verify -m "commit message"
```

## 📞 Support

### Resources

- **Gitleaks Documentation:** https://github.com/gitleaks/gitleaks
- **Semgrep Documentation:** https://semgrep.dev/docs/
- **OWASP Top 10:** https://owasp.org/www-project-top-ten/
- **AlgoTrendy Security Report:** See SECURITY_SCAN_REPORT.md

### Getting Help

1. Review SECURITY_SCAN_REPORT.md for detailed guidance
2. Check tool documentation for specific findings
3. Consult security team for critical issues
4. Follow OWASP guidelines for secure coding

## 📈 Metrics

**Current Repository Status:**
- Total Findings: 180 (95 Gitleaks + 85 Semgrep)
- Critical Issues: 55
- Security Score: 28/100 (CRITICAL)

**Goal:** Achieve 90+ security score within 1 month

## 🎯 Next Steps

1. **Today:** Review SECURITY_SCAN_REPORT.md
2. **This Week:** Complete Priority 1 remediation
3. **This Month:** Implement all security improvements
4. **Ongoing:** Run weekly scans and maintain security

---

*Last updated: 2025-10-21*
*Tools version: Gitleaks v8.18.2, Semgrep v1.140.0*
