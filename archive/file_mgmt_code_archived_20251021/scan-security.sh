#!/bin/bash
# AlgoTrendy Security Scanning Script
# Runs Gitleaks and Semgrep to find security vulnerabilities

set -e

# Color codes
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
NC='\033[0m' # No Color

echo -e "${BLUE}=================================="
echo "AlgoTrendy Security Scanner"
echo -e "==================================${NC}"
echo ""

# Check if tools are installed
if ! command -v gitleaks &> /dev/null; then
    echo -e "${RED}Error: Gitleaks is not installed${NC}"
    echo "Run './setup-security-tools.sh' to install required tools"
    exit 1
fi

if ! command -v semgrep &> /dev/null; then
    echo -e "${RED}Error: Semgrep is not installed${NC}"
    echo "Run './setup-security-tools.sh' to install required tools"
    exit 1
fi

# Create reports directory
REPORT_DIR="security-reports"
TIMESTAMP=$(date +%Y%m%d_%H%M%S)
mkdir -p "$REPORT_DIR"

echo -e "${YELLOW}Scan started at: $(date)${NC}"
echo ""

# 1. Run Gitleaks
echo -e "${BLUE}[1/2] Running Gitleaks (Secret Detection)...${NC}"
echo "Scanning for hardcoded credentials, API keys, and secrets..."

GITLEAKS_REPORT="$REPORT_DIR/gitleaks-report-$TIMESTAMP.json"
GITLEAKS_SUMMARY="$REPORT_DIR/gitleaks-summary-$TIMESTAMP.txt"

if gitleaks detect --no-git --report-format json --report-path "$GITLEAKS_REPORT" -v > "$GITLEAKS_SUMMARY" 2>&1; then
    echo -e "${GREEN}✓ No secrets detected by Gitleaks${NC}"
    GITLEAKS_FINDINGS=0
else
    GITLEAKS_FINDINGS=$(cat "$GITLEAKS_REPORT" | grep -c '"Description"' || echo "0")
    echo -e "${RED}✗ Gitleaks found $GITLEAKS_FINDINGS potential secrets${NC}"
fi

echo "Report saved to: $GITLEAKS_REPORT"
echo ""

# 2. Run Semgrep
echo -e "${BLUE}[2/2] Running Semgrep (Security Analysis)...${NC}"
echo "Scanning for security vulnerabilities and code issues..."

SEMGREP_REPORT="$REPORT_DIR/semgrep-report-$TIMESTAMP.json"
SEMGREP_SUMMARY="$REPORT_DIR/semgrep-summary-$TIMESTAMP.txt"

semgrep scan --config=auto --json --output="$SEMGREP_REPORT" . > "$SEMGREP_SUMMARY" 2>&1 || true

SEMGREP_FINDINGS=$(python3 -c "import json; data=json.load(open('$SEMGREP_REPORT')); print(len(data.get('results', [])))" 2>/dev/null || echo "0")

if [ "$SEMGREP_FINDINGS" -eq 0 ]; then
    echo -e "${GREEN}✓ No issues detected by Semgrep${NC}"
else
    echo -e "${YELLOW}⚠ Semgrep found $SEMGREP_FINDINGS potential issues${NC}"
fi

echo "Report saved to: $SEMGREP_REPORT"
echo ""

# 3. Generate Summary Report
echo -e "${BLUE}Generating summary report...${NC}"

SUMMARY_REPORT="$REPORT_DIR/SECURITY_SUMMARY_$TIMESTAMP.md"

cat > "$SUMMARY_REPORT" << EOF
# Security Scan Summary
**Generated:** $(date)
**Tools:** Gitleaks v$(gitleaks version), Semgrep v$(semgrep --version)

---

## Results Overview

| Tool | Findings | Status |
|------|----------|--------|
| Gitleaks | $GITLEAKS_FINDINGS | $([ "$GITLEAKS_FINDINGS" -eq 0 ] && echo "✅ PASS" || echo "❌ FAIL") |
| Semgrep | $SEMGREP_FINDINGS | $([ "$SEMGREP_FINDINGS" -eq 0 ] && echo "✅ PASS" || echo "⚠️ REVIEW") |

---

## Detailed Reports

