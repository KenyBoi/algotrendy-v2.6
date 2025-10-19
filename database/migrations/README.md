# AlgoTrendy Database Migrations

This directory contains SQL migration scripts for the AlgoTrendy v2.6 database schema.

## Migration Files

Migrations are numbered sequentially and should be run in order:

| File | Description | Status |
|------|-------------|--------|
| `000_create_orders_table.sql` | Creates the orders table with initial schema | ✅ Ready |
| `001_add_client_order_id.sql` | Adds client_order_id for order idempotency | ✅ Ready |

## Running Migrations

### Option 1: Manual Execution (PostgreSQL)

```bash
# Connect to PostgreSQL
psql -h localhost -U algotrendy -d algotrendy_db

# Run migrations in order
\i /root/AlgoTrendy_v2.6/database/migrations/000_create_orders_table.sql
\i /root/AlgoTrendy_v2.6/database/migrations/001_add_client_order_id.sql
```

### Option 2: Automated Script

```bash
# Make the runner script executable
chmod +x /root/AlgoTrendy_v2.6/database/migrations/run_migrations.sh

# Run all pending migrations
./run_migrations.sh
```

### Option 3: Using psql Command Line

```bash
# Run a specific migration
psql -h localhost -U algotrendy -d algotrendy_db \
  -f /root/AlgoTrendy_v2.6/database/migrations/001_add_client_order_id.sql
```

## Migration Structure

Each migration file follows this structure:

1. **Metadata** - Migration number, date, description, author
2. **Forward Migration (UP)** - SQL to apply the migration
3. **Verification Queries** - Queries to verify the migration succeeded
4. **Rollback Migration (DOWN)** - SQL to undo the migration (commented out)
5. **Notes** - Additional context, dependencies, performance impacts

## Best Practices

### Before Running Migrations

1. **Backup the database**:
   ```bash
   pg_dump -h localhost -U algotrendy algotrendy_db > backup_$(date +%Y%m%d_%H%M%S).sql
   ```

2. **Review the migration SQL** to understand what changes will be made

3. **Test on a staging/dev environment** first

### After Running Migrations

1. **Verify** the migration using the verification queries in the file
2. **Check application logs** for any errors
3. **Monitor performance** for large migrations

### Rolling Back

If a migration causes issues, use the rollback SQL (commented at the bottom of each file):

```sql
-- Uncomment and run the DOWN section from the migration file
-- Example for 001_add_client_order_id.sql:
DROP INDEX IF EXISTS uq_orders_client_order_id;
DROP INDEX IF EXISTS idx_orders_client_order_id;
ALTER TABLE orders DROP COLUMN IF EXISTS client_order_id;
```

## Migration Tracking

Create a `schema_migrations` table to track which migrations have been applied:

```sql
CREATE TABLE IF NOT EXISTS schema_migrations (
    migration_id VARCHAR(100) PRIMARY KEY,
    applied_at TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    description TEXT
);

-- Record a migration
INSERT INTO schema_migrations (migration_id, description)
VALUES ('001_add_client_order_id', 'Adds client_order_id for order idempotency');
```

## Environment Variables

The migrations assume the following connection details:

```bash
POSTGRES_HOST=localhost
POSTGRES_PORT=5432
POSTGRES_DB=algotrendy_db
POSTGRES_USER=algotrendy
POSTGRES_PASSWORD=<your_password>
```

## Dependencies

- PostgreSQL 13+ (for `gen_random_uuid()`)
- Orders table must exist before running `001_add_client_order_id.sql`

## Troubleshooting

### "relation orders does not exist"

Run `000_create_orders_table.sql` first.

### "column client_order_id already exists"

The migration is safe to re-run (uses `IF NOT EXISTS`). This is normal.

### Permission denied

Ensure your PostgreSQL user has `CREATE`, `ALTER`, and `INDEX` privileges:

```sql
GRANT CREATE, ALTER ON DATABASE algotrendy_db TO algotrendy;
GRANT ALL PRIVILEGES ON ALL TABLES IN SCHEMA public TO algotrendy;
```

## Related Code Changes

When a migration is applied, ensure corresponding code changes are deployed:

### For `001_add_client_order_id.sql`:

- ✅ `backend/AlgoTrendy.Core/Models/Order.cs` - Added ClientOrderId property
- ✅ `backend/AlgoTrendy.Core/Models/OrderRequest.cs` - Added ClientOrderId property
- ✅ `backend/AlgoTrendy.Core/Models/OrderFactory.cs` - ID generation logic
- ✅ `backend/AlgoTrendy.Core/Interfaces/IOrderRepository.cs` - Added GetByClientOrderIdAsync
- ✅ `backend/AlgoTrendy.Infrastructure/Repositories/OrderRepository.cs` - Implementation
- ✅ `backend/AlgoTrendy.TradingEngine/TradingEngine.cs` - Idempotency cache

## Support

For issues with migrations, contact the AlgoTrendy development team or create an issue in the repository.
