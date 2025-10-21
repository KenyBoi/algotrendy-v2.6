import { useState, useEffect } from 'react';
import { useRouter } from 'next/router';
import { backtestService, BacktestConfig, BacktestResults } from '@/services/backtest';

export default function BacktestingModule() {
  const router = useRouter();
  const [loading, setLoading] = useState(false);
  const [backtestResults, setBacktestResults] = useState<BacktestResults | null>(null);
  const [configOptions, setConfigOptions] = useState<any>(null);

  // Form state
  const [formData, setFormData] = useState<BacktestConfig>({
    ai_name: 'MemGPT AI v1',
    backtester: 'custom',
    asset_class: 'crypto',
    symbol: 'BTCUSDT',
    timeframe: 'day',
    timeframe_value: 1,
    start_date: '2024-01-01',
    end_date: '2024-10-01',
    initial_capital: 10000,
    indicators: {
      sma: false,
      ema: false,
      rsi: false,
      macd: false,
      bollinger: false,
      atr: false,
      stochastic: false,
      volume: false,
    },
    indicator_params: {
      sma_period: 20,
      ema_period: 12,
      rsi_period: 14,
      macd_fast: 12,
      macd_slow: 26,
      macd_signal: 9,
      bollinger_period: 20,
      bollinger_std: 2,
      atr_period: 14,
      stochastic_k: 14,
      stochastic_d: 3,
    },
    commission: 0.001,
    slippage: 0.0005,
  });

  // Load configuration options on mount
  useEffect(() => {
    loadConfigOptions();
  }, []);

  const loadConfigOptions = async () => {
    try {
      const options = await backtestService.getConfig();
      setConfigOptions(options);
    } catch (error) {
      console.error('Error loading config options:', error);
    }
  };

  const getSymbolOptions = () => {
    if (!configOptions) return ['BTCUSDT'];
    const assetClass = configOptions.asset_classes.find(
      (ac: any) => ac.value === formData.asset_class
    );
    return assetClass?.symbols || [];
  };

  const handleInputChange = (field: string, value: any) => {
    setFormData(prev => ({ ...prev, [field]: value }));
  };

  const handleIndicatorToggle = (indicator: string) => {
    setFormData(prev => ({
      ...prev,
      indicators: {
        ...prev.indicators,
        [indicator]: !prev.indicators[indicator],
      }
    }));
  };

  const handleIndicatorParamChange = (param: string, value: number) => {
    setFormData(prev => ({
      ...prev,
      indicator_params: {
        ...prev.indicator_params,
        [param]: value,
      }
    }));
  };

  const runBacktest = async () => {
    setLoading(true);
    try {
      const results = await backtestService.runBacktest(formData);
      setBacktestResults(results);
    } catch (error) {
      console.error('Error running backtest:', error);
      alert('Backtest failed. Please check the console for details.');
    } finally {
      setLoading(false);
    }
  };

  const formatCurrency = (value: number) => {
    return new Intl.NumberFormat('en-US', {
      style: 'currency',
      currency: 'USD',
      minimumFractionDigits: 2,
    }).format(value);
  };

  const formatPercent = (value: number) => {
    return `${value >= 0 ? '+' : ''}${value.toFixed(2)}%`;
  };

  return (
    <div className="min-h-screen bg-gradient-to-br from-gray-900 via-indigo-900 to-gray-900 text-white p-6">
      <div className="max-w-7xl mx-auto">
        {/* Header */}
        <div className="flex justify-between items-center mb-6">
          <div>
            <h1 className="text-4xl font-bold bg-gradient-to-r from-indigo-400 to-purple-500 bg-clip-text text-transparent">
              🧪 Backtesting Module
            </h1>
            <p className="text-gray-400 mt-1">Test your AI strategies with historical data</p>
          </div>
          <div className="flex gap-2">
            <button
              onClick={() => router.push('/dashboard')}
              className="px-4 py-2 bg-cyan-500/20 border border-cyan-500 rounded-lg text-cyan-400 hover:bg-cyan-500/30 transition"
            >
              📊 Dashboard
            </button>
            <button
              onClick={() => router.push('/settings')}
              className="px-4 py-2 bg-purple-500/20 border border-purple-500 rounded-lg text-purple-400 hover:bg-purple-500/30 transition"
            >
              ⚙️ Settings
            </button>
          </div>
        </div>

        <div className="grid grid-cols-1 lg:grid-cols-3 gap-6">
          {/* Configuration Panel (Left - 2 columns) */}
          <div className="lg:col-span-2 space-y-6">

            {/* AI & Engine Selection */}
            <div className="bg-white/5 backdrop-blur-sm border border-white/10 rounded-xl p-6">
              <h2 className="text-xl font-bold mb-4 flex items-center gap-2">
                🤖 Configuration
              </h2>

              <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
                {/* AI Name */}
                <div>
                  <label className="block text-sm font-medium text-gray-300 mb-2">
                    AI Model
                  </label>
                  <select
                    value={formData.ai_name}
                    onChange={(e) => handleInputChange('ai_name', e.target.value)}
                    className="w-full px-4 py-2 bg-gray-800 border border-gray-700 rounded-lg text-white focus:ring-2 focus:ring-indigo-500 focus:outline-none"
                  >
                    <option value="MemGPT AI v1">MemGPT AI v1</option>
                    <option value="MemGPT AI v2" disabled>MemGPT AI v2 (Coming Soon)</option>
                  </select>
                </div>

                {/* Backtester Selection */}
                <div>
                  <label className="block text-sm font-medium text-gray-300 mb-2">
                    Backtesting Engine
                  </label>
                  <select
                    value={formData.backtester}
                    onChange={(e) => handleInputChange('backtester', e.target.value as any)}
                    className="w-full px-4 py-2 bg-gray-800 border border-gray-700 rounded-lg text-white focus:ring-2 focus:ring-indigo-500 focus:outline-none"
                  >
                    <option value="custom">Custom Engine ✅</option>
                    <option value="quantconnect">QuantConnect (Coming Soon)</option>
                    <option value="backtester">Backtester.com (Coming Soon)</option>
                  </select>
                </div>
              </div>
            </div>

            {/* Asset & Symbol Selection */}
            <div className="bg-white/5 backdrop-blur-sm border border-white/10 rounded-xl p-6">
              <h2 className="text-xl font-bold mb-4 flex items-center gap-2">
                💰 Asset Selection
              </h2>

              <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
                {/* Asset Class */}
                <div>
                  <label className="block text-sm font-medium text-gray-300 mb-2">
                    Asset Class
                  </label>
                  <select
                    value={formData.asset_class}
                    onChange={(e) => {
                      handleInputChange('asset_class', e.target.value as any);
                      const symbols = getSymbolOptions();
                      if (symbols.length > 0) {
                        handleInputChange('symbol', symbols[0]);
                      }
                    }}
                    className="w-full px-4 py-2 bg-gray-800 border border-gray-700 rounded-lg text-white focus:ring-2 focus:ring-indigo-500 focus:outline-none"
                  >
                    <option value="crypto">Cryptocurrency</option>
                    <option value="futures">Futures</option>
                    <option value="equities">Equities</option>
                  </select>
                </div>

                {/* Symbol */}
                <div>
                  <label className="block text-sm font-medium text-gray-300 mb-2">
                    Symbol
                  </label>
                  <select
                    value={formData.symbol}
                    onChange={(e) => handleInputChange('symbol', e.target.value)}
                    className="w-full px-4 py-2 bg-gray-800 border border-gray-700 rounded-lg text-white focus:ring-2 focus:ring-indigo-500 focus:outline-none"
                  >
                    {getSymbolOptions().map(symbol => (
                      <option key={symbol} value={symbol}>{symbol}</option>
                    ))}
                  </select>
                </div>
              </div>
            </div>

            {/* Timeframe Selection */}
            <div className="bg-white/5 backdrop-blur-sm border border-white/10 rounded-xl p-6">
              <h2 className="text-xl font-bold mb-4 flex items-center gap-2">
                ⏰ Timeframe
              </h2>

              <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
                <div>
                  <label className="block text-sm font-medium text-gray-300 mb-2">
                    Timeframe Type
                  </label>
                  <select
                    value={formData.timeframe}
                    onChange={(e) => handleInputChange('timeframe', e.target.value as any)}
                    className="w-full px-4 py-2 bg-gray-800 border border-gray-700 rounded-lg text-white focus:ring-2 focus:ring-indigo-500 focus:outline-none"
                  >
                    <option value="tick">Tick</option>
                    <option value="min">Minute</option>
                    <option value="hr">Hour</option>
                    <option value="day">Day</option>
                    <option value="wk">Week</option>
                    <option value="mo">Month</option>
                    <option value="renko">Renko</option>
                    <option value="line">Line Break</option>
                    <option value="range">Range</option>
                  </select>
                </div>

                {(formData.timeframe === 'min' || formData.timeframe === 'hr') && (
                  <div>
                    <label className="block text-sm font-medium text-gray-300 mb-2">
                      {formData.timeframe === 'min' ? 'Minutes' : 'Hours'}
                    </label>
                    <input
                      type="number"
                      min="1"
                      value={formData.timeframe_value}
                      onChange={(e) => handleInputChange('timeframe_value', parseInt(e.target.value))}
                      className="w-full px-4 py-2 bg-gray-800 border border-gray-700 rounded-lg text-white focus:ring-2 focus:ring-indigo-500 focus:outline-none"
                    />
                  </div>
                )}
              </div>

              <div className="grid grid-cols-1 md:grid-cols-2 gap-4 mt-4">
                <div>
                  <label className="block text-sm font-medium text-gray-300 mb-2">
                    Start Date
                  </label>
                  <input
                    type="date"
                    value={formData.start_date}
                    onChange={(e) => handleInputChange('start_date', e.target.value)}
                    className="w-full px-4 py-2 bg-gray-800 border border-gray-700 rounded-lg text-white focus:ring-2 focus:ring-indigo-500 focus:outline-none"
                  />
                </div>
                <div>
                  <label className="block text-sm font-medium text-gray-300 mb-2">
                    End Date
                  </label>
                  <input
                    type="date"
                    value={formData.end_date}
                    onChange={(e) => handleInputChange('end_date', e.target.value)}
                    className="w-full px-4 py-2 bg-gray-800 border border-gray-700 rounded-lg text-white focus:ring-2 focus:ring-indigo-500 focus:outline-none"
                  />
                </div>
              </div>
            </div>

            {/* Indicators Selection */}
            <div className="bg-white/5 backdrop-blur-sm border border-white/10 rounded-xl p-6">
              <h2 className="text-xl font-bold mb-4 flex items-center gap-2">
                📈 Technical Indicators
              </h2>

              <div className="grid grid-cols-2 md:grid-cols-4 gap-4">
                {Object.entries(formData.indicators).map(([key, value]) => (
                  <div key={key}>
                    <label className="flex items-center space-x-2 cursor-pointer hover:text-indigo-400 transition">
                      <input
                        type="checkbox"
                        checked={value}
                        onChange={() => handleIndicatorToggle(key)}
                        className="w-4 h-4 text-indigo-600 bg-gray-800 border-gray-700 rounded focus:ring-indigo-500"
                      />
                      <span className="text-sm text-gray-300 uppercase font-semibold">{key}</span>
                    </label>
                  </div>
                ))}
              </div>

              {/* Indicator Parameters */}
              {Object.values(formData.indicators).some(v => v) && (
                <div className="mt-6 pt-6 border-t border-gray-700">
                  <h3 className="text-lg font-semibold mb-4 text-indigo-300">Indicator Parameters</h3>
                  <div className="grid grid-cols-2 md:grid-cols-3 gap-4">
                    {formData.indicators.sma && (
                      <div>
                        <label className="block text-xs text-gray-400 mb-1">SMA Period</label>
                        <input
                          type="number"
                          value={formData.indicator_params.sma_period}
                          onChange={(e) => handleIndicatorParamChange('sma_period', parseInt(e.target.value))}
                          className="w-full px-3 py-1 text-sm bg-gray-800 border border-gray-700 rounded text-white focus:ring-2 focus:ring-indigo-500 focus:outline-none"
                        />
                      </div>
                    )}
                    {formData.indicators.ema && (
                      <div>
                        <label className="block text-xs text-gray-400 mb-1">EMA Period</label>
                        <input
                          type="number"
                          value={formData.indicator_params.ema_period}
                          onChange={(e) => handleIndicatorParamChange('ema_period', parseInt(e.target.value))}
                          className="w-full px-3 py-1 text-sm bg-gray-800 border border-gray-700 rounded text-white focus:ring-2 focus:ring-indigo-500 focus:outline-none"
                        />
                      </div>
                    )}
                    {formData.indicators.rsi && (
                      <div>
                        <label className="block text-xs text-gray-400 mb-1">RSI Period</label>
                        <input
                          type="number"
                          value={formData.indicator_params.rsi_period}
                          onChange={(e) => handleIndicatorParamChange('rsi_period', parseInt(e.target.value))}
                          className="w-full px-3 py-1 text-sm bg-gray-800 border border-gray-700 rounded text-white focus:ring-2 focus:ring-indigo-500 focus:outline-none"
                        />
                      </div>
                    )}
                    {formData.indicators.macd && (
                      <>
                        <div>
                          <label className="block text-xs text-gray-400 mb-1">MACD Fast</label>
                          <input
                            type="number"
                            value={formData.indicator_params.macd_fast}
                            onChange={(e) => handleIndicatorParamChange('macd_fast', parseInt(e.target.value))}
                            className="w-full px-3 py-1 text-sm bg-gray-800 border border-gray-700 rounded text-white focus:ring-2 focus:ring-indigo-500 focus:outline-none"
                          />
                        </div>
                        <div>
                          <label className="block text-xs text-gray-400 mb-1">MACD Slow</label>
                          <input
                            type="number"
                            value={formData.indicator_params.macd_slow}
                            onChange={(e) => handleIndicatorParamChange('macd_slow', parseInt(e.target.value))}
                            className="w-full px-3 py-1 text-sm bg-gray-800 border border-gray-700 rounded text-white focus:ring-2 focus:ring-indigo-500 focus:outline-none"
                          />
                        </div>
                        <div>
                          <label className="block text-xs text-gray-400 mb-1">MACD Signal</label>
                          <input
                            type="number"
                            value={formData.indicator_params.macd_signal}
                            onChange={(e) => handleIndicatorParamChange('macd_signal', parseInt(e.target.value))}
                            className="w-full px-3 py-1 text-sm bg-gray-800 border border-gray-700 rounded text-white focus:ring-2 focus:ring-indigo-500 focus:outline-none"
                          />
                        </div>
                      </>
                    )}
                    {formData.indicators.bollinger && (
                      <>
                        <div>
                          <label className="block text-xs text-gray-400 mb-1">BB Period</label>
                          <input
                            type="number"
                            value={formData.indicator_params.bollinger_period}
                            onChange={(e) => handleIndicatorParamChange('bollinger_period', parseInt(e.target.value))}
                            className="w-full px-3 py-1 text-sm bg-gray-800 border border-gray-700 rounded text-white focus:ring-2 focus:ring-indigo-500 focus:outline-none"
                          />
                        </div>
                        <div>
                          <label className="block text-xs text-gray-400 mb-1">BB Std Dev</label>
                          <input
                            type="number"
                            step="0.1"
                            value={formData.indicator_params.bollinger_std}
                            onChange={(e) => handleIndicatorParamChange('bollinger_std', parseFloat(e.target.value))}
                            className="w-full px-3 py-1 text-sm bg-gray-800 border border-gray-700 rounded text-white focus:ring-2 focus:ring-indigo-500 focus:outline-none"
                          />
                        </div>
                      </>
                    )}
                    {formData.indicators.atr && (
                      <div>
                        <label className="block text-xs text-gray-400 mb-1">ATR Period</label>
                        <input
                          type="number"
                          value={formData.indicator_params.atr_period}
                          onChange={(e) => handleIndicatorParamChange('atr_period', parseInt(e.target.value))}
                          className="w-full px-3 py-1 text-sm bg-gray-800 border border-gray-700 rounded text-white focus:ring-2 focus:ring-indigo-500 focus:outline-none"
                        />
                      </div>
                    )}
                    {formData.indicators.stochastic && (
                      <>
                        <div>
                          <label className="block text-xs text-gray-400 mb-1">Stoch %K</label>
                          <input
                            type="number"
                            value={formData.indicator_params.stochastic_k}
                            onChange={(e) => handleIndicatorParamChange('stochastic_k', parseInt(e.target.value))}
                            className="w-full px-3 py-1 text-sm bg-gray-800 border border-gray-700 rounded text-white focus:ring-2 focus:ring-indigo-500 focus:outline-none"
                          />
                        </div>
                        <div>
                          <label className="block text-xs text-gray-400 mb-1">Stoch %D</label>
                          <input
                            type="number"
                            value={formData.indicator_params.stochastic_d}
                            onChange={(e) => handleIndicatorParamChange('stochastic_d', parseInt(e.target.value))}
                            className="w-full px-3 py-1 text-sm bg-gray-800 border border-gray-700 rounded text-white focus:ring-2 focus:ring-indigo-500 focus:outline-none"
                          />
                        </div>
                      </>
                    )}
                  </div>
                </div>
              )}
            </div>

            {/* Capital & Run Button */}
            <div className="bg-white/5 backdrop-blur-sm border border-white/10 rounded-xl p-6">
              <div className="flex items-center justify-between">
                <div className="flex-1 mr-4">
                  <label className="block text-sm font-medium text-gray-300 mb-2">
                    Initial Capital ($)
                  </label>
                  <input
                    type="number"
                    value={formData.initial_capital}
                    onChange={(e) => handleInputChange('initial_capital', parseFloat(e.target.value))}
                    className="w-full px-4 py-2 bg-gray-800 border border-gray-700 rounded-lg text-white focus:ring-2 focus:ring-indigo-500 focus:outline-none"
                  />
                </div>
                <button
                  onClick={runBacktest}
                  disabled={loading}
                  className="mt-6 px-8 py-3 bg-gradient-to-r from-indigo-500 to-purple-600 text-white font-semibold rounded-lg hover:from-indigo-600 hover:to-purple-700 disabled:opacity-50 disabled:cursor-not-allowed transition shadow-lg hover:shadow-xl"
                >
                  {loading ? (
                    <span className="flex items-center gap-2">
                      <svg className="animate-spin h-5 w-5" viewBox="0 0 24 24">
                        <circle className="opacity-25" cx="12" cy="12" r="10" stroke="currentColor" strokeWidth="4" fill="none"></circle>
                        <path className="opacity-75" fill="currentColor" d="M4 12a8 8 0 018-8V0C5.373 0 0 5.373 0 12h4zm2 5.291A7.962 7.962 0 014 12H0c0 3.042 1.135 5.824 3 7.938l3-2.647z"></path>
                      </svg>
                      Running...
                    </span>
                  ) : '🚀 Run Backtest'}
                </button>
              </div>
            </div>
          </div>

          {/* Results Panel (Right - 1 column) */}
          <div className="lg:col-span-1">
            <div className="bg-white/5 backdrop-blur-sm border border-white/10 rounded-xl p-6 sticky top-6">
              <h2 className="text-xl font-bold mb-4 flex items-center gap-2">
                📊 Quick Stats
              </h2>

              {backtestResults?.metrics ? (
                <div className="space-y-4">
                  <div className="bg-gradient-to-r from-green-500/20 to-emerald-600/20 border border-green-500/30 rounded-lg p-4">
                    <div className="text-sm text-gray-300">Total Return</div>
                    <div className={`text-2xl font-bold ${backtestResults.metrics.total_return >= 0 ? 'text-green-400' : 'text-red-400'}`}>
                      {formatPercent(backtestResults.metrics.total_return)}
                    </div>
                  </div>

                  <div className="bg-gray-800/50 rounded-lg p-4 border border-gray-700">
                    <div className="text-sm text-gray-400">Sharpe Ratio</div>
                    <div className="text-2xl font-bold text-blue-400">{backtestResults.metrics.sharpe_ratio.toFixed(2)}</div>
                  </div>

                  <div className="bg-gray-800/50 rounded-lg p-4 border border-gray-700">
                    <div className="text-sm text-gray-400">Win Rate</div>
                    <div className="text-2xl font-bold text-indigo-400">{backtestResults.metrics.win_rate.toFixed(1)}%</div>
                  </div>

                  <div className="bg-gradient-to-r from-red-500/20 to-orange-600/20 border border-red-500/30 rounded-lg p-4">
                    <div className="text-sm text-gray-300">Max Drawdown</div>
                    <div className="text-2xl font-bold text-red-400">
                      {backtestResults.metrics.max_drawdown.toFixed(2)}%
                    </div>
                  </div>

                  <div className="bg-gray-800/50 rounded-lg p-4 border border-gray-700">
                    <div className="text-sm text-gray-400">Total Trades</div>
                    <div className="text-2xl font-bold text-purple-400">{backtestResults.metrics.total_trades}</div>
                  </div>

                  <div className="bg-gray-800/50 rounded-lg p-4 border border-gray-700">
                    <div className="text-sm text-gray-400">Profit Factor</div>
                    <div className="text-2xl font-bold text-cyan-400">{backtestResults.metrics.profit_factor.toFixed(2)}</div>
                  </div>

                  <div className="pt-4 border-t border-gray-700 space-y-2 text-sm">
                    <div className="flex justify-between">
                      <span className="text-gray-400">Winning Trades</span>
                      <span className="text-green-400 font-semibold">{backtestResults.metrics.winning_trades}</span>
                    </div>
                    <div className="flex justify-between">
                      <span className="text-gray-400">Losing Trades</span>
                      <span className="text-red-400 font-semibold">{backtestResults.metrics.losing_trades}</span>
                    </div>
                    <div className="flex justify-between">
                      <span className="text-gray-400">Avg Win</span>
                      <span className="text-green-400 font-semibold">{formatCurrency(backtestResults.metrics.avg_win)}</span>
                    </div>
                    <div className="flex justify-between">
                      <span className="text-gray-400">Avg Loss</span>
                      <span className="text-red-400 font-semibold">{formatCurrency(backtestResults.metrics.avg_loss)}</span>
                    </div>
                  </div>
                </div>
              ) : (
                <div className="text-center text-gray-500 py-12">
                  <div className="text-6xl mb-4">📈</div>
                  <p>Configure and run a backtest</p>
                  <p className="text-sm mt-2">Results will appear here</p>
                </div>
              )}
            </div>
          </div>
        </div>

        {/* Trade History Section (if results exist) */}
        {backtestResults?.trades && backtestResults.trades.length > 0 && (
          <div className="mt-6 bg-white/5 backdrop-blur-sm border border-white/10 rounded-xl p-6">
            <h2 className="text-xl font-bold mb-4 flex items-center gap-2">
              📋 Trade History ({backtestResults.trades.length} trades)
            </h2>
            <div className="overflow-x-auto">
              <table className="w-full text-sm">
                <thead className="border-b border-gray-700">
                  <tr className="text-gray-400">
                    <th className="text-left py-3 px-2">Entry Time</th>
                    <th className="text-left py-3 px-2">Exit Time</th>
                    <th className="text-right py-3 px-2">Entry Price</th>
                    <th className="text-right py-3 px-2">Exit Price</th>
                    <th className="text-right py-3 px-2">Quantity</th>
                    <th className="text-right py-3 px-2">P&L</th>
                    <th className="text-right py-3 px-2">P&L %</th>
                    <th className="text-right py-3 px-2">Duration</th>
                  </tr>
                </thead>
                <tbody>
                  {backtestResults.trades.slice(0, 20).map((trade, idx) => (
                    <tr key={idx} className="border-b border-gray-800 hover:bg-white/5">
                      <td className="py-3 px-2 text-gray-300">{new Date(trade.entry_time).toLocaleDateString()}</td>
                      <td className="py-3 px-2 text-gray-300">{trade.exit_time ? new Date(trade.exit_time).toLocaleDateString() : '-'}</td>
                      <td className="py-3 px-2 text-right">{formatCurrency(trade.entry_price)}</td>
                      <td className="py-3 px-2 text-right">{trade.exit_price ? formatCurrency(trade.exit_price) : '-'}</td>
                      <td className="py-3 px-2 text-right">{trade.quantity.toFixed(4)}</td>
                      <td className={`py-3 px-2 text-right font-semibold ${(trade.pnl || 0) >= 0 ? 'text-green-400' : 'text-red-400'}`}>
                        {trade.pnl ? formatCurrency(trade.pnl) : '-'}
                      </td>
                      <td className={`py-3 px-2 text-right font-semibold ${(trade.pnl_percent || 0) >= 0 ? 'text-green-400' : 'text-red-400'}`}>
                        {trade.pnl_percent ? formatPercent(trade.pnl_percent) : '-'}
                      </td>
                      <td className="py-3 px-2 text-right text-gray-400">{trade.duration_minutes ? `${trade.duration_minutes}m` : '-'}</td>
                    </tr>
                  ))}
                </tbody>
              </table>
              {backtestResults.trades.length > 20 && (
                <div className="text-center text-gray-500 mt-4 text-sm">
                  Showing first 20 of {backtestResults.trades.length} trades
                </div>
              )}
            </div>
          </div>
        )}
      </div>
    </div>
  );
}
