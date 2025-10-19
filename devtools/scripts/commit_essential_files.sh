#!/bin/bash
#
# Commit Essential Files Script
# Commits untracked files in organized batches
#
# Usage: ./commit_essential_files.sh [--dry-run]
#

set -e

PROJECT_ROOT="/root/AlgoTrendy_v2.6"
DRY_RUN=0

if [ "$1" == "--dry-run" ]; then
    DRY_RUN=1
    echo "DRY RUN MODE - No commits will be made"
    echo ""
fi

cd "$PROJECT_ROOT"

# Colors
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
NC='\033[0m'

echo "========================================"
echo "Committing Essential Files"
echo "========================================"
echo ""

# Function to commit files
commit_files() {
    local files=("$@")
    local message=$1
    shift
    local file_patterns=("$@")

    echo -e "${GREEN}Processing:${NC} $message"

    if [ $DRY_RUN -eq 1 ]; then
        echo "[DRY RUN] Would add:"
        for pattern in "${file_patterns[@]}"; do
            echo "  - $pattern"
        done
        echo ""
        return
    fi

    # Add files
    for pattern in "${file_patterns[@]}"; do
        git add "$pattern" 2>/dev/null || true
    done

    # Check if anything was staged
    if git diff --cached --quiet; then
        echo "No files staged (may already be committed or don't exist)"
        echo ""
        return
    fi

    # Show what will be committed
    echo "Files to commit:"
    git diff --cached --name-only | sed 's/^/  - /'
    echo ""

    # Commit
    git commit -m "$message

🤖 Generated with [Claude Code](https://claude.com/claude-code)

Co-Authored-By: Claude <noreply@anthropic.com>"

    echo -e "${GREEN}✅ Committed${NC}"
    echo ""
}

# Batch 1: Critical Backend Code
echo "========================================"
echo "Batch 1: Critical Backend Code"
echo "========================================"
commit_files \
    "feat: Add margin/leverage management and order idempotency

- Add margin and leverage repositories with TimescaleDB integration
- Implement OrderFactory for idempotent order creation
- Add TradingController with order management endpoints
- Add Azure Key Vault integration for secrets management
- Add database migrations for orders table
- Add comprehensive unit tests" \
    "backend/AlgoTrendy.API/Controllers/TradingController.cs" \
    "backend/AlgoTrendy.Core/Enums/MarginType.cs" \
    "backend/AlgoTrendy.Core/Interfaces/IDebtManagementService.cs" \
    "backend/AlgoTrendy.Core/Interfaces/ILeverageRepository.cs" \
    "backend/AlgoTrendy.Core/Interfaces/IMarginRepository.cs" \
    "backend/AlgoTrendy.Core/Interfaces/ISecretsService.cs" \
    "backend/AlgoTrendy.Core/Models/DebtSummary.cs" \
    "backend/AlgoTrendy.Core/Models/LeverageInfo.cs" \
    "backend/AlgoTrendy.Core/Models/MarginConfiguration.cs" \
    "backend/AlgoTrendy.Core/Models/OrderFactory.cs" \
    "backend/AlgoTrendy.Infrastructure/Repositories/LeverageRepository.cs" \
    "backend/AlgoTrendy.Infrastructure/Repositories/MarginRepository.cs" \
    "backend/AlgoTrendy.Infrastructure/Repositories/OrderRepository.cs" \
    "backend/AlgoTrendy.Infrastructure/Services/AzureKeyVaultSecretsService.cs" \
    "backend/AlgoTrendy.API/Extensions/AzureKeyVaultExtensions.cs" \
    "backend/AlgoTrendy.Core/Configuration/AzureKeyVaultSettings.cs"

# Batch 2: Tests
echo "========================================"
echo "Batch 2: Unit Tests"
echo "========================================"
commit_files \
    "test: Add unit tests for OrderFactory and idempotency

- Add OrderFactoryTests for order creation validation
- Add IdempotencyTests for duplicate order handling
- Ensure order uniqueness constraints" \
    "backend/AlgoTrendy.Tests/Unit/Core/OrderFactoryTests.cs" \
    "backend/AlgoTrendy.Tests/Unit/TradingEngine/IdempotencyTests.cs"

# Batch 3: Database
echo "========================================"
echo "Batch 3: Database Migrations"
echo "========================================"
commit_files \
    "feat: Add database migrations for orders table

- Create orders table with TimescaleDB hypertable
- Add client_order_id uniqueness constraint
- Add migration runner script" \
    "database/migrations/"

# Batch 4: Configuration
echo "========================================"
echo "Batch 4: Configuration Files"
echo "========================================"
commit_files \
    "chore: Add backend configuration files

- Add .env.example template
- Add .gitignore for backend
- Add Azure Key Vault setup documentation" \
    "backend/.env.example" \
    "backend/.gitignore" \
    "backend/AZURE_KEY_VAULT_SETUP.md"

# Batch 5: Documentation
echo "========================================"
echo "Batch 5: Project Documentation"
echo "========================================"
commit_files \
    "docs: Add project documentation and planning materials

- Add comprehensive feature inventory and security status
- Add remediation plans and build strategies
- Add v2.6 evaluation reports
- Add version upgrade tools and documentation" \
    "BUILD_PLAN/" \
    "FEATURES.md" \
    "REMEDIATION_PLAN.md" \
    "SECURITY_STATUS.md" \
    "DEBT_MARGIN_LEVERAGE_INVENTORY.md" \
    "EVALUATION_CORRECTION.md" \
    "FIXES_COMPLETED.md" \
    "RECOMMENDED_TOOLS.md" \
    "algotrendy_v2.6_eval/" \
    "filled/"

# Batch 6: Scripts and Tools
echo "========================================"
echo "Batch 6: Scripts and Tools"
echo "========================================"
commit_files \
    "chore: Add development scripts and tools

- Add backup scripts
- Add version upgrade tooling
- Add Renovate configuration for dependency management" \
    "scripts/" \
    "version_upgrade_tools&doc/" \
    "renovate.json"

# Batch 7: Legacy Reference
echo "========================================"
echo "Batch 7: Legacy Reference (v2.5)"
echo "========================================"
commit_files \
    "chore: Archive AlgoTrendy v2.5 codebase for reference

Preserved complete v2.5 implementation including:
- Authentication system
- Backtesting engine
- Broker integrations
- Data channels (market data + news)
- Frontend (Next.js)
- Indicator calculations
- Trading strategies

Reference for migration to v2.6." \
    "legacy_reference/"

# Summary
echo "========================================"
echo "SUMMARY"
echo "========================================"
echo ""

if [ $DRY_RUN -eq 1 ]; then
    echo -e "${YELLOW}DRY RUN completed - no commits made${NC}"
    echo ""
    echo "Run without --dry-run to actually commit files"
else
    echo -e "${GREEN}All essential files committed!${NC}"
    echo ""
    echo "Review commits:"
    echo "  git log --oneline -10"
    echo ""
    echo "If satisfied, push to remote:"
    echo "  git push origin $(git branch --show-current)"
fi
