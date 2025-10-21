/**
 * AlgoTrendy JavaScript/TypeScript API Client
 * Usage example for interacting with AlgoTrendy v2.6 API
 */

class AlgoTrendyClient {
    /**
     * Initialize AlgoTrendy client
     * @param {string} baseUrl - API base URL (e.g., "http://localhost:5002")
     * @param {string} [apiKey] - Optional API key for authentication
     */
    constructor(baseUrl, apiKey = null) {
        this.baseUrl = baseUrl.replace(/\/$/, '');
        this.apiKey = apiKey;
    }

    /**
     * Make HTTP request
     * @private
     */
    async _request(method, endpoint, data = null) {
        const url = `${this.baseUrl}${endpoint}`;
        const headers = {
            'Content-Type': 'application/json'
        };

        if (this.apiKey) {
            headers['X-API-Key'] = this.apiKey;
        }

        const options = {
            method,
            headers
        };

        if (data) {
            options.body = JSON.stringify(data);
        }

        const response = await fetch(url, options);

        if (!response.ok) {
            const error = await response.json().catch(() => ({}));
            throw new Error(error.message || `HTTP ${response.status}: ${response.statusText}`);
        }

        return response.json();
    }

    /**
     * Make GET request
     * @private
     */
    async _get(endpoint, params = null) {
        const query = params ? '?' + new URLSearchParams(params).toString() : '';
        return this._request('GET', endpoint + query);
    }

    /**
     * Make POST request
     * @private
     */
    async _post(endpoint, data) {
        return this._request('POST', endpoint, data);
    }

    // Health & Status

    /**
     * Get API health status
     */
    async getHealth() {
        return this._get('/health');
    }

    /**
     * Get detailed health status with all checks
     */
    async getDetailedHealth() {
        return this._get('/api/health/detailed');
    }

    // Trading Operations

    /**
     * Get account balance
     * @param {string} exchange - Exchange name (bybit, binance, etc.)
     * @param {string} currency - Currency symbol (default: USDT)
     */
    async getBalance(exchange, currency = 'USDT') {
        return this._get('/api/trading/balance', { exchange, currency });
    }

    /**
     * Get all open positions
     * @param {string} exchange - Exchange name
     */
    async getPositions(exchange) {
        return this._get('/api/trading/positions', { exchange });
    }

    /**
     * Place a trading order
     * @param {Object} order - Order parameters
     * @param {string} order.exchange - Exchange name (bybit, binance, etc.)
     * @param {string} order.symbol - Trading pair (BTCUSDT, ETHUSDT, etc.)
     * @param {string} order.side - Buy or Sell
     * @param {string} order.type - Market, Limit, StopLoss, etc.
     * @param {number} order.quantity - Order quantity
     * @param {number} [order.price] - Limit price (for Limit orders)
     * @param {number} [order.stopPrice] - Stop price (for Stop orders)
     * @param {string} [order.clientOrderId] - Custom order ID for idempotency
     *
     * @example
     * // Market buy
     * const order = await client.placeOrder({
     *     exchange: 'bybit',
     *     symbol: 'BTCUSDT',
     *     side: 'Buy',
     *     type: 'Market',
     *     quantity: 0.001
     * });
     *
     * @example
     * // Limit sell
     * const order = await client.placeOrder({
     *     exchange: 'bybit',
     *     symbol: 'ETHUSDT',
     *     side: 'Sell',
     *     type: 'Limit',
     *     quantity: 0.1,
     *     price: 3000.00
     * });
     */
    async placeOrder(order) {
        return this._post('/api/trading/order', order);
    }

    /**
     * Cancel an order
     * @param {string} exchange - Exchange name
     * @param {string} orderId - Order ID to cancel
     */
    async cancelOrder(exchange, orderId) {
        return this._post('/api/trading/order/cancel', { exchange, orderId });
    }

    // Market Data

    /**
     * Get historical market data
     * @param {string} symbol - Trading symbol
     * @param {string} exchange - Exchange name
     * @param {string} interval - Time interval (1m, 5m, 1h, 1d)
     * @param {number} limit - Number of candles to retrieve
     */
    async getMarketData(symbol, exchange, interval = '1h', limit = 100) {
        return this._get('/api/marketdata', { symbol, exchange, interval, limit });
    }

