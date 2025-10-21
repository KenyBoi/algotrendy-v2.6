-- Migration: 003_create_positions_table
-- Date: 2025-10-21
-- Description: Creates the positions table for active trading position management
-- Author: AlgoTrendy Development Team

-- ==============================================================================
-- FORWARD MIGRATION (UP)
-- ==============================================================================

CREATE TABLE IF NOT EXISTS positions (
    -- Primary identifiers
    position_id VARCHAR(50) PRIMARY KEY,
    symbol VARCHAR(20) NOT NULL,
    exchange VARCHAR(20) NOT NULL,

    -- Position details
    side VARCHAR(10) NOT NULL CHECK (side IN ('Buy', 'Sell')),
    quantity DECIMAL(18, 8) NOT NULL,
    entry_price DECIMAL(18, 8) NOT NULL,
    current_price DECIMAL(18, 8) NOT NULL,

    -- Risk management
    stop_loss DECIMAL(18, 8),
    take_profit DECIMAL(18, 8),

    -- Leverage and margin (for margin/futures trading)
    leverage DECIMAL(8, 2) NOT NULL DEFAULT 1.0,
    margin_type VARCHAR(20) CHECK (margin_type IN ('Cross', 'Isolated')),
    collateral_amount DECIMAL(18, 8),
    borrowed_amount DECIMAL(18, 8),
    interest_rate DECIMAL(8, 4),
    liquidation_price DECIMAL(18, 8),
    margin_health_ratio DECIMAL(6, 4),

    -- Strategy tracking
    strategy_id VARCHAR(50),
    open_order_id VARCHAR(50),

    -- Timestamps
    opened_at TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP
);

-- ==============================================================================
-- INDEXES
-- ==============================================================================

-- Index on symbol for quick lookups
CREATE INDEX IF NOT EXISTS idx_positions_symbol
ON positions (symbol);

-- Index on exchange for filtering by exchange
CREATE INDEX IF NOT EXISTS idx_positions_exchange
ON positions (exchange);

-- Composite index for symbol + exchange
CREATE INDEX IF NOT EXISTS idx_positions_symbol_exchange
ON positions (symbol, exchange);

-- Index on strategy_id for strategy-level reporting
CREATE INDEX IF NOT EXISTS idx_positions_strategy_id
ON positions (strategy_id)
WHERE strategy_id IS NOT NULL;

-- Index on opened_at for time-based queries
CREATE INDEX IF NOT EXISTS idx_positions_opened_at
ON positions (opened_at DESC);

-- Index for margin positions
CREATE INDEX IF NOT EXISTS idx_positions_margin
ON positions (leverage, margin_health_ratio)
WHERE leverage > 1.0;

-- ==============================================================================
-- CONSTRAINTS
-- ==============================================================================

-- Add check constraint for positive quantities
ALTER TABLE positions
ADD CONSTRAINT chk_positions_positive_quantity
CHECK (quantity > 0);

-- Add check constraint for positive prices
ALTER TABLE positions
ADD CONSTRAINT chk_positions_positive_entry_price
CHECK (entry_price > 0);

-- Add check constraint for positive current price
ALTER TABLE positions
ADD CONSTRAINT chk_positions_positive_current_price
CHECK (current_price > 0);

-- Add check constraint for leverage >= 1.0
ALTER TABLE positions
ADD CONSTRAINT chk_positions_minimum_leverage
CHECK (leverage >= 1.0);

-- Add check constraint for margin health ratio between 0 and 1
ALTER TABLE positions
ADD CONSTRAINT chk_positions_margin_health_ratio
CHECK (margin_health_ratio IS NULL OR (margin_health_ratio >= 0 AND margin_health_ratio <= 1));

-- ==============================================================================
-- TRIGGERS
-- ==============================================================================

-- Trigger to auto-update updated_at timestamp
CREATE OR REPLACE FUNCTION update_positions_updated_at()
RETURNS TRIGGER AS $$
BEGIN
    NEW.updated_at = CURRENT_TIMESTAMP;
    RETURN NEW;
