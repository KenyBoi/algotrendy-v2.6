#!/bin/bash
#
# Run All Validations Script
# Runs build, tests, and checks for AlgoTrendy v2.6
#
# Usage: ./run_all_validations.sh
#

set -e

PROJECT_ROOT="/root/AlgoTrendy_v2.6"
BACKEND_DIR="$PROJECT_ROOT/backend"
RESULTS_DIR="$PROJECT_ROOT/devtools/analysis"

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
NC='\033[0m' # No Color

echo "========================================"
echo "AlgoTrendy v2.6 - Validation Suite"
echo "========================================"
echo ""

FAILURES=0

# Validation 1: C# Build
echo "========================================"
echo "1. Building C# Backend..."
echo "========================================"
cd "$BACKEND_DIR"

if dotnet build --no-restore 2>&1 | tee "$RESULTS_DIR/build-output.txt"; then
    echo -e "${GREEN}✅ Build succeeded${NC}"
    echo ""
else
    echo -e "${RED}❌ Build failed${NC}"
    echo "See: $RESULTS_DIR/build-output.txt"
    echo ""
    FAILURES=$((FAILURES+1))
fi

# Validation 2: Count Errors and Warnings
echo "========================================"
echo "2. Checking Build Diagnostics..."
echo "========================================"

ERROR_COUNT=$(grep -c "error CS" "$RESULTS_DIR/build-output.txt" || echo "0")
WARNING_COUNT=$(grep -c "warning CS" "$RESULTS_DIR/build-output.txt" || echo "0")

echo "Errors: $ERROR_COUNT"
echo "Warnings: $WARNING_COUNT"

if [ "$ERROR_COUNT" -eq 0 ]; then
    echo -e "${GREEN}✅ No compilation errors${NC}"
else
    echo -e "${RED}❌ $ERROR_COUNT compilation errors found${NC}"
    FAILURES=$((FAILURES+1))
fi

if [ "$WARNING_COUNT" -lt 20 ]; then
    echo -e "${GREEN}✅ Warnings acceptable ($WARNING_COUNT < 20)${NC}"
else
    echo -e "${YELLOW}⚠️  High warning count: $WARNING_COUNT${NC}"
fi
echo ""

# Validation 3: Unit Tests
echo "========================================"
echo "3. Running Unit Tests..."
echo "========================================"

if dotnet test --filter "Category=Unit" --no-build --logger "console;verbosity=minimal" 2>&1 | tee "$RESULTS_DIR/test-output.txt"; then
    echo -e "${GREEN}✅ Unit tests passed${NC}"
    echo ""
else
    echo -e "${YELLOW}⚠️  Some unit tests failed${NC}"
    echo "See: $RESULTS_DIR/test-output.txt"
    echo ""
    FAILURES=$((FAILURES+1))
fi

# Validation 4: Check for Secrets
echo "========================================"
echo "4. Scanning for Secrets..."
echo "========================================"

cd "$PROJECT_ROOT"
SECRET_PATTERNS=(
    "api_key.*=.*[a-zA-Z0-9]{20,}"
    "secret.*=.*[a-zA-Z0-9]{20,}"
    "password.*=.*[a-zA-Z0-9]{8,}"
    "private_key.*=.*[a-zA-Z0-9]{20,}"
)

SECRETS_FOUND=0
for pattern in "${SECRET_PATTERNS[@]}"; do
    if git diff --cached | grep -iE "$pattern" > /dev/null 2>&1; then
        echo -e "${RED}⚠️  Potential secret found: $pattern${NC}"
        SECRETS_FOUND=1
    fi
done

if [ $SECRETS_FOUND -eq 0 ]; then
    echo -e "${GREEN}✅ No secrets detected in staged changes${NC}"
else
    echo -e "${RED}❌ Potential secrets detected - review before committing${NC}"
    FAILURES=$((FAILURES+1))
fi
echo ""

# Validation 5: Check .env files
echo "========================================"
echo "5. Checking for .env files in Git..."
echo "========================================"

if git diff --cached --name-only | grep "\.env$" > /dev/null 2>&1; then
    echo -e "${RED}❌ .env file staged for commit!${NC}"
    git diff --cached --name-only | grep "\.env$"
    FAILURES=$((FAILURES+1))
else
    echo -e "${GREEN}✅ No .env files staged${NC}"
fi
echo ""

# Validation 6: Check for large files
echo "========================================"
echo "6. Checking for Large Files..."
echo "========================================"

LARGE_FILES=0
while IFS= read -r file; do
    SIZE=$(git cat-file -s ":0:$file" 2>/dev/null || echo "0")
    if [ "$SIZE" -gt 1048576 ]; then  # 1MB
        echo -e "${YELLOW}⚠️  Large file: $file ($(numfmt --to=iec-i --suffix=B $SIZE))${NC}"
        LARGE_FILES=1
    fi
done < <(git diff --cached --name-only)

if [ $LARGE_FILES -eq 0 ]; then
    echo -e "${GREEN}✅ No large files detected${NC}"
else
    echo -e "${YELLOW}⚠️  Large files detected - consider if they should be committed${NC}"
fi
echo ""

# Validation 7: Code Coverage (if available)
echo "========================================"
echo "7. Checking Code Coverage..."
echo "========================================"

if command -v dotnet-coverage &> /dev/null; then
    dotnet test --collect:"XPlat Code Coverage" --no-build --results-directory "$RESULTS_DIR/coverage" > /dev/null 2>&1 || true
    echo -e "${GREEN}✅ Coverage report generated${NC}"
    echo "See: $RESULTS_DIR/coverage/"
else
    echo -e "${YELLOW}⚠️  dotnet-coverage not installed - skipping${NC}"
fi
echo ""

# Summary
echo "========================================"
echo "VALIDATION SUMMARY"
echo "========================================"

if [ $FAILURES -eq 0 ]; then
    echo -e "${GREEN}✅ All validations passed!${NC}"
    echo ""
    echo "Safe to proceed with commit."
    exit 0
else
    echo -e "${RED}❌ $FAILURES validation(s) failed${NC}"
    echo ""
    echo "Please fix issues before committing."
    echo ""
    echo "Logs available in: $RESULTS_DIR/"
    exit 1
fi