    // Backtesting

    /**
     * Run a backtest
     * @param {Object} params - Backtest parameters
     * @param {string} params.strategy - Strategy name
     * @param {string[]} params.symbols - List of symbols to trade
     * @param {string} params.startDate - Start date (YYYY-MM-DD)
     * @param {string} params.endDate - End date (YYYY-MM-DD)
     * @param {number} params.initialCapital - Starting capital
     * @param {string} params.engine - Backtest engine (auto, quantconnect, local, custom)
     *
     * @example
     * const results = await client.runBacktest({
     *     strategy: 'EMA Cross',
     *     symbols: ['BTCUSDT'],
     *     startDate: '2024-01-01',
     *     endDate: '2024-12-31',
     *     initialCapital: 10000
     * });
     */
    async runBacktest(params) {
        return this._post('/api/backtest/run', params);
    }

    /**
     * Get backtest results by ID
     * @param {string} backtestId - Backtest ID
     */
    async getBacktestResults(backtestId) {
        return this._get(`/api/backtest/results/${backtestId}`);
    }

    // Metrics & Monitoring

    /**
     * Get application metrics
     */
    async getMetrics() {
        return this._get('/api/metrics');
    }

    /**
     * Get metrics summary
     */
    async getMetricsSummary() {
        return this._get('/api/metrics/summary');
    }
}

// Node.js export
if (typeof module !== 'undefined' && module.exports) {
    module.exports = AlgoTrendyClient;
}

// Example Usage (Node.js)
if (typeof require !== 'undefined' && require.main === module) {
    (async () => {
        // Initialize client
        const client = new AlgoTrendyClient('http://localhost:5002');

        try {
            // Check health
            console.log('Checking API health...');
            const health = await client.getHealth();
            console.log(`✅ API Status: ${health.status}`);

            // Get balance (requires credentials configured)
            try {
                console.log('\nGetting balance...');
                const balance = await client.getBalance('bybit');
                console.log(`💰 Balance: ${balance.balance} ${balance.currency}`);
            } catch (e) {
                console.log(`⚠️ Balance check failed (credentials may not be configured): ${e.message}`);
            }

            // Get market data
            console.log('\nGetting market data...');
            const candles = await client.getMarketData('BTCUSDT', 'binance', '1h', 10);
            console.log(`📊 Retrieved ${candles.length} candles`);
            if (candles.length > 0) {
                const latest = candles[candles.length - 1];
                console.log(`   Latest: Open=${latest.open}, Close=${latest.close}`);
            }

            // Place market order (TESTNET ONLY)
            // Uncomment to test (requires testnet credentials)
            /*
            console.log('\nPlacing test order...');
            const order = await client.placeOrder({
                exchange: 'bybit',
                symbol: 'BTCUSDT',
                side: 'Buy',
                type: 'Market',
                quantity: 0.001
            });
            console.log(`✅ Order placed: ${order.orderId}`);
            console.log(`   Status: ${order.status}`);
            */

            // Run backtest
            console.log('\nRunning backtest...');
            try {
                const endDate = new Date();
                const startDate = new Date();
                startDate.setFullYear(startDate.getFullYear() - 1);

                const backtest = await client.runBacktest({
                    strategy: 'RSI',
                    symbols: ['BTCUSDT'],
                    startDate: startDate.toISOString().split('T')[0],
                    endDate: endDate.toISOString().split('T')[0],
                    initialCapital: 10000
                });
                console.log(`📈 Backtest ID: ${backtest.backtestId}`);
                console.log(`   Status: ${backtest.status}`);
            } catch (e) {
                console.log(`⚠️ Backtest failed: ${e.message}`);
            }

            // Get metrics
            console.log('\nGetting metrics...');
            try {
                const metrics = await client.getMetricsSummary();
                console.log(`📊 Total Requests: ${metrics.summary.totalRequests}`);
                console.log(`   Error Rate: ${metrics.summary.errorRate}%`);
                console.log(`   Avg Duration: ${metrics.summary.averageDurationMs}ms`);
            } catch (e) {
                console.log(`⚠️ Metrics not available: ${e.message}`);
            }

            console.log('\n✅ Example completed!');
        } catch (error) {
            console.error('❌ Error:', error.message);
        }
    })();
}
