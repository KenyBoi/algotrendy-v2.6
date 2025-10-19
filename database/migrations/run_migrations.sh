#!/bin/bash

# ==============================================================================
# AlgoTrendy Database Migration Runner
# ==============================================================================
#
# This script automatically runs all pending database migrations in order.
#
# Usage:
#   ./run_migrations.sh                    # Run all migrations
#   ./run_migrations.sh --dry-run          # Show what would be executed
#   ./run_migrations.sh --rollback 001     # Rollback a specific migration
#
# Environment Variables:
#   POSTGRES_HOST     - Database host (default: localhost)
#   POSTGRES_PORT     - Database port (default: 5432)
#   POSTGRES_DB       - Database name (default: algotrendy_db)
#   POSTGRES_USER     - Database user (default: algotrendy)
#   POSTGRES_PASSWORD - Database password (required)
#
# ==============================================================================

set -e  # Exit on error

# ==============================================================================
# Configuration
# ==============================================================================

POSTGRES_HOST="${POSTGRES_HOST:-localhost}"
POSTGRES_PORT="${POSTGRES_PORT:-5432}"
POSTGRES_DB="${POSTGRES_DB:-algotrendy_db}"
POSTGRES_USER="${POSTGRES_USER:-algotrendy}"
POSTGRES_PASSWORD="${POSTGRES_PASSWORD}"

MIGRATIONS_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
NC='\033[0m' # No Color

# ==============================================================================
# Helper Functions
# ==============================================================================

log_info() {
    echo -e "${BLUE}ℹ ${NC}$1"
}

log_success() {
    echo -e "${GREEN}✓ ${NC}$1"
}

log_warning() {
    echo -e "${YELLOW}⚠ ${NC}$1"
}

log_error() {
    echo -e "${RED}✗ ${NC}$1"
}

# ==============================================================================
# Database Connection Test
# ==============================================================================

test_connection() {
    log_info "Testing database connection..."

    if [ -z "$POSTGRES_PASSWORD" ]; then
        log_error "POSTGRES_PASSWORD environment variable is not set"
        exit 1
    fi

    PGPASSWORD="$POSTGRES_PASSWORD" psql -h "$POSTGRES_HOST" -p "$POSTGRES_PORT" \
        -U "$POSTGRES_USER" -d "$POSTGRES_DB" -c "SELECT 1;" > /dev/null 2>&1

    if [ $? -eq 0 ]; then
        log_success "Database connection successful"
    else
        log_error "Failed to connect to database"
        log_error "Host: $POSTGRES_HOST:$POSTGRES_PORT, Database: $POSTGRES_DB, User: $POSTGRES_USER"
        exit 1
    fi
}

# ==============================================================================
# Create Migration Tracking Table
# ==============================================================================

create_tracking_table() {
    log_info "Creating schema_migrations table if it doesn't exist..."

    PGPASSWORD="$POSTGRES_PASSWORD" psql -h "$POSTGRES_HOST" -p "$POSTGRES_PORT" \
        -U "$POSTGRES_USER" -d "$POSTGRES_DB" <<EOF
CREATE TABLE IF NOT EXISTS schema_migrations (
    migration_id VARCHAR(100) PRIMARY KEY,
    applied_at TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    description TEXT,
    checksum VARCHAR(64)
);
EOF

    log_success "Migration tracking table ready"
}

# ==============================================================================
# Check if Migration Was Applied
# ==============================================================================

is_migration_applied() {
    local migration_id="$1"

    result=$(PGPASSWORD="$POSTGRES_PASSWORD" psql -h "$POSTGRES_HOST" -p "$POSTGRES_PORT" \
        -U "$POSTGRES_USER" -d "$POSTGRES_DB" -t -c \
        "SELECT COUNT(*) FROM schema_migrations WHERE migration_id = '$migration_id';")

    count=$(echo "$result" | tr -d ' ')
    [ "$count" -gt 0 ]
}

# ==============================================================================
# Record Migration as Applied
# ==============================================================================

