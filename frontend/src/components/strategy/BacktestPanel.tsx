import { useState } from 'react';
import { Play, TrendingUp, TrendingDown } from 'lucide-react';
import { BacktestConfig, BacktestResults } from '../../types/strategy';
import { LineChart, Line, XAxis, YAxis, CartesianGrid, Tooltip, ResponsiveContainer } from 'recharts';

interface Props {
  onRunBacktest: (config: BacktestConfig) => void;
  results: BacktestResults | null;
}

export default function BacktestPanel({ onRunBacktest, results }: Props) {
  const [config, setConfig] = useState<BacktestConfig>({
    symbol: 'BTC-USD',
    timeframe: '1h',
    startDate: new Date(Date.now() - 90 * 24 * 60 * 60 * 1000).toISOString().split('T')[0],
    endDate: new Date().toISOString().split('T')[0],
    initialCapital: 10000,
    commission: 0.1,
    slippage: 0.05,
  });

  const handleRunBacktest = () => {
    onRunBacktest(config);
  };

  return (
    <div className="backtest-panel">
      <h2 className="mt-0">Backtest Configuration</h2>

      <div className="backtest-config">
        <div className="form-grid">
          <div className="form-group">
            <label>Symbol *</label>
            <input
              type="text"
              value={config.symbol}
              onChange={e => setConfig({ ...config, symbol: e.target.value })}
              className="form-input"
              placeholder="e.g., BTC-USD"
            />
          </div>

          <div className="form-group">
            <label>Timeframe *</label>
            <select
              value={config.timeframe}
              onChange={e => setConfig({ ...config, timeframe: e.target.value })}
              className="form-input"
            >
              <option value="1m">1 Minute</option>
              <option value="5m">5 Minutes</option>
              <option value="15m">15 Minutes</option>
              <option value="1h">1 Hour</option>
              <option value="4h">4 Hours</option>
              <option value="1d">1 Day</option>
            </select>
          </div>

          <div className="form-group">
            <label>Start Date *</label>
            <input
              type="date"
              value={config.startDate}
              onChange={e => setConfig({ ...config, startDate: e.target.value })}
              className="form-input"
            />
          </div>

          <div className="form-group">
            <label>End Date *</label>
            <input
              type="date"
              value={config.endDate}
              onChange={e => setConfig({ ...config, endDate: e.target.value })}
              className="form-input"
            />
          </div>

          <div className="form-group">
            <label>Initial Capital ($) *</label>
            <input
              type="number"
              value={config.initialCapital}
              onChange={e => setConfig({ ...config, initialCapital: parseFloat(e.target.value) })}
              className="form-input"
              min="100"
              step="100"
            />
          </div>

          <div className="form-group">
            <label>Commission (%)</label>
            <input
              type="number"
              value={config.commission || ''}
              onChange={e => setConfig({ ...config, commission: parseFloat(e.target.value) || 0 })}
              className="form-input"
              min="0"
              max="5"
              step="0.01"
              placeholder="0.1"
            />
          </div>

          <div className="form-group">
            <label>Slippage (%)</label>
            <input
              type="number"
              value={config.slippage || ''}
              onChange={e => setConfig({ ...config, slippage: parseFloat(e.target.value) || 0 })}
              className="form-input"
              min="0"
              max="5"
              step="0.01"
              placeholder="0.05"
            />
          </div>
        </div>

        <button
          className="btn-primary mt-lg"
          onClick={handleRunBacktest}
        >
          <Play size={18} />
          Run Backtest
        </button>
      </div>

      {results && (
        <div className="backtest-results">
          <h2>Backtest Results</h2>

          <div className="metrics-grid">
            <MetricCard
              label="Total Return"
              value={`${(results.totalReturn * 100).toFixed(2)}%`}
              trend={results.totalReturn > 0 ? 'up' : 'down'}
            />
            <MetricCard
              label="Sharpe Ratio"
              value={results.sharpeRatio.toFixed(2)}
              trend={results.sharpeRatio > 1 ? 'up' : 'neutral'}
            />
            <MetricCard
              label="Max Drawdown"
              value={`${(results.maxDrawdown * 100).toFixed(2)}%`}
              trend="down"
            />
            <MetricCard
              label="Win Rate"
              value={`${(results.winRate * 100).toFixed(1)}%`}
              trend={results.winRate > 0.5 ? 'up' : 'neutral'}
            />
            <MetricCard
              label="Profit Factor"
              value={results.profitFactor.toFixed(2)}
              trend={results.profitFactor > 1 ? 'up' : 'down'}
            />
            <MetricCard
              label="Total Trades"
              value={results.totalTrades.toString()}
              trend="neutral"
            />
          </div>

          {results.equity && results.equity.length > 0 && (
            <div className="equity-chart">
              <h3>Equity Curve</h3>
              <ResponsiveContainer width="100%" height={300}>
                <LineChart data={results.equity}>
                  <CartesianGrid strokeDasharray="3 3" stroke="var(--border)" />
                  <XAxis dataKey="timestamp" stroke="var(--text-secondary)" />
                  <YAxis stroke="var(--text-secondary)" />
                  <Tooltip
                    contentStyle={{
                      background: 'var(--card-bg)',
                      border: '1px solid var(--border)',
                      borderRadius: '6px',
                    }}
                  />
                  <Line type="monotone" dataKey="value" stroke="var(--primary)" strokeWidth={2} dot={false} />
                </LineChart>
              </ResponsiveContainer>
            </div>
          )}
        </div>
      )}
    </div>
  );
}

function MetricCard({ label, value, trend }: { label: string; value: string; trend: 'up' | 'down' | 'neutral' }) {
  const TrendIcon = trend === 'up' ? TrendingUp : trend === 'down' ? TrendingDown : null;
  const trendClass = trend === 'up' ? 'positive' : trend === 'down' ? 'negative' : 'neutral';

  return (
    <div className="metric-card">
      <div className="metric-label">{label}</div>
      <div className="metric-value">
        <span className={`metric-value-text ${trendClass}`}>
          {value}
        </span>
        {TrendIcon && <TrendIcon size={20} className={`text-${trend === 'up' ? 'success' : trend === 'down' ? 'error' : 'secondary'}`} />}
      </div>
    </div>
  );
}
