#!/bin/bash
#
# Verify Dependency Usage Script
# Checks if a package is actually used in the frontend codebase
#
# Usage: ./verify_dependency_usage.sh <package-name>
# Example: ./verify_dependency_usage.sh "react-hook-form"
#

set -e

if [ -z "$1" ]; then
    echo "Usage: $0 <package-name>"
    echo "Example: $0 react-hook-form"
    exit 1
fi

PACKAGE=$1
FRONTEND_DIR="/root/AlgoTrendy_v2.6/legacy_reference/v2.5_frontend"

if [ ! -d "$FRONTEND_DIR" ]; then
    echo "❌ Frontend directory not found: $FRONTEND_DIR"
    exit 1
fi

echo "========================================"
echo "Checking usage of package: $PACKAGE"
echo "========================================"
echo ""

FOUND=0

# Check static imports (ES6)
echo "=== Static Imports (ES6) ==="
if grep -r "from ['\"]$PACKAGE['\"]" "$FRONTEND_DIR/src" 2>/dev/null; then
    FOUND=1
else
    echo "None found"
fi
echo ""

# Check static imports with path
echo "=== Static Imports (with subpath) ==="
if grep -r "from ['\"]$PACKAGE/" "$FRONTEND_DIR/src" 2>/dev/null; then
    FOUND=1
else
    echo "None found"
fi
echo ""

# Check dynamic imports
echo "=== Dynamic Imports ==="
if grep -r "import(['\"]$PACKAGE" "$FRONTEND_DIR/src" 2>/dev/null; then
    FOUND=1
else
    echo "None found"
fi
echo ""

# Check require statements
echo "=== Require Statements ==="
if grep -r "require(['\"]$PACKAGE['\"])" "$FRONTEND_DIR" 2>/dev/null; then
    FOUND=1
else
    echo "None found"
fi
echo ""

# Check in config files
echo "=== Configuration Files ==="
if grep -r "$PACKAGE" "$FRONTEND_DIR"/*.config.* "$FRONTEND_DIR"/*.json 2>/dev/null | grep -v "package.json" | grep -v "package-lock.json"; then
    FOUND=1
else
    echo "None found"
fi
echo ""

# Check in Next.js config specifically
echo "=== Next.js Config ==="
if [ -f "$FRONTEND_DIR/next.config.js" ]; then
    if grep "$PACKAGE" "$FRONTEND_DIR/next.config.js" 2>/dev/null; then
        FOUND=1
    else
        echo "None found"
    fi
else
    echo "next.config.js not found"
fi
echo ""

# Check package.json for the dependency
echo "=== Package.json Entry ==="
if grep "\"$PACKAGE\"" "$FRONTEND_DIR/package.json" 2>/dev/null; then
    echo "(Listed in package.json)"
else
    echo "Not in package.json"
fi
echo ""

# Summary
echo "========================================"
if [ $FOUND -eq 1 ]; then
    echo "✅ RESULT: Package '$PACKAGE' IS used in the code"
    echo ""
    echo "⚠️  DO NOT REMOVE - Package is actively used"
else
    echo "❌ RESULT: Package '$PACKAGE' appears UNUSED"
    echo ""
    echo "⚠️  VERIFY MANUALLY before removing:"
    echo "   1. Check for dynamic imports/requires"
    echo "   2. Check if peer dependency of another package"
    echo "   3. Test build after removal"
    echo ""
    echo "To remove (if truly unused):"
    echo "   cd $FRONTEND_DIR"
    echo "   npm uninstall $PACKAGE"
    echo "   npm run build  # Verify build succeeds"
fi
echo "========================================"

exit $FOUND