- Gitleaks JSON: \`$GITLEAKS_REPORT\`
- Gitleaks Summary: \`$GITLEAKS_SUMMARY\`
- Semgrep JSON: \`$SEMGREP_REPORT\`
- Semgrep Summary: \`$SEMGREP_SUMMARY\`

---

## Gitleaks Analysis

EOF

if [ "$GITLEAKS_FINDINGS" -gt 0 ]; then
    echo "Analyzing Gitleaks findings..."
    python3 << 'PYTHON_SCRIPT' >> "$SUMMARY_REPORT"
import json
import sys

try:
    with open('$GITLEAKS_REPORT') as f:
        data = json.load(f)

    rules = {}
    files = {}
    for finding in data:
        rule = finding.get('RuleID', 'unknown')
        file = finding.get('File', 'unknown')
        rules[rule] = rules.get(rule, 0) + 1
        files[file] = files.get(file, 0) + 1

    print('### Findings by Rule Type\n')
    print('| Rule | Count |')
    print('|------|-------|')
    for rule, count in sorted(rules.items(), key=lambda x: x[1], reverse=True):
        print(f'| {rule} | {count} |')

    print('\n### Top 10 Files\n')
    print('| File | Findings |')
    print('|------|----------|')
    for file, count in sorted(files.items(), key=lambda x: x[1], reverse=True)[:10]:
        print(f'| {file} | {count} |')
except Exception as e:
    print(f'Error analyzing Gitleaks report: {e}')
PYTHON_SCRIPT
else
    echo "No secrets detected." >> "$SUMMARY_REPORT"
fi

cat >> "$SUMMARY_REPORT" << EOF

---

## Semgrep Analysis

EOF

if [ "$SEMGREP_FINDINGS" -gt 0 ]; then
    echo "Analyzing Semgrep findings..."
    python3 << 'PYTHON_SCRIPT' >> "$SUMMARY_REPORT"
import json

try:
    with open('$SEMGREP_REPORT') as f:
        data = json.load(f)

    findings = data.get('results', [])

    by_severity = {}
    by_file = {}

    for f in findings:
        sev = f.get('extra', {}).get('severity', 'UNKNOWN')
        file = f.get('path', 'unknown')
        by_severity[sev] = by_severity.get(sev, 0) + 1
        by_file[file] = by_file.get(file, 0) + 1

    print('### Findings by Severity\n')
    print('| Severity | Count |')
    print('|----------|-------|')
    for sev in ['ERROR', 'WARNING', 'INFO', 'UNKNOWN']:
        if sev in by_severity:
            print(f'| {sev} | {by_severity[sev]} |')

    print('\n### Top 10 Files\n')
    print('| File | Findings |')
    print('|------|----------|')
    for file, count in sorted(by_file.items(), key=lambda x: x[1], reverse=True)[:10]:
        print(f'| {file} | {count} |')
except Exception as e:
    print(f'Error analyzing Semgrep report: {e}')
PYTHON_SCRIPT
else
    echo "No issues detected." >> "$SUMMARY_REPORT"
fi

cat >> "$SUMMARY_REPORT" << EOF

---

## Recommendations

EOF

if [ "$GITLEAKS_FINDINGS" -gt 0 ] || [ "$SEMGREP_FINDINGS" -gt 0 ]; then
    cat >> "$SUMMARY_REPORT" << EOF
1. **Review all findings** in the detailed reports
2. **Prioritize CRITICAL and ERROR severity** issues
3. **Revoke exposed credentials** immediately
4. **Implement fixes** based on severity
5. **Set up pre-commit hooks** to prevent future issues
6. **Re-run this scan** after fixes to verify

See \`SECURITY_SCAN_REPORT.md\` for detailed remediation guidance.
EOF
else
    cat >> "$SUMMARY_REPORT" << EOF
🎉 **No security issues detected!**

Continue to:
- Run scans regularly (weekly recommended)
- Keep security tools updated
- Review new code for security issues
- Maintain pre-commit hooks
EOF
fi

echo "Summary report saved to: $SUMMARY_REPORT"
echo ""

# Display summary
echo -e "${BLUE}=================================="
echo "Scan Summary"
echo -e "==================================${NC}"
echo ""
echo "Gitleaks findings: $GITLEAKS_FINDINGS"
echo "Semgrep findings: $SEMGREP_FINDINGS"
echo ""

if [ "$GITLEAKS_FINDINGS" -eq 0 ] && [ "$SEMGREP_FINDINGS" -eq 0 ]; then
    echo -e "${GREEN}✓ All scans passed! No security issues detected.${NC}"
    exit 0
elif [ "$GITLEAKS_FINDINGS" -gt 0 ]; then
    echo -e "${RED}✗ CRITICAL: Secrets detected in repository!${NC}"
    echo "  Review: $GITLEAKS_REPORT"
    echo ""
    echo "Immediate actions required:"
    echo "  1. Revoke all exposed credentials"
    echo "  2. Remove secrets from code"
    echo "  3. Use environment variables or secret management"
    exit 1
else
    echo -e "${YELLOW}⚠ Security issues detected. Review required.${NC}"
    echo "  Review: $SEMGREP_REPORT"
    exit 1
fi
