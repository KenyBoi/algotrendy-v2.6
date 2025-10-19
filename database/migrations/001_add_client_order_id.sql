-- Migration: 001_add_client_order_id
-- Date: 2025-10-19
-- Description: Adds client_order_id column to orders table for idempotency support
-- Author: AlgoTrendy Development Team

-- ==============================================================================
-- FORWARD MIGRATION (UP)
-- ==============================================================================

-- Step 1: Add client_order_id column (nullable initially for backward compatibility)
ALTER TABLE orders
ADD COLUMN IF NOT EXISTS client_order_id VARCHAR(100);

-- Step 2: Create index on client_order_id for fast lookups
CREATE INDEX IF NOT EXISTS idx_orders_client_order_id
ON orders (client_order_id);

-- Step 3: Add unique constraint on client_order_id (NULL values are excluded)
-- Note: In PostgreSQL, NULL values don't violate unique constraints
CREATE UNIQUE INDEX IF NOT EXISTS uq_orders_client_order_id
ON orders (client_order_id)
WHERE client_order_id IS NOT NULL;

-- Step 4: Backfill existing rows with generated client_order_ids
-- Format: AT_{timestamp}_{uuid}
UPDATE orders
SET client_order_id = 'AT_' ||
    EXTRACT(EPOCH FROM created_at)::BIGINT || '_' ||
    REPLACE(gen_random_uuid()::TEXT, '-', '')
WHERE client_order_id IS NULL;

-- Step 5: Make column NOT NULL after backfill
ALTER TABLE orders
ALTER COLUMN client_order_id SET NOT NULL;

-- Step 6: Add comment for documentation
COMMENT ON COLUMN orders.client_order_id IS
'Client-generated idempotency key to prevent duplicate orders on network retries. Format: AT_{timestamp}_{guid}';

-- ==============================================================================
-- VERIFICATION QUERIES
-- ==============================================================================

-- Verify column exists
SELECT column_name, data_type, is_nullable, character_maximum_length
FROM information_schema.columns
WHERE table_name = 'orders' AND column_name = 'client_order_id';

-- Verify unique constraint
SELECT indexname, indexdef
FROM pg_indexes
WHERE tablename = 'orders' AND indexname = 'uq_orders_client_order_id';

-- Verify index
SELECT indexname, indexdef
FROM pg_indexes
WHERE tablename = 'orders' AND indexname = 'idx_orders_client_order_id';

-- Count orders without client_order_id (should be 0)
SELECT COUNT(*) as orders_without_client_order_id
FROM orders
WHERE client_order_id IS NULL;

-- Sample client_order_ids to verify format
SELECT order_id, client_order_id, created_at
FROM orders
ORDER BY created_at DESC
LIMIT 5;

-- ==============================================================================
-- ROLLBACK MIGRATION (DOWN)
-- ==============================================================================

-- Uncomment to rollback:
-- DROP INDEX IF EXISTS uq_orders_client_order_id;
-- DROP INDEX IF EXISTS idx_orders_client_order_id;
-- ALTER TABLE orders DROP COLUMN IF EXISTS client_order_id;

-- ==============================================================================
-- NOTES
-- ==============================================================================

-- 1. This migration is safe to run multiple times (uses IF NOT EXISTS)
-- 2. Existing orders are backfilled with auto-generated client_order_ids
-- 3. The unique constraint allows efficient duplicate detection
-- 4. NULL values are excluded from unique constraint for partial uniqueness
-- 5. Index on client_order_id improves query performance for idempotency checks
--
-- Performance Impact:
-- - Minimal impact on existing queries
-- - Fast idempotency lookups via indexed unique constraint
-- - Backfill may take time on large datasets (consider batching for millions of rows)
--
-- Dependencies:
-- - Requires PostgreSQL 13+ for gen_random_uuid()
-- - Requires orders table with created_at column
--
-- Related Code Changes:
-- - backend/AlgoTrendy.Core/Models/Order.cs (added ClientOrderId property)
-- - backend/AlgoTrendy.Core/Models/OrderRequest.cs (added ClientOrderId property)
-- - backend/AlgoTrendy.TradingEngine/TradingEngine.cs (idempotency cache)
-- - backend/AlgoTrendy.Infrastructure/Repositories/OrderRepository.cs (GetByClientOrderIdAsync)
