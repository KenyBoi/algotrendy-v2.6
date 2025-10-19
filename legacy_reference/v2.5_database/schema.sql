-- AlgoTrendy v2.5 Database Schema
-- PostgreSQL 16 + TimescaleDB 2.22.1

-- ===========================
-- Data Sources Registry
-- ===========================
CREATE TABLE data_sources (
    source_id SERIAL PRIMARY KEY,
    source_name VARCHAR(100) UNIQUE NOT NULL,
    source_type VARCHAR(50) NOT NULL, -- 'market_data', 'news', 'sentiment', 'onchain', 'blockchain', 'alt_data'
    source_category VARCHAR(50), -- 'exchange', 'news_api', 'social', 'blockchain_explorer', etc.
    is_active BOOLEAN DEFAULT true,
    config_json JSONB, -- API keys, endpoints, rate limits, etc.
    last_successful_fetch TIMESTAMPTZ,
    created_at TIMESTAMPTZ DEFAULT NOW(),
    updated_at TIMESTAMPTZ DEFAULT NOW()
);

CREATE INDEX idx_data_sources_type ON data_sources(source_type);
CREATE INDEX idx_data_sources_active ON data_sources(is_active);

-- ===========================
-- Market Data (TimescaleDB Hypertable)
-- ===========================
CREATE TABLE market_data (
    timestamp TIMESTAMPTZ NOT NULL,
    symbol VARCHAR(20) NOT NULL,
    source_id INTEGER REFERENCES data_sources(source_id),
    open NUMERIC(20, 8),
    high NUMERIC(20, 8),
    low NUMERIC(20, 8),
    close NUMERIC(20, 8),
    volume NUMERIC(20, 8),
    quote_volume NUMERIC(20, 8),
    trades_count INTEGER,
    vwap NUMERIC(20, 8), -- Volume Weighted Average Price
    metadata_json JSONB, -- Additional exchange-specific data
    PRIMARY KEY (timestamp, symbol, source_id)
);

-- Convert to hypertable (partitioned by time)
SELECT create_hypertable('market_data', 'timestamp',
    chunk_time_interval => INTERVAL '1 day',
    if_not_exists => TRUE
);

-- Compression policy (compress data older than 7 days)
ALTER TABLE market_data SET (
    timescaledb.compress,
    timescaledb.compress_segmentby = 'symbol,source_id',
    timescaledb.compress_orderby = 'timestamp DESC'
);

SELECT add_compression_policy('market_data', INTERVAL '7 days');

-- Retention policy (keep 2 years of data)
SELECT add_retention_policy('market_data', INTERVAL '2 years');

CREATE INDEX idx_market_data_symbol ON market_data(symbol, timestamp DESC);
CREATE INDEX idx_market_data_source ON market_data(source_id, timestamp DESC);

-- ===========================
-- OHLCV Aggregates (Materialized Views)
-- ===========================
-- 1-minute aggregates
CREATE MATERIALIZED VIEW market_data_1m
WITH (timescaledb.continuous) AS
SELECT
    time_bucket('1 minute', timestamp) AS bucket,
    symbol,
    source_id,
    first(open, timestamp) AS open,
    max(high) AS high,
    min(low) AS low,
    last(close, timestamp) AS close,
    sum(volume) AS volume,
    sum(quote_volume) AS quote_volume,
    sum(trades_count) AS trades_count
FROM market_data
GROUP BY bucket, symbol, source_id
WITH NO DATA;

SELECT add_continuous_aggregate_policy('market_data_1m',
    start_offset => INTERVAL '1 hour',
    end_offset => INTERVAL '1 minute',
    schedule_interval => INTERVAL '1 minute'
);

-- 5-minute aggregates
CREATE MATERIALIZED VIEW market_data_5m
WITH (timescaledb.continuous) AS
SELECT
    time_bucket('5 minutes', timestamp) AS bucket,
    symbol,
    source_id,
    first(open, timestamp) AS open,
    max(high) AS high,
    min(low) AS low,
    last(close, timestamp) AS close,
    sum(volume) AS volume,
    sum(quote_volume) AS quote_volume,
    sum(trades_count) AS trades_count
