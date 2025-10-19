#!/bin/bash

echo "🔍 AlgoTrendy System Health Check"
echo "=================================="
echo ""

# Color codes
GREEN='\033[0;32m'
RED='\033[0;31m'
YELLOW='\033[1;33m'
NC='\033[0m' # No Color

# PostgreSQL
echo -n "PostgreSQL Service: "
if systemctl is-active --quiet postgresql; then
    echo -e "${GREEN}✅ Running${NC}"
else
    echo -e "${RED}❌ Stopped${NC}"
fi

# Database Connection
echo -n "Database Connection: "
if sudo -u postgres psql -d algotrendy_v25 -c "SELECT 1" &>/dev/null; then
    echo -e "${GREEN}✅ Connected${NC}"
else
    echo -e "${RED}❌ Failed${NC}"
fi

# Latest Backup
echo -n "Latest Backup: "
LATEST_BACKUP=$(sudo -u postgres pgbackrest --stanza=algotrendy info --output=json 2>/dev/null | grep -o '"label":"[^"]*"' | head -1 | cut -d'"' -f4)
if [ -n "$LATEST_BACKUP" ]; then
    BACKUP_TIME=$(sudo -u postgres pgbackrest --stanza=algotrendy info | grep "timestamp start/stop" | head -1 | awk '{print $3}')
    echo -e "${GREEN}✅ $LATEST_BACKUP ($BACKUP_TIME)${NC}"
else
    echo -e "${RED}❌ No backups found${NC}"
fi

# Backup Age
echo -n "Backup Age: "
LAST_BACKUP_DATE=$(sudo -u postgres pgbackrest --stanza=algotrendy info | grep "timestamp start/stop" | head -1 | awk '{print $3}')
if [ -n "$LAST_BACKUP_DATE" ]; then
    BACKUP_AGE=$(( ($(date +%s) - $(date -d "$LAST_BACKUP_DATE" +%s)) / 86400 ))
    if [ $BACKUP_AGE -le 1 ]; then
        echo -e "${GREEN}✅ $BACKUP_AGE days old${NC}"
    elif [ $BACKUP_AGE -le 7 ]; then
        echo -e "${YELLOW}⚠️  $BACKUP_AGE days old (consider running backup)${NC}"
    else
        echo -e "${RED}❌ $BACKUP_AGE days old (CRITICAL - backup now!)${NC}"
    fi
else
    echo -e "${RED}❌ Cannot determine${NC}"
fi

# Database Version Tag
echo -n "Database Version: "
DB_VERSION=$(sudo -u postgres psql -d algotrendy_v25 -t -c "SELECT tag FROM databasechangelog WHERE tag IS NOT NULL ORDER BY dateexecuted DESC LIMIT 1;" 2>/dev/null | xargs)
if [ -n "$DB_VERSION" ]; then
    echo -e "${GREEN}✅ $DB_VERSION${NC}"
else
    echo -e "${YELLOW}⚠️  Not tagged${NC}"
fi

# Liquibase Status
echo -n "Liquibase: "
if [ -f /root/AlgoTrendy_v2.6/database/db.changelog.xml ]; then
    CHANGESETS=$(grep -c '<changeSet' /root/AlgoTrendy_v2.6/database/db.changelog.xml)
    echo -e "${GREEN}✅ $CHANGESETS changesets tracked${NC}"
else
    echo -e "${RED}❌ Changelog missing${NC}"
fi

# Ansible
echo -n "Ansible: "
if ansible all -m ping &>/dev/null; then
    echo -e "${GREEN}✅ Ready${NC}"
else
    echo -e "${RED}❌ Not working${NC}"
fi

# Disk Space
echo -n "Disk Space (/var/lib/pgbackrest): "
DISK_USAGE=$(df -h /var/lib/pgbackrest | tail -1 | awk '{print $5}' | sed 's/%//')
if [ "$DISK_USAGE" -lt 80 ]; then
    echo -e "${GREEN}✅ ${DISK_USAGE}% used${NC}"
elif [ "$DISK_USAGE" -lt 90 ]; then
    echo -e "${YELLOW}⚠️  ${DISK_USAGE}% used (monitor closely)${NC}"
else
    echo -e "${RED}❌ ${DISK_USAGE}% used (CRITICAL)${NC}"
fi

# Cron Jobs
echo -n "Automated Backups: "
if crontab -l 2>/dev/null | grep -q pgbackrest; then
    echo -e "${GREEN}✅ Scheduled${NC}"
else
    echo -e "${YELLOW}⚠️  Not scheduled${NC}"
fi

echo ""
echo "=================================="
echo "Overall Status: "
echo ""

# Count issues
CRITICAL=0
WARNING=0

! systemctl is-active --quiet postgresql && ((CRITICAL++))
! sudo -u postgres psql -d algotrendy_v25 -c "SELECT 1" &>/dev/null && ((CRITICAL++))
[ -z "$LATEST_BACKUP" ] && ((CRITICAL++))
[ "$BACKUP_AGE" -gt 7 ] 2>/dev/null && ((CRITICAL++))
[ "$DISK_USAGE" -gt 90 ] 2>/dev/null && ((CRITICAL++))

[ "$BACKUP_AGE" -gt 1 ] 2>/dev/null && [ "$BACKUP_AGE" -le 7 ] 2>/dev/null && ((WARNING++))
[ "$DISK_USAGE" -gt 80 ] 2>/dev/null && [ "$DISK_USAGE" -le 90 ] 2>/dev/null && ((WARNING++))

if [ $CRITICAL -eq 0 ] && [ $WARNING -eq 0 ]; then
    echo -e "${GREEN}✅ All systems operational${NC}"
elif [ $CRITICAL -eq 0 ]; then
    echo -e "${YELLOW}⚠️  $WARNING warning(s) - review recommended${NC}"
else
    echo -e "${RED}❌ $CRITICAL critical issue(s) - immediate action required${NC}"
fi

echo "=================================="
