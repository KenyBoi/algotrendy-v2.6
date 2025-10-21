-- Migration: Add ingestion configuration table
-- Purpose: Store dynamic configuration for data ingestion intervals and active symbols
-- Date: 2025-10-17

-- Create ingestion_config table
CREATE TABLE IF NOT EXISTS ingestion_config (
    id SERIAL PRIMARY KEY,
    config_key VARCHAR(100) UNIQUE NOT NULL,
    config_value TEXT NOT NULL,
    config_type VARCHAR(50) NOT NULL, -- 'interval', 'symbols', 'enabled'
    description TEXT,
    updated_at TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP,
    updated_by VARCHAR(100) DEFAULT 'system'
);

-- Create index on config_key for fast lookups
CREATE INDEX idx_ingestion_config_key ON ingestion_config(config_key);

-- Insert default configuration values
INSERT INTO ingestion_config (config_key, config_value, config_type, description) VALUES
-- Ingestion intervals (in seconds)
('market_data_interval', '60', 'interval', 'Market data ingestion interval in seconds (default: 60s / 1min)'),
('market_data_interval_min', '10', 'interval', 'Minimum allowed market data interval in seconds'),
('market_data_interval_max', '300', 'interval', 'Maximum allowed market data interval in seconds (5min)'),

('news_interval', '300', 'interval', 'News ingestion interval in seconds (default: 300s / 5min)'),
('news_interval_min', '60', 'interval', 'Minimum allowed news interval in seconds'),
('news_interval_max', '1800', 'interval', 'Maximum allowed news interval in seconds (30min)'),

('sentiment_interval', '900', 'interval', 'Sentiment ingestion interval in seconds (default: 900s / 15min)'),
('sentiment_interval_min', '300', 'interval', 'Minimum allowed sentiment interval in seconds'),
('sentiment_interval_max', '3600', 'interval', 'Maximum allowed sentiment interval in seconds (1hr)'),

('onchain_interval', '3600', 'interval', 'On-chain data interval in seconds (default: 3600s / 1hr)'),
('onchain_interval_min', '600', 'interval', 'Minimum allowed on-chain interval in seconds'),
('onchain_interval_max', '21600', 'interval', 'Maximum allowed on-chain interval in seconds (6hr)'),

-- Active symbols (JSON array)
('active_symbols', '["BTCUSDT", "ETHUSDT", "BNBUSDT", "XRPUSDT", "SOLUSDT", "ADAUSDT", "DOTUSDT", "LINKUSDT", "MATICUSDT", "AVAXUSDT"]', 'symbols', 'List of active cryptocurrency symbols to ingest'),

-- Channel enable/disable flags
('binance_enabled', 'true', 'enabled', 'Enable/disable Binance channel'),
('okx_enabled', 'true', 'enabled', 'Enable/disable OKX channel'),
('coinbase_enabled', 'true', 'enabled', 'Enable/disable Coinbase channel'),
('kraken_enabled', 'true', 'enabled', 'Enable/disable Kraken channel')

ON CONFLICT (config_key) DO NOTHING;

-- Create function to update timestamp automatically
CREATE OR REPLACE FUNCTION update_ingestion_config_timestamp()
RETURNS TRIGGER AS $$
BEGIN
    NEW.updated_at = CURRENT_TIMESTAMP;
    RETURN NEW;
END;
$$ LANGUAGE plpgsql;

-- Create trigger
DROP TRIGGER IF EXISTS ingestion_config_update_timestamp ON ingestion_config;
CREATE TRIGGER ingestion_config_update_timestamp
    BEFORE UPDATE ON ingestion_config
    FOR EACH ROW
    EXECUTE FUNCTION update_ingestion_config_timestamp();

-- Grant permissions (adjust as needed)
GRANT SELECT, INSERT, UPDATE ON ingestion_config TO PUBLIC;
GRANT USAGE, SELECT ON SEQUENCE ingestion_config_id_seq TO PUBLIC;

COMMENT ON TABLE ingestion_config IS 'Configuration for data ingestion intervals and active symbols';
COMMENT ON COLUMN ingestion_config.config_key IS 'Unique configuration key';
COMMENT ON COLUMN ingestion_config.config_value IS 'Configuration value (string, number, or JSON)';
COMMENT ON COLUMN ingestion_config.config_type IS 'Type of configuration: interval, symbols, enabled';
COMMENT ON COLUMN ingestion_config.description IS 'Human-readable description of the configuration';