FROM market_data
GROUP BY bucket, symbol, source_id
WITH NO DATA;

SELECT add_continuous_aggregate_policy('market_data_5m',
    start_offset => INTERVAL '1 day',
    end_offset => INTERVAL '5 minutes',
    schedule_interval => INTERVAL '5 minutes'
);

-- 1-hour aggregates
CREATE MATERIALIZED VIEW market_data_1h
WITH (timescaledb.continuous) AS
SELECT
    time_bucket('1 hour', timestamp) AS bucket,
    symbol,
    source_id,
    first(open, timestamp) AS open,
    max(high) AS high,
    min(low) AS low,
    last(close, timestamp) AS close,
    sum(volume) AS volume,
    sum(quote_volume) AS quote_volume,
    sum(trades_count) AS trades_count
FROM market_data
GROUP BY bucket, symbol, source_id
WITH NO DATA;

SELECT add_continuous_aggregate_policy('market_data_1h',
    start_offset => INTERVAL '7 days',
    end_offset => INTERVAL '1 hour',
    schedule_interval => INTERVAL '1 hour'
);

-- ===========================
-- News Articles (TimescaleDB Hypertable)
-- ===========================
CREATE TABLE news_articles (
    article_id BIGSERIAL,
    timestamp TIMESTAMPTZ NOT NULL,
    source_id INTEGER REFERENCES data_sources(source_id),
    article_url VARCHAR(500) UNIQUE,
    title TEXT NOT NULL,
    summary TEXT,
    content TEXT,
    author VARCHAR(200),
    symbols TEXT[], -- Array of related symbols
    categories TEXT[], -- ['crypto', 'bitcoin', 'regulation', etc.]
    sentiment_score NUMERIC(3, 2), -- -1.0 to 1.0
    sentiment_label VARCHAR(20), -- 'positive', 'negative', 'neutral'
    image_url VARCHAR(500),
    metadata_json JSONB,
    PRIMARY KEY (timestamp, article_id)
);

SELECT create_hypertable('news_articles', 'timestamp',
    chunk_time_interval => INTERVAL '7 days',
    if_not_exists => TRUE
);

-- Compression policy
ALTER TABLE news_articles SET (
    timescaledb.compress,
    timescaledb.compress_segmentby = 'source_id',
    timescaledb.compress_orderby = 'timestamp DESC'
);

SELECT add_compression_policy('news_articles', INTERVAL '30 days');

-- Retention policy (keep 1 year)
SELECT add_retention_policy('news_articles', INTERVAL '1 year');

CREATE INDEX idx_news_timestamp ON news_articles(timestamp DESC);
CREATE INDEX idx_news_source ON news_articles(source_id);
CREATE INDEX idx_news_symbols ON news_articles USING GIN(symbols);
CREATE INDEX idx_news_categories ON news_articles USING GIN(categories);
CREATE INDEX idx_news_sentiment ON news_articles(sentiment_score);

-- ===========================
-- Social Sentiment (TimescaleDB Hypertable)
-- ===========================
CREATE TABLE social_sentiment (
    timestamp TIMESTAMPTZ NOT NULL,
    source_id INTEGER REFERENCES data_sources(source_id),
    platform VARCHAR(50), -- 'reddit', 'twitter', 'telegram', etc.
    symbol VARCHAR(20),
    post_id VARCHAR(200),
    post_url VARCHAR(500),
    author VARCHAR(200),
    content TEXT,
    sentiment_score NUMERIC(3, 2),
    sentiment_label VARCHAR(20),
    engagement_score INTEGER, -- likes, upvotes, retweets, etc.
    metadata_json JSONB,
    PRIMARY KEY (timestamp, post_id)
);

SELECT create_hypertable('social_sentiment', 'timestamp',
    chunk_time_interval => INTERVAL '7 days',
    if_not_exists => TRUE
);

