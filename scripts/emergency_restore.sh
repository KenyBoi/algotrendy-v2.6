#!/bin/bash
set -e

echo "⚠️  EMERGENCY DATABASE RESTORE"
echo "================================"
echo ""
echo "This will restore the AlgoTrendy database to the last backup."
echo ""
read -p "Are you absolutely sure? Type 'YES' to continue: " confirm

if [ "$confirm" != "YES" ]; then
    echo "Restore cancelled."
    exit 0
fi

echo ""
echo "🔍 Available backups:"
sudo -u postgres pgbackrest --stanza=algotrendy info

echo ""
read -p "Enter backup ID to restore (or press Enter for latest): " backup_id

# Stop any applications using the database
echo ""
echo "⏸️  Stopping applications (if any)..."
systemctl stop algotrendy 2>/dev/null || echo "   No algotrendy service found (OK)"

# Restore database
echo ""
echo "🔄 Restoring database..."
if [ -z "$backup_id" ]; then
    # Restore latest
    sudo -u postgres pgbackrest --stanza=algotrendy restore
else
    # Restore specific backup
    sudo -u postgres pgbackrest --stanza=algotrendy --set="$backup_id" restore
fi

# Restart PostgreSQL
echo ""
echo "🔄 Restarting PostgreSQL..."
systemctl restart postgresql

# Wait for PostgreSQL
sleep 3

# Verify database
echo ""
echo "✅ Verifying database..."
sudo -u postgres psql -d algotrendy_v25 -c "SELECT 'Database restored successfully' as status;" 2>/dev/null && echo "   ✅ Database is accessible"

# Optionally rollback Liquibase
echo ""
read -p "Rollback Liquibase to v2.5? (y/n): " rollback_liquibase

if [ "$rollback_liquibase" = "y" ]; then
    cd /root/AlgoTrendy_v2.6/database
    /opt/liquibase/liquibase --username=postgres --password=algotrendy_dev_pass_2025 rollback v2.5
    echo "   ✅ Liquibase rolled back to v2.5"
fi

echo ""
echo "✅ RESTORE COMPLETE!"
echo ""
echo "Next steps:"
echo "  1. Verify data integrity"
echo "  2. Check application logs"
echo "  3. Restart application if needed: systemctl start algotrendy"
echo ""
