#!/bin/bash
# AlgoTrendy v2.6 - Configuration Migration Script
# Safely copies configuration files from v2.5 to v2.6 with validation

set -e  # Exit on error
set -u  # Exit on undefined variable

# Color codes for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
NC='\033[0m' # No Color

# Directories
V25_DIR="/root/algotrendy_v2.5"
V26_DIR="/root/AlgoTrendy_v2.6"
BACKUP_DIR="${V26_DIR}/backups/configs_$(date +%Y%m%d_%H%M%S)"

# Log file
LOG_FILE="${V26_DIR}/logs/migration_$(date +%Y%m%d_%H%M%S).log"
mkdir -p "$(dirname "$LOG_FILE")"

# Logging function
log() {
    echo -e "${2:-$NC}[$(date +'%Y-%m-%d %H:%M:%S')] $1${NC}" | tee -a "$LOG_FILE"
}

log "Starting configuration migration from v2.5 to v2.6" "$BLUE"

# Create backup directory
mkdir -p "$BACKUP_DIR"
log "Created backup directory: $BACKUP_DIR" "$GREEN"

# Files to migrate (safe, non-secret configs only)
declare -a CONFIG_FILES=(
    "docker-compose.yml"
    "celeryconfig.py"
    "requirements.txt"
    "package.json"
    "tsconfig.json"
    "next.config.js"
    ".prettierrc"
    ".eslintrc.json"
)

# Directories to migrate
declare -a CONFIG_DIRS=(
    "database/schemas"
    "database/migrations"
)

# Files to EXCLUDE (contain secrets)
declare -a EXCLUDED_FILES=(
    ".env"
    ".env.local"
    ".env.production"
    "credentials.json"
    "appsettings.*.json"
)

# Function to check if file should be excluded
is_excluded() {
    local file=$1
    for excluded in "${EXCLUDED_FILES[@]}"; do
        if [[ "$file" == *"$excluded"* ]]; then
            return 0  # True, is excluded
        fi
    done
    return 1  # False, not excluded
}

# Function to migrate a file
migrate_file() {
    local src_file=$1
    local dest_file=$2

    if [[ ! -f "$src_file" ]]; then
        log "SKIP: Source file not found: $src_file" "$YELLOW"
        return
    fi

    if is_excluded "$src_file"; then
        log "SKIP: File contains secrets (excluded): $src_file" "$YELLOW"
        return
    fi

    # Create destination directory if needed
    mkdir -p "$(dirname "$dest_file")"

    # Copy file
    cp "$src_file" "$dest_file"
    log "COPIED: $src_file → $dest_file" "$GREEN"

    # Create backup
    cp "$src_file" "$BACKUP_DIR/$(basename "$src_file")"
}

# Migrate configuration files
log "Migrating configuration files..." "$BLUE"
for config_file in "${CONFIG_FILES[@]}"; do
    src="${V25_DIR}/${config_file}"
    dest="${V26_DIR}/${config_file}"
    migrate_file "$src" "$dest"
done

# Migrate configuration directories
log "Migrating configuration directories..." "$BLUE"
for config_dir in "${CONFIG_DIRS[@]}"; do
    src="${V25_DIR}/${config_dir}"
    dest="${V26_DIR}/${config_dir}"

    if [[ ! -d "$src" ]]; then
        log "SKIP: Source directory not found: $src" "$YELLOW"
        continue
    fi

    mkdir -p "$dest"

    # Copy directory contents, excluding secrets
    rsync -av --exclude='.env*' --exclude='credentials*' --exclude='*.key' \
        "$src/" "$dest/" 2>&1 | tee -a "$LOG_FILE"

    log "COPIED: $src → $dest" "$GREEN"
done

# Validate migrated files
log "Validating migrated configurations..." "$BLUE"

# Check for accidental secret exposure
log "Scanning for exposed secrets..." "$YELLOW"
if grep -r -i "api_key\|api_secret\|password\|token" "$V26_DIR" \
    --exclude-dir=node_modules \
    --exclude-dir=.git \
    --exclude=".env.example" \
    --exclude="migrate_configs.sh" \
    --exclude="*.md" | grep -v "CHANGE_ME\|your_.*_here"; then
    log "WARNING: Potential secrets found in migrated files!" "$RED"
    log "Review the above matches and remove any hardcoded secrets" "$RED"
else
    log "✓ No exposed secrets detected" "$GREEN"
fi

# Summary
log "========================================" "$BLUE"
log "Configuration migration completed!" "$GREEN"
log "Backup location: $BACKUP_DIR" "$GREEN"
log "Log file: $LOG_FILE" "$GREEN"
log "========================================" "$BLUE"
log "" "$NC"
log "NEXT STEPS:" "$YELLOW"
log "1. Review migrated files in $V26_DIR" "$YELLOW"
log "2. Copy .env.example to .env and fill in secrets" "$YELLOW"
log "3. Run validation script: python3 scripts/migration/validate_migration.py" "$YELLOW"
log "4. Initialize Git repository: bash scripts/setup/init_v26_repo.sh" "$YELLOW"