ALTER TABLE social_sentiment SET (
    timescaledb.compress,
    timescaledb.compress_segmentby = 'platform,symbol',
    timescaledb.compress_orderby = 'timestamp DESC'
);

SELECT add_compression_policy('social_sentiment', INTERVAL '30 days');
SELECT add_retention_policy('social_sentiment', INTERVAL '6 months');

CREATE INDEX idx_social_symbol ON social_sentiment(symbol, timestamp DESC);
CREATE INDEX idx_social_platform ON social_sentiment(platform);

-- ===========================
-- On-Chain Data (TimescaleDB Hypertable)
-- ===========================
CREATE TABLE onchain_data (
    timestamp TIMESTAMPTZ NOT NULL,
    source_id INTEGER REFERENCES data_sources(source_id),
    blockchain VARCHAR(50), -- 'bitcoin', 'ethereum', 'solana', etc.
    metric_name VARCHAR(100), -- 'active_addresses', 'txn_count', 'hash_rate', etc.
    metric_value NUMERIC(30, 8),
    symbol VARCHAR(20),
    metadata_json JSONB,
    PRIMARY KEY (timestamp, blockchain, metric_name)
);

SELECT create_hypertable('onchain_data', 'timestamp',
    chunk_time_interval => INTERVAL '1 day',
    if_not_exists => TRUE
);

ALTER TABLE onchain_data SET (
    timescaledb.compress,
    timescaledb.compress_segmentby = 'blockchain,metric_name',
    timescaledb.compress_orderby = 'timestamp DESC'
);

SELECT add_compression_policy('onchain_data', INTERVAL '30 days');

CREATE INDEX idx_onchain_blockchain ON onchain_data(blockchain, metric_name, timestamp DESC);

-- ===========================
-- Alternative Data (TimescaleDB Hypertable)
-- ===========================
CREATE TABLE alt_data (
    timestamp TIMESTAMPTZ NOT NULL,
    source_id INTEGER REFERENCES data_sources(source_id),
    data_type VARCHAR(50), -- 'google_trends', 'whale_alert', 'fear_greed_index', etc.
    symbol VARCHAR(20),
    metric_name VARCHAR(100),
    metric_value NUMERIC(20, 8),
    metadata_json JSONB,
    PRIMARY KEY (timestamp, data_type, metric_name)
);

SELECT create_hypertable('alt_data', 'timestamp',
    chunk_time_interval => INTERVAL '1 day',
    if_not_exists => TRUE
);

ALTER TABLE alt_data SET (
    timescaledb.compress,
    timescaledb.compress_segmentby = 'data_type,symbol',
    timescaledb.compress_orderby = 'timestamp DESC'
);

SELECT add_compression_policy('alt_data', INTERVAL '30 days');

CREATE INDEX idx_alt_data_type ON alt_data(data_type, timestamp DESC);

-- ===========================
-- Trading Signals
-- ===========================
CREATE TABLE signals (
    signal_id BIGSERIAL PRIMARY KEY,
    timestamp TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    symbol VARCHAR(20) NOT NULL,
    signal_type VARCHAR(20) NOT NULL, -- 'BUY', 'SELL', 'HOLD'
    strategy_name VARCHAR(100),
    confidence NUMERIC(3, 2), -- 0.0 to 1.0
    price_target NUMERIC(20, 8),
    stop_loss NUMERIC(20, 8),
    timeframe VARCHAR(20), -- '1m', '5m', '15m', '1h', '4h', '1d'
    indicators_json JSONB, -- Technical indicators values
    reasoning TEXT,
    status VARCHAR(20) DEFAULT 'ACTIVE', -- 'ACTIVE', 'EXECUTED', 'EXPIRED', 'CANCELLED'
    created_at TIMESTAMPTZ DEFAULT NOW()
);

CREATE INDEX idx_signals_timestamp ON signals(timestamp DESC);
CREATE INDEX idx_signals_symbol ON signals(symbol, timestamp DESC);
CREATE INDEX idx_signals_status ON signals(status);

