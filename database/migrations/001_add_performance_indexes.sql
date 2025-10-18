-- =====================================================
-- Performance Optimization Indexes for AlgoTrendy v2.5
-- =====================================================
-- Created: 2025-10-17
-- Purpose: Add indexes to improve query performance

-- Trades table indexes
CREATE INDEX IF NOT EXISTS idx_trades_symbol_timestamp
    ON trades(symbol, timestamp DESC);

CREATE INDEX IF NOT EXISTS idx_trades_user_timestamp
    ON trades(user_id, timestamp DESC);

CREATE INDEX IF NOT EXISTS idx_trades_strategy_timestamp
    ON trades(strategy_id, timestamp DESC);

-- Positions table indexes
CREATE INDEX IF NOT EXISTS idx_positions_user_active
    ON positions(user_id, is_active)
    WHERE is_active = true;

CREATE INDEX IF NOT EXISTS idx_positions_symbol
    ON positions(symbol);

CREATE INDEX IF NOT EXISTS idx_positions_strategy
    ON positions(strategy_id);

-- Bars/Candles table indexes
CREATE INDEX IF NOT EXISTS idx_bars_symbol_type_time
    ON bars(symbol, bar_type, timestamp DESC);

CREATE INDEX IF NOT EXISTS idx_bars_symbol_time
    ON bars(symbol, timestamp DESC);

-- Market data indexes
CREATE INDEX IF NOT EXISTS idx_market_data_symbol_timestamp
    ON market_data(symbol, timestamp DESC);

CREATE INDEX IF NOT EXISTS idx_market_data_source
    ON market_data(data_source, timestamp DESC);

-- Signals table indexes
CREATE INDEX IF NOT EXISTS idx_signals_symbol_timestamp
    ON signals(symbol, timestamp DESC);

CREATE INDEX IF NOT EXISTS idx_signals_strategy
    ON signals(strategy_id, timestamp DESC);

CREATE INDEX IF NOT EXISTS idx_signals_status
    ON signals(status, timestamp DESC);

-- Strategies table indexes
CREATE INDEX IF NOT EXISTS idx_strategies_enabled
    ON strategies(id)
    WHERE enabled = true;

CREATE INDEX IF NOT EXISTS idx_strategies_user
    ON strategies(user_id, enabled);

-- News/Events table indexes (if exists)
CREATE INDEX IF NOT EXISTS idx_news_symbol_timestamp
    ON news(symbol, published_at DESC);

CREATE INDEX IF NOT EXISTS idx_news_source
    ON news(source, published_at DESC);

-- Portfolio snapshots indexes (for historical tracking)
CREATE INDEX IF NOT EXISTS idx_portfolio_snapshots_user_time
    ON portfolio_snapshots(user_id, timestamp DESC);

-- Create partial indexes for common queries
CREATE INDEX IF NOT EXISTS idx_active_trades
    ON trades(id, timestamp DESC)
    WHERE status = 'active';

CREATE INDEX IF NOT EXISTS idx_open_positions
    ON positions(id, symbol, entry_price)
    WHERE is_active = true;

-- Multi-column statistics for better query planning
CREATE STATISTICS IF NOT EXISTS trades_multi
    ON (symbol, timestamp)
    FROM trades;

CREATE STATISTICS IF NOT EXISTS positions_multi
    ON (user_id, symbol, is_active)
    FROM positions;

-- Add comments for documentation
COMMENT ON INDEX idx_trades_symbol_timestamp IS
    'Optimizes symbol-based trade queries with time filtering';
COMMENT ON INDEX idx_positions_user_active IS
    'Optimizes active position lookups for users';
COMMENT ON INDEX idx_bars_symbol_type_time IS
    'Optimizes bar/candle data retrieval by symbol and type';

-- Vacuum and analyze for statistics update
VACUUM ANALYZE trades;
VACUUM ANALYZE positions;
VACUUM ANALYZE bars;
VACUUM ANALYZE market_data;
VACUUM ANALYZE signals;

-- Print completion message
DO $$
BEGIN
    RAISE NOTICE '✅ Performance indexes created successfully';
    RAISE NOTICE '📊 Run EXPLAIN ANALYZE on your queries to verify index usage';
END $$;
