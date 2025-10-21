-- QuestDB-compatible table creation for AlgoTrendy v2.6
-- Note: QuestDB doesn't support triggers, CHECK constraints, or PL/pgSQL

-- ==============================================================================
-- Orders Table
-- ==============================================================================
CREATE TABLE IF NOT EXISTS orders (
    order_id VARCHAR,
    client_order_id VARCHAR,
    exchange_order_id VARCHAR,
    symbol VARCHAR,
    exchange VARCHAR,
    side VARCHAR,
    type VARCHAR,
    status VARCHAR,
    quantity DOUBLE,
    filled_quantity DOUBLE,
    price DOUBLE,
    stop_price DOUBLE,
    average_fill_price DOUBLE,
    strategy_id VARCHAR,
    created_at TIMESTAMP,
    updated_at TIMESTAMP,
    submitted_at TIMESTAMP,
    closed_at TIMESTAMP,
    metadata STRING
);

-- ==============================================================================
-- Positions Table
-- ==============================================================================
CREATE TABLE IF NOT EXISTS positions (
    position_id VARCHAR,
    symbol VARCHAR,
    exchange VARCHAR,
    side VARCHAR,
    quantity DOUBLE,
    entry_price DOUBLE,
    current_price DOUBLE,
    stop_loss DOUBLE,
    take_profit DOUBLE,
    leverage DOUBLE,
    margin_type VARCHAR,
    collateral_amount DOUBLE,
    borrowed_amount DOUBLE,
    interest_rate DOUBLE,
    liquidation_price DOUBLE,
    margin_health_ratio DOUBLE,
    strategy_id VARCHAR,
    open_order_id VARCHAR,
    opened_at TIMESTAMP,
    updated_at TIMESTAMP
);