-- ===========================
-- Positions
-- ===========================
CREATE TABLE positions (
    position_id BIGSERIAL PRIMARY KEY,
    symbol VARCHAR(20) NOT NULL,
    side VARCHAR(10) NOT NULL, -- 'LONG', 'SHORT'
    size NUMERIC(20, 8) NOT NULL,
    entry_price NUMERIC(20, 8) NOT NULL,
    current_price NUMERIC(20, 8),
    unrealized_pnl NUMERIC(20, 8),
    unrealized_pnl_percent NUMERIC(10, 4),
    stop_loss NUMERIC(20, 8),
    take_profit NUMERIC(20, 8),
    leverage NUMERIC(5, 2) DEFAULT 1.0,
    margin NUMERIC(20, 8),
    status VARCHAR(20) DEFAULT 'OPEN', -- 'OPEN', 'CLOSED', 'LIQUIDATED'
    broker VARCHAR(50), -- 'binance', 'bybit', 'okx', etc.
    opened_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    closed_at TIMESTAMPTZ,
    metadata_json JSONB
);

CREATE INDEX idx_positions_status ON positions(status);
CREATE INDEX idx_positions_symbol ON positions(symbol, status);
CREATE INDEX idx_positions_broker ON positions(broker);

-- ===========================
-- Trades
-- ===========================
CREATE TABLE trades (
    trade_id BIGSERIAL PRIMARY KEY,
    position_id BIGINT REFERENCES positions(position_id),
    signal_id BIGINT REFERENCES signals(signal_id),
    timestamp TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    symbol VARCHAR(20) NOT NULL,
    side VARCHAR(10) NOT NULL, -- 'BUY', 'SELL'
    order_type VARCHAR(20), -- 'MARKET', 'LIMIT', 'STOP_LOSS', etc.
    quantity NUMERIC(20, 8) NOT NULL,
    price NUMERIC(20, 8) NOT NULL,
    commission NUMERIC(20, 8),
    commission_asset VARCHAR(10),
    realized_pnl NUMERIC(20, 8),
    broker VARCHAR(50),
    broker_order_id VARCHAR(100),
    status VARCHAR(20) DEFAULT 'FILLED', -- 'PENDING', 'FILLED', 'CANCELLED', 'REJECTED'
    metadata_json JSONB
);

CREATE INDEX idx_trades_timestamp ON trades(timestamp DESC);
CREATE INDEX idx_trades_symbol ON trades(symbol, timestamp DESC);
CREATE INDEX idx_trades_position ON trades(position_id);
CREATE INDEX idx_trades_broker ON trades(broker);

-- ===========================
-- AI Recommendations
-- ===========================
CREATE TABLE ai_recommendations (
    recommendation_id BIGSERIAL PRIMARY KEY,
    timestamp TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    symbol VARCHAR(20) NOT NULL,
    recommendation VARCHAR(20) NOT NULL, -- 'STRONG_BUY', 'BUY', 'HOLD', 'SELL', 'STRONG_SELL'
    confidence NUMERIC(3, 2), -- 0.0 to 1.0
    target_price NUMERIC(20, 8),
    stop_loss NUMERIC(20, 8),
    timeframe VARCHAR(20),
    reasoning TEXT,
    technical_score NUMERIC(3, 2),
    fundamental_score NUMERIC(3, 2),
    sentiment_score NUMERIC(3, 2),
    risk_level VARCHAR(20), -- 'LOW', 'MEDIUM', 'HIGH', 'EXTREME'
    model_version VARCHAR(50),
    metadata_json JSONB
);

CREATE INDEX idx_ai_recommendations_symbol ON ai_recommendations(symbol, timestamp DESC);
CREATE INDEX idx_ai_recommendations_timestamp ON ai_recommendations(timestamp DESC);

