-- AlgoTrendy v2.6 - QuestDB Schema Creation
-- Time-series optimized schema for market data storage
-- Generated: 2025-10-18

-- ============================================================================
-- Table 1: Market Data (1-minute OHLCV)
-- ============================================================================
CREATE TABLE IF NOT EXISTS market_data_1m (
    symbol SYMBOL CAPACITY 256 CACHE INDEX CAPACITY 128,
    timestamp TIMESTAMP,
    open DOUBLE,
    high DOUBLE,
    low DOUBLE,
    close DOUBLE,
    volume DOUBLE,
    quote_volume DOUBLE,
    trades_count INT,
    source SYMBOL CAPACITY 16 CACHE,
    metadata_json STRING
) TIMESTAMP(timestamp) PARTITION BY DAY WAL;

-- ============================================================================
-- Table 2: Order Book Snapshots
-- ============================================================================
CREATE TABLE IF NOT EXISTS order_book_snapshots (
    symbol SYMBOL CAPACITY 256 CACHE INDEX CAPACITY 128,
    timestamp TIMESTAMP,
    exchange SYMBOL CAPACITY 16 CACHE,
    best_bid DOUBLE,
    best_ask DOUBLE,
    bid_volume DOUBLE,
    ask_volume DOUBLE,
    spread DOUBLE,
    spread_percent DOUBLE,
    depth_levels INT,
    snapshot_data STRING
) TIMESTAMP(timestamp) PARTITION BY DAY WAL;

-- ============================================================================
-- Table 3: Trade Executions
-- ============================================================================
CREATE TABLE IF NOT EXISTS trade_executions (
    trade_id STRING,
    symbol SYMBOL CAPACITY 256 CACHE INDEX CAPACITY 128,
    timestamp TIMESTAMP,
    exchange SYMBOL CAPACITY 16 CACHE,
    side SYMBOL CAPACITY 4,  -- 'buy' or 'sell'
    price DOUBLE,
    quantity DOUBLE,
    quote_quantity DOUBLE,
    fee DOUBLE,
    fee_currency SYMBOL CAPACITY 16,
    order_type SYMBOL CAPACITY 16,
    strategy_id STRING,
    execution_metadata STRING
) TIMESTAMP(timestamp) PARTITION BY DAY WAL;

-- ============================================================================
-- Table 4: Performance Metrics
-- ============================================================================
CREATE TABLE IF NOT EXISTS performance_metrics (
    metric_name SYMBOL CAPACITY 128 CACHE,
    timestamp TIMESTAMP,
    metric_value DOUBLE,
    metric_unit STRING,
    category SYMBOL CAPACITY 32,
    tags STRING,
    metadata STRING
) TIMESTAMP(timestamp) PARTITION BY DAY WAL;

-- ============================================================================
-- Table 5: System Health Metrics
-- ============================================================================
CREATE TABLE IF NOT EXISTS system_health (
    server_name SYMBOL CAPACITY 8 CACHE,  -- chicago-vps-1, chicago-vm-2, cdmx-vps-3
    timestamp TIMESTAMP,
    cpu_percent DOUBLE,
    memory_percent DOUBLE,
    disk_usage_percent DOUBLE,
    network_rx_bytes LONG,
    network_tx_bytes LONG,
    active_connections INT,
    queue_depth INT,
    latency_ms DOUBLE,
    status SYMBOL CAPACITY 16,
    metadata STRING
) TIMESTAMP(timestamp) PARTITION BY HOUR WAL;

-- ============================================================================
-- Indexes for improved query performance
-- ============================================================================
-- Note: QuestDB automatically creates indexes for SYMBOL columns with INDEX keyword
-- Additional indexes are created automatically for timestamp-based partitions

-- ============================================================================
-- Sample Queries for Verification
-- ============================================================================
-- Query 1: Latest market data per symbol
-- SELECT symbol, timestamp, close, volume
-- FROM market_data_1m
-- WHERE timestamp > dateadd('h', -1, now())
-- LATEST ON timestamp PARTITION BY symbol;

-- Query 2: Order book spread analysis
-- SELECT symbol, avg(spread_percent) as avg_spread
-- FROM order_book_snapshots
-- WHERE timestamp > dateadd('d', -1, now())
-- GROUP BY symbol;

-- Query 3: Trading performance by strategy
-- SELECT strategy_id, sum(quote_quantity) as total_volume, count(*) as trade_count
-- FROM trade_executions
-- WHERE timestamp > dateadd('d', -7, now())
-- GROUP BY strategy_id;

-- Query 4: System health overview
-- SELECT server_name, avg(cpu_percent), avg(memory_percent), avg(latency_ms)
-- FROM system_health
-- WHERE timestamp > dateadd('h', -1, now())
-- GROUP BY server_name;
