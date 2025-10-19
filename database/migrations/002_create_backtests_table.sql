-- Migration: 002_create_backtests_table.sql
-- Description: Create backtests table for storing backtest results and configurations
-- Author: AlgoTrendy Phase 7B
-- Date: October 19, 2025

-- ============================================================================
-- UP MIGRATION
-- ============================================================================

-- Create backtests table
CREATE TABLE IF NOT EXISTS backtests (
    -- Primary key
    backtest_id VARCHAR(50) PRIMARY KEY,

    -- Backtest metadata
    symbol VARCHAR(20) NOT NULL,
    status VARCHAR(20) NOT NULL DEFAULT 'Pending',

    -- Timestamps
    created_at TIMESTAMP NOT NULL DEFAULT NOW(),
    updated_at TIMESTAMP NOT NULL DEFAULT NOW(),
    started_at TIMESTAMP NULL,
    completed_at TIMESTAMP NULL,

    -- Execution info
    execution_time_seconds DECIMAL(10, 2) NULL,

    -- Configuration (stored as JSONB for flexibility)
    config_json JSONB NOT NULL,

    -- Results (stored as JSONB)
    results_json JSONB NULL,

    -- Metrics (extracted for easier querying)
    total_return DECIMAL(10, 2) NULL,
    annual_return DECIMAL(10, 2) NULL,
    sharpe_ratio DECIMAL(10, 4) NULL,
    sortino_ratio DECIMAL(10, 4) NULL,
    max_drawdown DECIMAL(10, 2) NULL,
    win_rate DECIMAL(10, 2) NULL,
    profit_factor DECIMAL(10, 4) NULL,
    total_trades INT NULL,
    winning_trades INT NULL,
    losing_trades INT NULL,

    -- Error handling
    error_message TEXT NULL,
    error_details JSONB NULL,

    -- Additional metadata
    metadata JSONB NULL,

    -- Indicators used (array)
    indicators_used TEXT[] NULL,

    -- Asset class
    asset_class VARCHAR(20) NULL,

    -- Timeframe
    timeframe VARCHAR(10) NULL,

    -- Initial capital
    initial_capital DECIMAL(18, 2) NULL,
    final_value DECIMAL(18, 2) NULL,
    total_pnl DECIMAL(18, 2) NULL
);

-- ============================================================================
-- INDEXES
-- ============================================================================

-- Index on symbol for fast filtering by symbol
CREATE INDEX IF NOT EXISTS idx_backtests_symbol
ON backtests(symbol);

-- Index on created_at for chronological queries
CREATE INDEX IF NOT EXISTS idx_backtests_created_at
ON backtests(created_at DESC);

-- Index on status for filtering by status
CREATE INDEX IF NOT EXISTS idx_backtests_status
ON backtests(status);

-- Compound index on symbol + created_at for common query pattern
CREATE INDEX IF NOT EXISTS idx_backtests_symbol_created_at
ON backtests(symbol, created_at DESC);

-- Index on total_return for sorting by performance
CREATE INDEX IF NOT EXISTS idx_backtests_total_return
ON backtests(total_return DESC)
WHERE total_return IS NOT NULL;

-- Index on sharpe_ratio for sorting by risk-adjusted return
CREATE INDEX IF NOT EXISTS idx_backtests_sharpe_ratio
ON backtests(sharpe_ratio DESC)
WHERE sharpe_ratio IS NOT NULL;

-- GIN index on config_json for JSONB queries
CREATE INDEX IF NOT EXISTS idx_backtests_config_json
ON backtests USING GIN(config_json);

-- GIN index on metadata for JSONB queries
CREATE INDEX IF NOT EXISTS idx_backtests_metadata
ON backtests USING GIN(metadata);

-- ============================================================================
-- CONSTRAINTS
-- ============================================================================

-- Check constraint for status values
ALTER TABLE backtests
ADD CONSTRAINT chk_backtests_status
CHECK (status IN ('Pending', 'Running', 'Completed', 'Failed', 'Cancelled'));