-- ===========================
-- Data Ingestion Logs
-- ===========================
CREATE TABLE ingestion_logs (
    log_id BIGSERIAL PRIMARY KEY,
    timestamp TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    source_id INTEGER REFERENCES data_sources(source_id),
    job_id VARCHAR(100),
    status VARCHAR(20), -- 'SUCCESS', 'FAILED', 'PARTIAL'
    records_fetched INTEGER,
    records_inserted INTEGER,
    records_updated INTEGER,
    records_failed INTEGER,
    duration_ms INTEGER,
    error_message TEXT,
    metadata_json JSONB
);

CREATE INDEX idx_ingestion_logs_source ON ingestion_logs(source_id, timestamp DESC);
CREATE INDEX idx_ingestion_logs_status ON ingestion_logs(status);

-- ===========================
-- Portfolio Performance (Aggregated View)
-- ===========================
CREATE TABLE portfolio_snapshots (
    snapshot_id BIGSERIAL PRIMARY KEY,
    timestamp TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    total_balance NUMERIC(20, 8),
    total_equity NUMERIC(20, 8),
    available_margin NUMERIC(20, 8),
    unrealized_pnl NUMERIC(20, 8),
    realized_pnl NUMERIC(20, 8),
    total_pnl NUMERIC(20, 8),
    win_rate NUMERIC(5, 2),
    profit_factor NUMERIC(10, 4),
    sharpe_ratio NUMERIC(10, 4),
    max_drawdown NUMERIC(10, 4),
    positions_count INTEGER,
    metadata_json JSONB
);

CREATE INDEX idx_portfolio_snapshots_timestamp ON portfolio_snapshots(timestamp DESC);

-- ===========================
-- Watchlist
-- ===========================
CREATE TABLE watchlist (
    watchlist_id SERIAL PRIMARY KEY,
    symbol VARCHAR(20) UNIQUE NOT NULL,
    display_order INTEGER,
    notes TEXT,
    alerts_enabled BOOLEAN DEFAULT false,
    alert_price_above NUMERIC(20, 8),
    alert_price_below NUMERIC(20, 8),
    added_at TIMESTAMPTZ DEFAULT NOW()
);

CREATE INDEX idx_watchlist_order ON watchlist(display_order);

-- ===========================
-- System Configuration
-- ===========================
CREATE TABLE system_config (
    config_key VARCHAR(100) PRIMARY KEY,
    config_value TEXT,
    data_type VARCHAR(20), -- 'string', 'integer', 'boolean', 'json'
    description TEXT,
    updated_at TIMESTAMPTZ DEFAULT NOW()
);

-- ===========================
-- Initial Data Sources
-- ===========================
INSERT INTO data_sources (source_name, source_type, source_category, is_active) VALUES
-- Market Data Sources
('Binance', 'market_data', 'exchange', true),
('Bybit', 'market_data', 'exchange', true),
('OKX', 'market_data', 'exchange', true),
('Coinbase', 'market_data', 'exchange', true),
('Kraken', 'market_data', 'exchange', true),
('Crypto.com', 'market_data', 'exchange', true),
('Deribit', 'market_data', 'exchange', true),
('Gemini', 'market_data', 'exchange', false),

-- News Sources
('Financial Modeling Prep', 'news', 'news_api', true),
('Yahoo Finance', 'news', 'news_api', true),
('Polygon.io', 'news', 'news_api', true),
('CryptoPanic', 'news', 'news_api', true),
('CoinDesk', 'news', 'news_api', false),
('CoinTelegraph', 'news', 'news_api', false),
('Benzinga', 'news', 'news_api', false),

-- Social/Sentiment
('Reddit', 'sentiment', 'social', false),
('Twitter/X', 'sentiment', 'social', false),
('Telegram', 'sentiment', 'social', false),
('LunarCrush', 'sentiment', 'aggregator', false),

-- On-Chain
('GitHub', 'onchain', 'code_repository', false),
('CoinGecko', 'onchain', 'aggregator', false),
('Glassnode', 'onchain', 'analytics', false),
('IntoTheBlock', 'onchain', 'analytics', false),
('DeFiLlama', 'onchain', 'defi', false),

