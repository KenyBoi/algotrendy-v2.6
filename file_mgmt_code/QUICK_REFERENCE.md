# Security Tools Quick Reference

## 🚀 Common Commands

### Run Security Scan
```bash
cd /root/AlgoTrendy_v2.6/file_mgmt_code
./scan-security.sh
```

### View Latest Report
```bash
ls -lt security-reports/ | head -5
cat security-reports/SECURITY_SUMMARY_*.md | head -50
```

### Manual Scans

#### Gitleaks Only
```bash
# Scan current directory
gitleaks detect --no-git -v

# Scan with report
gitleaks detect --no-git --report-format json --report-path report.json

# Scan staged files (pre-commit)
gitleaks protect --staged
```

#### Semgrep Only
```bash
# Auto scan (recommended)
semgrep scan --config=auto .

# Specific rulesets
semgrep scan --config=p/security-audit .
semgrep scan --config=p/owasp-top-ten .
semgrep scan --config=p/cwe-top-25 .

# Save to file
semgrep scan --config=auto --json -o report.json .
```

## 🔍 Common Search Patterns

### Find API Keys
```bash
grep -rn "api[_-]key" --include="*.cs" --include="*.json" .
grep -rn "API_KEY" --include="*.env" .
```

### Find Passwords
```bash
grep -rn "password\s*=" --include="*.cs" --include="*.json" .
grep -rn "PASSWORD" --include="*.env" .
```

### Find Hardcoded Secrets
```bash
gitleaks detect --no-git -v | grep -A 3 "Secret:"
```

## 🔧 Fix Common Issues

### Remove .env from Git
```bash
# Stop tracking .env
git rm --cached .env

# Add to gitignore
echo ".env" >> .gitignore

# Commit changes
git add .gitignore
git commit -m "security: Remove .env from version control"
```

### Create .env.example
```bash
# Copy .env to .env.example
cp .env .env.example

# Remove all sensitive values manually, replace with placeholders
# Example: BINANCE_API_KEY=your_binance_api_key_here

# Add to git
git add .env.example
git commit -m "docs: Add .env.example template"
```

### Clean Git History (DANGEROUS - Rewrites history)
```bash
# Install BFG Repo Cleaner
wget https://repo1.maven.org/maven2/com/madgag/bfg/1.14.0/bfg-1.14.0.jar

# Remove .env from all commits
java -jar bfg-1.14.0.jar --delete-files .env

# Clean up
git reflog expire --expire=now --all
git gc --prune=now --aggressive

# Force push (requires team coordination!)
# git push origin --force --all
```

## 🔒 Secret Management Examples

### .NET User Secrets
```bash
# Initialize
cd backend/AlgoTrendy.API
dotnet user-secrets init

# Set secrets
dotnet user-secrets set "Binance:ApiKey" "your-key"
dotnet user-secrets set "Binance:ApiSecret" "your-secret"

# List secrets
dotnet user-secrets list

# Remove secret
dotnet user-secrets remove "Binance:ApiKey"

# Clear all secrets
dotnet user-secrets clear
```

### Environment Variables (Docker)
```yaml
# docker-compose.yml
services:
  api:
    environment:
      - BINANCE_API_KEY=${BINANCE_API_KEY}
      - BINANCE_API_SECRET=${BINANCE_API_SECRET}
    env_file:
      - .env  # Make sure .env is in .gitignore!
```

### Azure Key Vault
```bash
# Create Key Vault
az keyvault create --name "AlgoTrendyVault" --resource-group "AlgoTrendy" --location "eastus"

# Add secrets
az keyvault secret set --vault-name "AlgoTrendyVault" --name "BinanceApiKey" --value "your-key"

# Get secret
az keyvault secret show --vault-name "AlgoTrendyVault" --name "BinanceApiKey"

# In C# code:
# var client = new SecretClient(new Uri(vaultUri), new DefaultAzureCredential());
# var secret = await client.GetSecretAsync("BinanceApiKey");
```

### AWS Secrets Manager
```bash
# Create secret
aws secretsmanager create-secret \
  --name AlgoTrendy/Binance/ApiKey \
  --secret-string "your-key"

# Retrieve secret
aws secretsmanager get-secret-value \
  --secret-id AlgoTrendy/Binance/ApiKey
```

## 🛡️ Pre-commit Hooks

### Install
```bash
./setup-precommit-hooks.sh
```