record_migration() {
    local migration_id="$1"
    local description="$2"

    PGPASSWORD="$POSTGRES_PASSWORD" psql -h "$POSTGRES_HOST" -p "$POSTGRES_PORT" \
        -U "$POSTGRES_USER" -d "$POSTGRES_DB" -c \
        "INSERT INTO schema_migrations (migration_id, description) VALUES ('$migration_id', '$description');" \
        > /dev/null 2>&1
}

# ==============================================================================
# Run a Single Migration
# ==============================================================================

run_migration() {
    local migration_file="$1"
    local migration_name=$(basename "$migration_file" .sql)

    # Extract description from file (first comment line)
    local description=$(grep "^-- Description:" "$migration_file" | sed 's/-- Description: //')

    if is_migration_applied "$migration_name"; then
        log_warning "Migration $migration_name already applied - skipping"
        return 0
    fi

    log_info "Running migration: $migration_name"
    log_info "Description: $description"

    # Run the migration
    PGPASSWORD="$POSTGRES_PASSWORD" psql -h "$POSTGRES_HOST" -p "$POSTGRES_PORT" \
        -U "$POSTGRES_USER" -d "$POSTGRES_DB" -f "$migration_file"

    if [ $? -eq 0 ]; then
        record_migration "$migration_name" "$description"
        log_success "Migration $migration_name completed successfully"
        return 0
    else
        log_error "Migration $migration_name failed"
        return 1
    fi
}

# ==============================================================================
# Run All Migrations
# ==============================================================================

run_all_migrations() {
    local dry_run="$1"

    log_info "Scanning for migration files in: $MIGRATIONS_DIR"

    # Find all .sql files and sort them
    migration_files=$(find "$MIGRATIONS_DIR" -maxdepth 1 -name "*.sql" -type f | sort)

    if [ -z "$migration_files" ]; then
        log_warning "No migration files found"
        exit 0
    fi

    log_info "Found $(echo "$migration_files" | wc -l) migration file(s)"
    echo ""

    local applied_count=0
    local skipped_count=0
    local failed_count=0

    for migration_file in $migration_files; do
        migration_name=$(basename "$migration_file" .sql)

        if [ "$dry_run" == "true" ]; then
            if is_migration_applied "$migration_name"; then
                echo "  [SKIP] $migration_name (already applied)"
                skipped_count=$((skipped_count + 1))
            else
                echo "  [RUN]  $migration_name"
                applied_count=$((applied_count + 1))
            fi
        else
            if is_migration_applied "$migration_name"; then
                skipped_count=$((skipped_count + 1))
            else
                run_migration "$migration_file"
                if [ $? -eq 0 ]; then
                    applied_count=$((applied_count + 1))
                else
                    failed_count=$((failed_count + 1))
                    log_error "Stopping due to migration failure"
                    break
                fi
            fi
        fi
    done

    echo ""
    log_info "Migration Summary:"
    echo "  Applied: $applied_count"
    echo "  Skipped: $skipped_count"
    echo "  Failed:  $failed_count"

    if [ $failed_count -gt 0 ]; then
        exit 1
    fi
}

# ==============================================================================
# Main Script
# ==============================================================================

main() {
    echo "=========================================="
    echo "  AlgoTrendy Database Migration Runner"
    echo "=========================================="
    echo ""

    # Parse command line arguments
    DRY_RUN=false

    while [[ $# -gt 0 ]]; do
        case $1 in
            --dry-run)
                DRY_RUN=true
                log_info "Dry run mode - no changes will be made"
                shift
                ;;
            --help)
                echo "Usage: $0 [--dry-run] [--help]"
                echo ""
                echo "Options:"
                echo "  --dry-run    Show what would be executed without making changes"
                echo "  --help       Show this help message"
                exit 0
                ;;
            *)
                log_error "Unknown option: $1"
                echo "Use --help for usage information"
                exit 1
                ;;
        esac
    done

    # Run migration process
    test_connection

    if [ "$DRY_RUN" != "true" ]; then
        create_tracking_table
    fi

    run_all_migrations "$DRY_RUN"

    echo ""
    log_success "Migration process completed"
}

# Run main function
main "$@"