-- Alternative Data
('Google Trends', 'alt_data', 'search_trends', false),
('Whale Alert', 'alt_data', 'blockchain_monitor', false),
('Fear & Greed Index', 'alt_data', 'sentiment_index', false);

-- ===========================
-- Initial Watchlist
-- ===========================
INSERT INTO watchlist (symbol, display_order, alerts_enabled) VALUES
('BTCUSDT', 1, false),
('ETHUSDT', 2, false),
('SOLUSDT', 3, false),
('ADAUSDT', 4, false),
('BNBUSDT', 5, false);

-- ===========================
-- Initial System Config
-- ===========================
INSERT INTO system_config (config_key, config_value, data_type, description) VALUES
('api_version', '2.5.0', 'string', 'API version'),
('data_retention_days', '730', 'integer', 'Days to retain raw market data'),
('news_retention_days', '365', 'integer', 'Days to retain news articles'),
('update_interval_seconds', '2', 'integer', 'Real-time data update interval'),
('enable_auto_trading', 'false', 'boolean', 'Enable automated trading'),
('max_positions', '10', 'integer', 'Maximum concurrent positions'),
('default_leverage', '1.0', 'string', 'Default leverage for trades'),
('risk_per_trade_percent', '2.0', 'string', 'Risk percentage per trade');

-- ===========================
-- Helper Functions
-- ===========================

-- Function to get latest market data for a symbol
CREATE OR REPLACE FUNCTION get_latest_market_data(p_symbol VARCHAR)
RETURNS TABLE (
    timestamp TIMESTAMPTZ,
    open NUMERIC,
    high NUMERIC,
    low NUMERIC,
    close NUMERIC,
    volume NUMERIC,
    source_name VARCHAR
) AS $$
BEGIN
    RETURN QUERY
    SELECT
        md.timestamp,
        md.open,
        md.high,
        md.low,
        md.close,
        md.volume,
        ds.source_name
    FROM market_data md
    JOIN data_sources ds ON md.source_id = ds.source_id
    WHERE md.symbol = p_symbol
    ORDER BY md.timestamp DESC
    LIMIT 1;
END;
$$ LANGUAGE plpgsql;

-- Function to calculate unrealized PnL for all open positions
CREATE OR REPLACE FUNCTION update_positions_pnl()
RETURNS void AS $$
BEGIN
    UPDATE positions p
    SET
        current_price = (
            SELECT close
            FROM market_data
            WHERE symbol = p.symbol
            ORDER BY timestamp DESC
            LIMIT 1
        ),
        unrealized_pnl = CASE
            WHEN p.side = 'LONG' THEN
                (SELECT close FROM market_data WHERE symbol = p.symbol ORDER BY timestamp DESC LIMIT 1) - p.entry_price
            WHEN p.side = 'SHORT' THEN
                p.entry_price - (SELECT close FROM market_data WHERE symbol = p.symbol ORDER BY timestamp DESC LIMIT 1)
        END * p.size,
        unrealized_pnl_percent = CASE
            WHEN p.side = 'LONG' THEN
                ((SELECT close FROM market_data WHERE symbol = p.symbol ORDER BY timestamp DESC LIMIT 1) - p.entry_price) / p.entry_price * 100
            WHEN p.side = 'SHORT' THEN
                (p.entry_price - (SELECT close FROM market_data WHERE symbol = p.symbol ORDER BY timestamp DESC LIMIT 1)) / p.entry_price * 100
        END
    WHERE p.status = 'OPEN';
END;
$$ LANGUAGE plpgsql;

-- Grant permissions to algotrendy user
GRANT ALL ON ALL TABLES IN SCHEMA public TO algotrendy;
GRANT ALL ON ALL SEQUENCES IN SCHEMA public TO algotrendy;
GRANT EXECUTE ON ALL FUNCTIONS IN SCHEMA public TO algotrendy;
