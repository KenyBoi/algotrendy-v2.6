#!/bin/bash
# AlgoTrendy Pre-Upgrade Backup Script
# Run this BEFORE every version upgrade

set -e

VERSION=$(date +%Y%m%d_%H%M%S)
BACKUP_DIR="/root/AlgoTrendy_v2.6/backups/pre_upgrade_${VERSION}"

echo "🔒 Creating pre-upgrade backup for version ${VERSION}"

# Create backup directory
mkdir -p "${BACKUP_DIR}"

# 1. Backup PostgreSQL database
echo "📦 Backing up PostgreSQL database..."
pg_dump -U algotrendy -d algotrendy_production -F c -f "${BACKUP_DIR}/database.dump"
pg_dump -U algotrendy -d algotrendy_production --schema-only > "${BACKUP_DIR}/schema.sql"
pg_dump -U algotrendy -d algotrendy_production --data-only > "${BACKUP_DIR}/data.sql"

# 2. Backup configuration files
echo "⚙️  Backing up configuration files..."
mkdir -p "${BACKUP_DIR}/configs"
cp -r /root/AlgoTrendy_v2.6/backend/AlgoTrendy.API/appsettings*.json "${BACKUP_DIR}/configs/" 2>/dev/null || true
cp -r /root/AlgoTrendy_v2.6/frontend/.env* "${BACKUP_DIR}/configs/" 2>/dev/null || true

# 3. Backup user secrets
echo "🔐 Backing up user secrets..."
if [ -d ~/.microsoft/usersecrets ]; then
    cp -r ~/.microsoft/usersecrets "${BACKUP_DIR}/secrets/"
fi

# 4. Export user data to JSON (safe format)
echo "👥 Exporting user data..."
psql -U algotrendy -d algotrendy_production -c "\COPY (SELECT row_to_json(u) FROM users u) TO '${BACKUP_DIR}/users.json'"
psql -U algotrendy -d algotrendy_production -c "\COPY (SELECT row_to_json(p) FROM positions p) TO '${BACKUP_DIR}/positions.json'"
psql -U algotrendy -d algotrendy_production -c "\COPY (SELECT row_to_json(o) FROM orders o) TO '${BACKUP_DIR}/orders.json'"

# 5. Create migration snapshot
echo "📸 Creating migration snapshot..."
cd /root/AlgoTrendy_v2.6/backend
dotnet ef migrations script --idempotent --output "${BACKUP_DIR}/current_migrations.sql" || true

# 6. Create restore script
cat > "${BACKUP_DIR}/RESTORE.sh" << 'EOF'
#!/bin/bash
# Restore script - use if upgrade fails

echo "⚠️  WARNING: This will restore the database to pre-upgrade state"
read -p "Are you sure? (yes/no): " confirm

if [ "$confirm" != "yes" ]; then
    echo "Restore cancelled"
    exit 1
fi

# Drop current database
dropdb -U algotrendy algotrendy_production --if-exists

# Create fresh database
createdb -U algotrendy algotrendy_production

# Restore from dump
pg_restore -U algotrendy -d algotrendy_production database.dump

echo "✅ Database restored successfully"
EOF

chmod +x "${BACKUP_DIR}/RESTORE.sh"

# 7. Create backup manifest
cat > "${BACKUP_DIR}/MANIFEST.txt" << EOF
AlgoTrendy Backup Manifest
Created: $(date)
Version: ${VERSION}
Git Commit: $(git rev-parse HEAD 2>/dev/null || echo "N/A")
Git Branch: $(git branch --show-current 2>/dev/null || echo "N/A")

Contents:
- database.dump (PostgreSQL binary backup)
- schema.sql (Database schema)
- data.sql (Database data)
- configs/ (Application configuration files)
- secrets/ (User secrets)
- users.json (User data export)
- positions.json (Trading positions export)
- orders.json (Order history export)
- current_migrations.sql (EF Core migration snapshot)
- RESTORE.sh (Restore script)

To restore:
1. cd to this directory
2. Run: ./RESTORE.sh
EOF

# Calculate sizes
TOTAL_SIZE=$(du -sh "${BACKUP_DIR}" | cut -f1)

echo ""
echo "✅ Backup completed successfully!"
echo "📁 Location: ${BACKUP_DIR}"
echo "💾 Size: ${TOTAL_SIZE}"
echo ""
echo "To restore if upgrade fails:"
echo "  cd ${BACKUP_DIR}"
echo "  ./RESTORE.sh"
echo ""
echo "⚠️  Keep this backup until you verify the upgrade succeeded!"
