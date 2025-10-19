-- Migration: 000_create_orders_table
-- Date: 2025-10-19
-- Description: Creates the orders table for trade order management
-- Author: AlgoTrendy Development Team

-- ==============================================================================
-- FORWARD MIGRATION (UP)
-- ==============================================================================

CREATE TABLE IF NOT EXISTS orders (
    -- Primary identifiers
    order_id VARCHAR(50) PRIMARY KEY,
    exchange_order_id VARCHAR(100),

    -- Trading details
    symbol VARCHAR(20) NOT NULL,
    exchange VARCHAR(20) NOT NULL,
    side VARCHAR(10) NOT NULL CHECK (side IN ('Buy', 'Sell')),
    type VARCHAR(20) NOT NULL CHECK (type IN ('Market', 'Limit', 'StopLoss', 'StopLimit', 'TakeProfit')),
    status VARCHAR(20) NOT NULL CHECK (status IN ('Pending', 'Open', 'PartiallyFilled', 'Filled', 'Cancelled', 'Rejected', 'Expired')),

    -- Quantity and pricing
    quantity DECIMAL(18, 8) NOT NULL,
    filled_quantity DECIMAL(18, 8) NOT NULL DEFAULT 0,
    price DECIMAL(18, 8),
    stop_price DECIMAL(18, 8),
    average_fill_price DECIMAL(18, 8),

    -- Strategy tracking
    strategy_id VARCHAR(50),

    -- Timestamps
    created_at TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    submitted_at TIMESTAMP,
    closed_at TIMESTAMP,

    -- Metadata
    metadata JSONB
);

-- ==============================================================================
-- INDEXES
-- ==============================================================================

-- Index on exchange_order_id for quick lookups
CREATE INDEX IF NOT EXISTS idx_orders_exchange_order_id
ON orders (exchange_order_id);

-- Index on symbol for filtering by trading pair
CREATE INDEX IF NOT EXISTS idx_orders_symbol
ON orders (symbol);

-- Index on status for filtering active/completed orders
CREATE INDEX IF NOT EXISTS idx_orders_status
ON orders (status);

-- Composite index for active orders by symbol
CREATE INDEX IF NOT EXISTS idx_orders_active_symbol
ON orders (symbol, status)
WHERE status IN ('Open', 'PartiallyFilled');

-- Index on strategy_id for strategy-level reporting
CREATE INDEX IF NOT EXISTS idx_orders_strategy_id
ON orders (strategy_id)
WHERE strategy_id IS NOT NULL;

-- Index on created_at for time-based queries
CREATE INDEX IF NOT EXISTS idx_orders_created_at
ON orders (created_at DESC);

-- Composite index for time range queries by symbol
CREATE INDEX IF NOT EXISTS idx_orders_symbol_time
ON orders (symbol, created_at DESC);

-- ==============================================================================
-- CONSTRAINTS
-- ==============================================================================

-- Add check constraint for filled_quantity <= quantity
ALTER TABLE orders
ADD CONSTRAINT chk_orders_filled_quantity
CHECK (filled_quantity <= quantity);

-- Add check constraint for positive quantities
ALTER TABLE orders
ADD CONSTRAINT chk_orders_positive_quantity
CHECK (quantity > 0);

-- ==============================================================================
-- TRIGGERS
-- ==============================================================================

-- Trigger to auto-update updated_at timestamp
CREATE OR REPLACE FUNCTION update_orders_updated_at()
RETURNS TRIGGER AS $$
BEGIN
    NEW.updated_at = CURRENT_TIMESTAMP;
    RETURN NEW;
END;
$$ LANGUAGE plpgsql;

CREATE TRIGGER trg_orders_updated_at
BEFORE UPDATE ON orders
FOR EACH ROW
EXECUTE FUNCTION update_orders_updated_at();

-- ==============================================================================
-- COMMENTS
-- ==============================================================================

COMMENT ON TABLE orders IS 'Stores all trading orders with full lifecycle tracking';
COMMENT ON COLUMN orders.order_id IS 'Internal unique order ID (UUID)';
COMMENT ON COLUMN orders.exchange_order_id IS 'Exchange-provided order ID after submission';
COMMENT ON COLUMN orders.symbol IS 'Trading symbol (e.g., BTCUSDT)';
COMMENT ON COLUMN orders.exchange IS 'Exchange where order is placed (binance, okx, etc.)';
COMMENT ON COLUMN orders.side IS 'Order side: Buy or Sell';
COMMENT ON COLUMN orders.type IS 'Order type: Market, Limit, StopLoss, StopLimit, TakeProfit';
COMMENT ON COLUMN orders.status IS 'Order status: Pending, Open, PartiallyFilled, Filled, Cancelled, Rejected, Expired';
COMMENT ON COLUMN orders.quantity IS 'Order quantity in base currency';
COMMENT ON COLUMN orders.filled_quantity IS 'Quantity filled so far';
COMMENT ON COLUMN orders.price IS 'Limit price (NULL for market orders)';
COMMENT ON COLUMN orders.stop_price IS 'Stop price for stop orders';
COMMENT ON COLUMN orders.average_fill_price IS 'Average price at which order was filled';
COMMENT ON COLUMN orders.strategy_id IS 'Strategy that generated this order';
COMMENT ON COLUMN orders.created_at IS 'Timestamp when order was created (UTC)';
COMMENT ON COLUMN orders.updated_at IS 'Timestamp when order was last updated (UTC)';
COMMENT ON COLUMN orders.submitted_at IS 'Timestamp when order was submitted to exchange (UTC)';
COMMENT ON COLUMN orders.closed_at IS 'Timestamp when order was filled/cancelled/expired (UTC)';
COMMENT ON COLUMN orders.metadata IS 'Additional metadata in JSON format';

-- ==============================================================================
-- VERIFICATION QUERIES
-- ==============================================================================

-- Verify table structure
SELECT column_name, data_type, is_nullable, column_default
FROM information_schema.columns
WHERE table_name = 'orders'
ORDER BY ordinal_position;

-- Verify indexes
SELECT indexname, indexdef
FROM pg_indexes
WHERE tablename = 'orders'
ORDER BY indexname;

-- Verify triggers
SELECT trigger_name, event_manipulation, action_statement
FROM information_schema.triggers
WHERE event_object_table = 'orders';

-- ==============================================================================
-- ROLLBACK MIGRATION (DOWN)
-- ==============================================================================

-- Uncomment to rollback:
-- DROP TRIGGER IF EXISTS trg_orders_updated_at ON orders;
-- DROP FUNCTION IF EXISTS update_orders_updated_at();
-- DROP TABLE IF EXISTS orders CASCADE;

-- ==============================================================================
-- SAMPLE DATA (Optional - for testing)
-- ==============================================================================

-- Uncomment to insert sample data:
-- INSERT INTO orders (
--     order_id, symbol, exchange, side, type, status, quantity, price, strategy_id
-- ) VALUES (
--     gen_random_uuid()::TEXT,
--     'BTCUSDT',
--     'binance',
--     'Buy',
--     'Limit',
--     'Open',
--     0.01,
--     50000.00,
--     'strategy_001'
-- );
