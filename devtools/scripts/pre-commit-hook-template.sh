#!/bin/bash
#
# Pre-Commit Hook Template
# Prevents commits that break the build or contain secrets
#
# Installation:
#   cp devtools/scripts/pre-commit-hook-template.sh .git/hooks/pre-commit
#   chmod +x .git/hooks/pre-commit
#

set -e

# Colors
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
NC='\033[0m'

PROJECT_ROOT="/root/AlgoTrendy_v2.6"
BACKEND_DIR="$PROJECT_ROOT/backend"

echo ""
echo "========================================"
echo "Pre-Commit Checks"
echo "========================================"
echo ""

CHECKS_FAILED=0

# Check 1: Scan for secrets
echo "1. Scanning for secrets..."

SECRET_PATTERNS=(
    "api_key.*=.*['\"][a-zA-Z0-9]{20,}['\"]"
    "secret.*=.*['\"][a-zA-Z0-9]{20,}['\"]"
    "password.*=.*['\"][a-zA-Z0-9]{8,}['\"]"
    "private_key"
    "BEGIN (RSA|DSA|EC) PRIVATE KEY"
)

for pattern in "${SECRET_PATTERNS[@]}"; do
    if git diff --cached | grep -iE "$pattern" > /dev/null 2>&1; then
        echo -e "${RED}❌ Potential secret detected: $pattern${NC}"
        echo ""
        echo "Found in:"
        git diff --cached | grep -iE "$pattern" --color=always
        echo ""
        CHECKS_FAILED=1
    fi
done

# Check 2: .env files
if git diff --cached --name-only | grep "\.env$" > /dev/null 2>&1; then
    echo -e "${RED}❌ .env file staged for commit:${NC}"
    git diff --cached --name-only | grep "\.env$"
    echo ""
    echo "Use .env.example instead"
    echo ""
    CHECKS_FAILED=1
fi

# Check 3: Large files
LARGE_FILE_THRESHOLD=1048576  # 1MB in bytes

while IFS= read -r file; do
    if [ -f "$file" ]; then
        SIZE=$(stat -f%z "$file" 2>/dev/null || stat -c%s "$file" 2>/dev/null || echo "0")
        if [ "$SIZE" -gt "$LARGE_FILE_THRESHOLD" ]; then
            SIZE_MB=$((SIZE / 1048576))
            echo -e "${YELLOW}⚠️  Large file (${SIZE_MB}MB): $file${NC}"
            echo ""
            read -p "Continue with large file? (y/N) " -n 1 -r
            echo
            if [[ ! $REPLY =~ ^[Yy]$ ]]; then
                CHECKS_FAILED=1
            fi
        fi
    fi
done < <(git diff --cached --name-only --diff-filter=ACM)

# Check 4: Build C# backend (if any .cs files changed)
if git diff --cached --name-only | grep "\.cs$" > /dev/null 2>&1; then
    echo ""
    echo "2. Building C# backend (C# files changed)..."

    cd "$BACKEND_DIR"

    if dotnet build --no-restore > /dev/null 2>&1; then
        echo -e "${GREEN}✅ Build succeeded${NC}"
    else
        echo -e "${RED}❌ Build failed${NC}"
        echo ""
        echo "Fix build errors before committing."
        echo "Run: cd $BACKEND_DIR && dotnet build"
        echo ""
        CHECKS_FAILED=1
    fi
else
    echo "2. Skipping build (no C# files changed)"
fi

# Check 5: Run unit tests (if any .cs files changed)
if git diff --cached --name-only | grep "\.cs$" > /dev/null 2>&1; then
    echo ""
    echo "3. Running unit tests..."

    cd "$BACKEND_DIR"

    if dotnet test --filter "Category=Unit" --no-build --logger "console;verbosity=quiet" > /dev/null 2>&1; then
        echo -e "${GREEN}✅ Unit tests passed${NC}"
    else
        echo -e "${YELLOW}⚠️  Some unit tests failed${NC}"
        echo ""
        read -p "Continue anyway? (y/N) " -n 1 -r
        echo
        if [[ ! $REPLY =~ ^[Yy]$ ]]; then
            CHECKS_FAILED=1
        fi
    fi
else
    echo "3. Skipping tests (no C# files changed)"
fi

# Summary
echo ""
echo "========================================"

if [ $CHECKS_FAILED -eq 1 ]; then
    echo -e "${RED}❌ Pre-commit checks failed${NC}"
    echo "========================================"
    echo ""
    echo "Fix issues and try again."
    echo ""
    echo "To bypass (not recommended):"
    echo "  git commit --no-verify"
    echo ""
    exit 1
else
    echo -e "${GREEN}✅ All pre-commit checks passed${NC}"
    echo "========================================"
    echo ""
    exit 0
fi
