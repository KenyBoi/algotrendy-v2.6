#!/bin/bash
#
# AlgoTrendy v2.6 - QuestDB Backup Script
# Automated backup of QuestDB database with retention policy
#
# Usage: ./backup_questdb.sh
# Schedule: 0 2 * * * /root/AlgoTrendy_v2.6/scripts/backup_questdb.sh
#

set -e

# Configuration
BACKUP_DIR="/root/AlgoTrendy_v2.6/backups"
TIMESTAMP=$(date +%Y%m%d_%H%M%S)
BACKUP_NAME="questdb_backup_${TIMESTAMP}"
RETENTION_DAYS=7
DOCKER_COMPOSE_FILE="/root/AlgoTrendy_v2.6/docker-compose.prod.yml"
VOLUME_NAME="algotrendy_v26_questdb_data"
LOG_FILE="${BACKUP_DIR}/backup.log"

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
NC='\033[0m' # No Color

# Logging function
log() {
    echo -e "[$(date +'%Y-%m-%d %H:%M:%S')] $1" | tee -a "$LOG_FILE"
}

log_success() {
    echo -e "${GREEN}[✓]${NC} $1" | tee -a "$LOG_FILE"
}

log_error() {
    echo -e "${RED}[✗]${NC} $1" | tee -a "$LOG_FILE"
}

log_warning() {
    echo -e "${YELLOW}[!]${NC} $1" | tee -a "$LOG_FILE"
}

# Create backup directory if it doesn't exist
mkdir -p "$BACKUP_DIR"

log "========================================="
log "Starting QuestDB backup: ${BACKUP_NAME}"
log "========================================="

# Check if Docker is running
if ! docker info > /dev/null 2>&1; then
    log_error "Docker is not running. Exiting."
    exit 1
fi

# Check disk space (need at least 10GB free)
AVAILABLE_SPACE=$(df -BG "$BACKUP_DIR" | tail -1 | awk '{print $4}' | sed 's/G//')
if [ "$AVAILABLE_SPACE" -lt 10 ]; then
    log_warning "Low disk space: ${AVAILABLE_SPACE}GB available"
    log_warning "Continuing backup but consider freeing up space"
fi

# Check if volume exists
if ! docker volume inspect "$VOLUME_NAME" > /dev/null 2>&1; then
    log_error "QuestDB volume '$VOLUME_NAME' not found"
    exit 1
fi

# Get volume mount point
VOLUME_PATH=$(docker volume inspect "$VOLUME_NAME" --format '{{.Mountpoint}}')
log "Volume path: ${VOLUME_PATH}"

# Stop services for consistent backup (optional - comment out for live backup)
# log "Stopping services for consistent backup..."
# cd /root/AlgoTrendy_v2.6
# docker-compose -f "$DOCKER_COMPOSE_FILE" stop
# sleep 5

# Perform backup
log "Creating backup archive..."
cd "$BACKUP_DIR"

if sudo tar -czf "${BACKUP_NAME}.tar.gz" -C "$(dirname $VOLUME_PATH)" "$(basename $VOLUME_PATH)"; then
    BACKUP_SIZE=$(du -h "${BACKUP_NAME}.tar.gz" | cut -f1)
    log_success "Backup created: ${BACKUP_NAME}.tar.gz (${BACKUP_SIZE})"
else
    log_error "Backup failed!"
    exit 1
fi

# Restart services if they were stopped
# log "Restarting services..."
# docker-compose -f "$DOCKER_COMPOSE_FILE" start
# sleep 10

# Verify backup file
if [ -f "${BACKUP_DIR}/${BACKUP_NAME}.tar.gz" ]; then
    log_success "Backup file verified"

    # Calculate checksum
    CHECKSUM=$(sha256sum "${BACKUP_DIR}/${BACKUP_NAME}.tar.gz" | cut -d' ' -f1)
    echo "$CHECKSUM  ${BACKUP_NAME}.tar.gz" > "${BACKUP_DIR}/${BACKUP_NAME}.sha256"
    log_success "Checksum saved: ${CHECKSUM:0:16}..."
else
    log_error "Backup file not found after creation!"
    exit 1
fi

# Clean up old backups (keep last 7 days)
log "Cleaning up backups older than ${RETENTION_DAYS} days..."
DELETED_COUNT=$(find "$BACKUP_DIR" -name "questdb_backup_*.tar.gz" -mtime +${RETENTION_DAYS} -delete -print | wc -l)
if [ "$DELETED_COUNT" -gt 0 ]; then
    log_success "Deleted ${DELETED_COUNT} old backup(s)"
else
    log "No old backups to delete"
fi

# Also delete old checksums
find "$BACKUP_DIR" -name "questdb_backup_*.sha256" -mtime +${RETENTION_DAYS} -delete

# List current backups
log "Current backups:"
ls -lh "$BACKUP_DIR"/questdb_backup_*.tar.gz 2>/dev/null | tail -5 | tee -a "$LOG_FILE"

# Summary
TOTAL_BACKUPS=$(ls -1 "$BACKUP_DIR"/questdb_backup_*.tar.gz 2>/dev/null | wc -l)
TOTAL_SIZE=$(du -sh "$BACKUP_DIR"/questdb_backup_*.tar.gz 2>/dev/null | awk '{sum+=$1} END {print sum}')

log "========================================="
log_success "Backup completed successfully!"
log "Total backups: ${TOTAL_BACKUPS}"
log "Backup location: ${BACKUP_DIR}"
log "Available space: ${AVAILABLE_SPACE}GB"
log "========================================="

# Optional: Send notification (uncomment if you have a notification service)
# curl -X POST https://your-notification-service.com/notify \
#   -d "message=QuestDB backup completed: ${BACKUP_NAME}"

exit 0
