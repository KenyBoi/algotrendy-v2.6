#!/bin/bash
# AlgoTrendy v2.6 - QuestDB Installation Script
# Installs and configures QuestDB for time-series data storage

set -e

# Color codes
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
NC='\033[0m'

log() {
    echo -e "${2:-$NC}[$(date +'%Y-%m-%d %H:%M:%S')] $1${NC}"
}

log "Installing QuestDB via Docker" "$BLUE"
log "========================================" "$BLUE"

# Check if QuestDB container already exists
if docker ps -a | grep -q questdb; then
    log "QuestDB container already exists" "$YELLOW"
    read -p "Remove existing container and reinstall? (y/N): " -n 1 -r
    echo
    if [[ $REPLY =~ ^[Yy]$ ]]; then
        log "Stopping and removing existing container..." "$YELLOW"
        docker stop questdb 2>/dev/null || true
        docker rm questdb 2>/dev/null || true
        log "✓ Removed existing container" "$GREEN"
    else
        log "Keeping existing container" "$YELLOW"
        log "Starting existing container..." "$BLUE"
        docker start questdb
        log "✓ QuestDB started" "$GREEN"
        exit 0
    fi
fi

# Create data directory
log "Creating QuestDB data directory..." "$BLUE"
mkdir -p /root/AlgoTrendy_v2.6/questdb-data
log "✓ Data directory created: /root/AlgoTrendy_v2.6/questdb-data" "$GREEN"

# Pull QuestDB image
log "Pulling QuestDB Docker image..." "$BLUE"
docker pull questdb/questdb:latest
log "✓ QuestDB image pulled" "$GREEN"

# Run QuestDB container
log "Starting QuestDB container..." "$BLUE"
docker run -d \
    --name questdb \
    --restart unless-stopped \
    -p 9000:9000 \
    -p 9009:9009 \
    -p 8812:8812 \
    -p 9003:9003 \
    -v /root/AlgoTrendy_v2.6/questdb-data:/var/lib/questdb \
    -e QDB_TELEMETRY_ENABLED=false \
    questdb/questdb:latest

log "✓ QuestDB container started" "$GREEN"

# Wait for QuestDB to be ready
log "Waiting for QuestDB to be ready..." "$YELLOW"
sleep 5

# Check if QuestDB is responding
max_attempts=30
attempt=0
while [ $attempt -lt $max_attempts ]; do
    if curl -s http://localhost:9000 > /dev/null 2>&1; then
        log "✓ QuestDB is ready!" "$GREEN"
        break
    fi
    attempt=$((attempt + 1))
    sleep 1
done

if [ $attempt -eq $max_attempts ]; then
    log "✗ QuestDB failed to start within 30 seconds" "$RED"
    log "Check logs: docker logs questdb" "$RED"
    exit 1
fi

# Display connection information
log "" "$NC"
log "========================================" "$BLUE"
log "QuestDB Installation Complete!" "$GREEN"
log "========================================" "$BLUE"
log "" "$NC"
log "Connection Details:" "$BLUE"
log "  - HTTP Console:   http://localhost:9000" "$GREEN"
log "  - PostgreSQL:     localhost:8812" "$GREEN"
log "  - InfluxDB Line:  localhost:9009" "$GREEN"
log "  - Health Check:   http://localhost:9003" "$GREEN"
log "" "$NC"
log "Data Directory: /root/AlgoTrendy_v2.6/questdb-data" "$BLUE"
log "Container Name: questdb" "$BLUE"
log "" "$NC"
log "Useful Commands:" "$YELLOW"
log "  docker logs questdb       # View logs" "$YELLOW"
log "  docker stop questdb       # Stop QuestDB" "$YELLOW"
log "  docker start questdb      # Start QuestDB" "$YELLOW"
log "  docker restart questdb    # Restart QuestDB" "$YELLOW"
log "========================================" "$BLUE"