### Run Manually
```bash
# Run on all files
pre-commit run --all-files

# Run on staged files
pre-commit run

# Run specific hook
pre-commit run gitleaks
pre-commit run semgrep
```

### Update Hooks
```bash
pre-commit autoupdate
```

### Bypass (NOT RECOMMENDED)
```bash
git commit --no-verify -m "message"
```

## 📊 Analysis Commands

### Gitleaks Statistics
```bash
# Count findings by type
cat gitleaks-report.json | jq '.[] | .RuleID' | sort | uniq -c | sort -rn

# List all files with secrets
cat gitleaks-report.json | jq '.[] | .File' | sort -u

# Find high-entropy secrets
cat gitleaks-report.json | jq '.[] | select(.Entropy > 4.5) | {File, Line, Secret}'
```

### Semgrep Statistics
```bash
# Count by severity
cat semgrep-report.json | jq '.results[] | .extra.severity' | sort | uniq -c

# Count by category
cat semgrep-report.json | jq '.results[] | .extra.metadata.category' | sort | uniq -c

# List ERROR severity issues
cat semgrep-report.json | jq '.results[] | select(.extra.severity=="ERROR") | {file: .path, line: .start.line, message: .extra.message}'
```

## 🔄 CI/CD Integration

### GitHub Actions
```yaml
# .github/workflows/security.yml
name: Security Scan
on: [push, pull_request]
jobs:
  security:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v3
      - name: Gitleaks
        uses: gitleaks/gitleaks-action@v2
      - name: Semgrep
        uses: returntocorp/semgrep-action@v1
```

### GitLab CI
```yaml
# .gitlab-ci.yml
security_scan:
  stage: test
  script:
    - ./file_mgmt_code/scan-security.sh
  artifacts:
    paths:
      - security-reports/
```

### Jenkins
```groovy
stage('Security Scan') {
  steps {
    sh './file_mgmt_code/scan-security.sh'
    publishHTML([
      reportDir: 'security-reports',
      reportFiles: 'SECURITY_SUMMARY_*.md',
      reportName: 'Security Scan Report'
    ])
  }
}
```

## 🆘 Emergency Response

### Leaked Credentials Discovered

1. **Immediately revoke the credentials**
2. **Generate new credentials**
3. **Remove from repository**
4. **Clean git history** (if public repo)
5. **Monitor accounts** for unauthorized access
6. **Update documentation**
7. **Notify team**

### Script for Emergency Cleanup
```bash
#!/bin/bash
# emergency-cleanup.sh

echo "EMERGENCY CLEANUP - Leaked Credentials"
echo "======================================"

# 1. Remove from working directory
rm -f .env
rm -f *.key
rm -f *.pem

# 2. Add to gitignore
cat >> .gitignore << EOF
.env
.env.local
.env.*.local
*.key
*.pem
secrets/
EOF

# 3. Remove from git cache
git rm --cached .env 2>/dev/null
git rm --cached *.key 2>/dev/null
git rm --cached *.pem 2>/dev/null

# 4. Commit changes
git add .gitignore
git commit -m "security: Emergency cleanup of leaked credentials"

echo ""
echo "NEXT STEPS:"
echo "1. Revoke all exposed credentials from provider dashboards"
echo "2. Generate new credentials"
echo "3. Update production deployments"
echo "4. Clean git history (use BFG or git filter-branch)"
echo "5. Force push (if repo is private and team is coordinated)"
```

## 📞 Quick Links

- **Gitleaks Docs:** https://github.com/gitleaks/gitleaks
- **Semgrep Docs:** https://semgrep.dev/docs/
- **OWASP Top 10:** https://owasp.org/www-project-top-ten/
- **CWE Top 25:** https://cwe.mitre.org/top25/
- **Secret Management:** https://cheatsheetseries.owasp.org/cheatsheets/Secrets_Management_Cheat_Sheet.html

## 🎯 Regular Maintenance

### Weekly
- [ ] Run `./scan-security.sh`
- [ ] Review new findings
- [ ] Update security tools

### Monthly
- [ ] Review and update .gitignore
- [ ] Audit secret management practices
- [ ] Review access to production secrets
- [ ] Update dependencies

### Quarterly
- [ ] Security training for team
- [ ] Review and update security policies
- [ ] Penetration testing
- [ ] Compliance audit

---

*Keep this reference handy for quick security checks!*