-- Check constraint for valid returns
ALTER TABLE backtests
ADD CONSTRAINT chk_backtests_total_return
CHECK (total_return IS NULL OR total_return >= -100);

-- Check constraint for valid win rate
ALTER TABLE backtests
ADD CONSTRAINT chk_backtests_win_rate
CHECK (win_rate IS NULL OR (win_rate >= 0 AND win_rate <= 100));

-- Check constraint for valid profit factor
ALTER TABLE backtests
ADD CONSTRAINT chk_backtests_profit_factor
CHECK (profit_factor IS NULL OR profit_factor >= 0);

-- Check constraint for completed_at > started_at
ALTER TABLE backtests
ADD CONSTRAINT chk_backtests_completion_time
CHECK (completed_at IS NULL OR started_at IS NULL OR completed_at >= started_at);

-- ============================================================================
-- TRIGGERS
-- ============================================================================

-- Function to auto-update updated_at timestamp
CREATE OR REPLACE FUNCTION update_backtests_updated_at()
RETURNS TRIGGER AS $$
BEGIN
    NEW.updated_at = NOW();
    RETURN NEW;
END;
$$ LANGUAGE plpgsql;

-- Trigger to auto-update updated_at on every update
DROP TRIGGER IF EXISTS trigger_update_backtests_updated_at ON backtests;
CREATE TRIGGER trigger_update_backtests_updated_at
    BEFORE UPDATE ON backtests
    FOR EACH ROW
    EXECUTE FUNCTION update_backtests_updated_at();

-- ============================================================================
-- COMMENTS
-- ============================================================================

COMMENT ON TABLE backtests IS 'Stores backtest configurations and results';
COMMENT ON COLUMN backtests.backtest_id IS 'Unique identifier for backtest (GUID)';
COMMENT ON COLUMN backtests.symbol IS 'Trading symbol (e.g., AAPL, BTC-USD, ES)';
COMMENT ON COLUMN backtests.status IS 'Backtest status: Pending, Running, Completed, Failed, Cancelled';
COMMENT ON COLUMN backtests.config_json IS 'Full backtest configuration as JSON';
COMMENT ON COLUMN backtests.results_json IS 'Complete backtest results including trades and equity curve';
COMMENT ON COLUMN backtests.total_return IS 'Total return percentage';
COMMENT ON COLUMN backtests.sharpe_ratio IS 'Risk-adjusted return (Sharpe Ratio)';
COMMENT ON COLUMN backtests.sortino_ratio IS 'Downside risk-adjusted return (Sortino Ratio)';
COMMENT ON COLUMN backtests.max_drawdown IS 'Maximum drawdown percentage';
COMMENT ON COLUMN backtests.win_rate IS 'Percentage of winning trades';
COMMENT ON COLUMN backtests.profit_factor IS 'Ratio of gross profit to gross loss';

-- ============================================================================
-- ROLLBACK MIGRATION
-- ============================================================================

/*
-- To rollback this migration, run:

DROP TRIGGER IF EXISTS trigger_update_backtests_updated_at ON backtests;
DROP FUNCTION IF EXISTS update_backtests_updated_at();
DROP TABLE IF EXISTS backtests CASCADE;

-- Verify rollback
SELECT tablename FROM pg_tables WHERE schemaname = 'public' AND tablename = 'backtests';
-- Should return no rows
*/

-- ============================================================================
-- VERIFICATION
-- ============================================================================

-- Verify table creation
SELECT
    tablename,
    schemaname
FROM pg_tables
WHERE tablename = 'backtests';

-- Verify indexes
SELECT
    indexname,
    indexdef
FROM pg_indexes
WHERE tablename = 'backtests'
ORDER BY indexname;

-- Verify constraints
SELECT
    conname AS constraint_name,
    contype AS constraint_type
FROM pg_constraint
WHERE conrelid = 'backtests'::regclass
ORDER BY conname;

-- Migration complete
SELECT 'Migration 002_create_backtests_table.sql completed successfully' AS status;