END;
$$ LANGUAGE plpgsql;

CREATE TRIGGER trg_positions_updated_at
BEFORE UPDATE ON positions
FOR EACH ROW
EXECUTE FUNCTION update_positions_updated_at();

-- ==============================================================================
-- COMMENTS
-- ==============================================================================

COMMENT ON TABLE positions IS 'Stores all active trading positions with P&L tracking';
COMMENT ON COLUMN positions.position_id IS 'Unique position ID (UUID)';
COMMENT ON COLUMN positions.symbol IS 'Trading symbol (e.g., BTCUSDT)';
COMMENT ON COLUMN positions.exchange IS 'Exchange where position is held (binance, okx, etc.)';
COMMENT ON COLUMN positions.side IS 'Position side: Buy (Long) or Sell (Short)';
COMMENT ON COLUMN positions.quantity IS 'Position size in base currency';
COMMENT ON COLUMN positions.entry_price IS 'Average entry price';
COMMENT ON COLUMN positions.current_price IS 'Current market price (updated in real-time)';
COMMENT ON COLUMN positions.stop_loss IS 'Stop loss price (optional)';
COMMENT ON COLUMN positions.take_profit IS 'Take profit price (optional)';
COMMENT ON COLUMN positions.leverage IS 'Leverage multiplier (1.0 for spot, >1 for margin/futures)';
COMMENT ON COLUMN positions.margin_type IS 'Margin type: Cross or Isolated (for margin positions)';
COMMENT ON COLUMN positions.collateral_amount IS 'Collateral amount (for margin positions)';
COMMENT ON COLUMN positions.borrowed_amount IS 'Borrowed amount (for margin positions)';
COMMENT ON COLUMN positions.interest_rate IS 'Interest rate on borrowed funds (daily percentage)';
COMMENT ON COLUMN positions.liquidation_price IS 'Price at which position will be liquidated';
COMMENT ON COLUMN positions.margin_health_ratio IS 'Margin health ratio (0.0 to 1.0, lower is riskier)';
COMMENT ON COLUMN positions.strategy_id IS 'Strategy that opened this position';
COMMENT ON COLUMN positions.open_order_id IS 'Order ID that opened this position';
COMMENT ON COLUMN positions.opened_at IS 'Timestamp when position was opened (UTC)';
COMMENT ON COLUMN positions.updated_at IS 'Timestamp when position was last updated (UTC)';

-- ==============================================================================
-- VERIFICATION QUERIES
-- ==============================================================================

-- Verify table structure
SELECT column_name, data_type, is_nullable, column_default
FROM information_schema.columns
WHERE table_name = 'positions'
ORDER BY ordinal_position;

-- Verify indexes
SELECT indexname, indexdef
FROM pg_indexes
WHERE tablename = 'positions'
ORDER BY indexname;

-- Verify triggers
SELECT trigger_name, event_manipulation, action_statement
FROM information_schema.triggers
WHERE event_object_table = 'positions';

-- ==============================================================================
-- ROLLBACK MIGRATION (DOWN)
-- ==============================================================================

-- Uncomment to rollback:
-- DROP TRIGGER IF EXISTS trg_positions_updated_at ON positions;
-- DROP FUNCTION IF EXISTS update_positions_updated_at();
-- DROP TABLE IF EXISTS positions CASCADE;

-- ==============================================================================
-- SAMPLE DATA (Optional - for testing)
-- ==============================================================================

-- Uncomment to insert sample data:
-- INSERT INTO positions (
--     position_id, symbol, exchange, side, quantity, entry_price, current_price,
--     stop_loss, take_profit, leverage, strategy_id
-- ) VALUES (
--     gen_random_uuid()::TEXT,
--     'BTCUSDT',
--     'binance',
--     'Buy',
--     0.1,
--     45000.00,
--     46000.00,
--     44000.00,
--     48000.00,
--     1.0,
--     'strategy_001'
-- );
