#!/bin/bash
# Cleanup Script for Duplicate/Unused Components
# Option A: Delete unused components

set -e  # Exit on error

echo "🧹 AlgoTrendy Frontend Cleanup Script"
echo "======================================"
echo ""
echo "This will DELETE the following files:"
echo ""
echo "  ❌ src/components/Dashboard.tsx (322 lines - ML dashboard, unused)"
echo "  ❌ src/components/Login.tsx (149 lines - Login component, unused)"
echo "  ❌ src/Dockerfile/ (misplaced Docker code)"
echo ""
echo "⚠️  WARNING: This action cannot be undone!"
echo ""
read -p "Continue? (y/N): " -n 1 -r
echo
if [[ ! $REPLY =~ ^[Yy]$ ]]; then
    echo "Cancelled."
    exit 1
fi

echo ""
echo "Starting cleanup..."
echo ""

# Delete unused Dashboard component
if [ -f "src/components/Dashboard.tsx" ]; then
    rm src/components/Dashboard.tsx
    echo "✓ Deleted src/components/Dashboard.tsx"
else
    echo "⚠ src/components/Dashboard.tsx not found"
fi

# Delete unused Login component
if [ -f "src/components/Login.tsx" ]; then
    rm src/components/Login.tsx
    echo "✓ Deleted src/components/Login.tsx"
else
    echo "⚠ src/components/Login.tsx not found"
fi

# Delete misplaced Dockerfile folder
if [ -d "src/Dockerfile" ]; then
    rm -rf src/Dockerfile
    echo "✓ Deleted src/Dockerfile/ directory"
else
    echo "⚠ src/Dockerfile/ not found"
fi

echo ""
echo "✅ Cleanup complete!"
echo ""
echo "Summary:"
echo "  • Removed 2 unused components (~471 lines)"
echo "  • Removed misplaced Dockerfile code"
echo "  • Project structure is now cleaner"
echo ""
echo "Next: The dev server should hot-reload automatically"
echo "      Check http://localhost:3000 to verify"
