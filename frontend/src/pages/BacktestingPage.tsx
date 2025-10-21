import { useState, useEffect } from 'react';
import { Play, Download, Calendar, TrendingUp, Activity } from 'lucide-react';
import { LineChart, Line, XAxis, YAxis, CartesianGrid, Tooltip, ResponsiveContainer } from 'recharts';
import { tradingApi } from '../lib/tradingApi';

interface BacktestResult {
  id: string;
  strategy: string;
  symbol: string;
  startDate: string;
  endDate: string;
  initialCapital: number;
  finalValue: number;
  totalReturn: number;
  sharpeRatio: number;
  maxDrawdown: number;
  winRate: number;
  totalTrades: number;
  status: 'completed' | 'running' | 'failed';
  equityCurve: Array<{ date: string; value: number }>;
}

export default function BacktestingPage() {
  const [strategy, setStrategy] = useState('RSI');
  const [symbol, setSymbol] = useState('BTC-USD');
  const [startDate, setStartDate] = useState('2024-01-01');
  const [endDate, setEndDate] = useState('2024-12-31');
  const [initialCapital, setInitialCapital] = useState('10000');
  const [running, setRunning] = useState(false);
  const [results, setResults] = useState<BacktestResult[]>([]);
  const [selectedResult, setSelectedResult] = useState<BacktestResult | null>(null);

  const strategies = ['RSI', 'MACD', 'Momentum', 'VWAP', 'MFI', 'MEM-Enhanced'];
  const symbols = ['BTC-USD', 'ETH-USD', 'BNB-USD', 'XRP-USD', 'SOL-USD', 'ADA-USD'];

  useEffect(() => {
    loadBacktestResults();
  }, []);

  const loadBacktestResults = async () => {
    try {
      const data = await tradingApi.getBacktestResults();
      setResults(data);
      if (data.length > 0) {
        setSelectedResult(data[0]);
      }
    } catch (error) {
      console.error('Error loading backtest results:', error);
    }
  };

  const handleRunBacktest = async () => {
    setRunning(true);

    try {
      const result = await tradingApi.runBacktest({
        strategy,
        symbol,
        startDate,
        endDate,
        initialCapital: parseFloat(initialCapital),
      });

      setResults([result, ...results]);
      setSelectedResult(result);
      setRunning(false);
    } catch (error) {
      console.error('Error running backtest:', error);
      alert(`Failed to run backtest: ${error instanceof Error ? error.message : 'Unknown error'}`);
      setRunning(false);
    }
  };

  const performanceMetrics = selectedResult ? [
    { name: 'Total Trades', value: selectedResult.totalTrades },
    { name: 'Win Rate', value: selectedResult.winRate.toFixed(2) + '%' },
    { name: 'Sharpe Ratio', value: selectedResult.sharpeRatio.toFixed(2) },
    { name: 'Max Drawdown', value: selectedResult.maxDrawdown.toFixed(2) + '%' },
  ] : [];

  return (
    <div className="dashboard">
      {/* Backtest Configuration */}
      <div className="card">
        <div className="card-header">
          <h2 className="card-title">🔬 Backtesting Engine</h2>
          <div style={{ display: 'flex', gap: '1rem', alignItems: 'center' }}>
            <span className="badge badge-success">QuantConnect Integration</span>
            <span className="badge badge-success">Local LEAN Engine</span>
          </div>
        </div>

        <div style={{ display: 'grid', gridTemplateColumns: 'repeat(auto-fit, minmax(200px, 1fr))', gap: '1rem' }}>
          {/* Strategy */}
          <div>
            <label style={{ display: 'block', fontSize: '0.875rem', marginBottom: '0.5rem', color: 'var(--text-secondary)' }}>
              Strategy
            </label>
            <select
              value={strategy}
              onChange={(e) => setStrategy(e.target.value)}
              style={{
                width: '100%',
                padding: '0.5rem',
                backgroundColor: 'var(--bg-tertiary)',
                border: '1px solid var(--border)',
                borderRadius: '6px',
                color: 'var(--text-primary)',
              }}
            >
              {strategies.map(s => (
                <option key={s} value={s}>{s}</option>
              ))}
            </select>
          </div>

          {/* Symbol */}
          <div>
            <label style={{ display: 'block', fontSize: '0.875rem', marginBottom: '0.5rem', color: 'var(--text-secondary)' }}>
              Symbol
            </label>
            <select
              value={symbol}
              onChange={(e) => setSymbol(e.target.value)}
              style={{
                width: '100%',
                padding: '0.5rem',
                backgroundColor: 'var(--bg-tertiary)',
                border: '1px solid var(--border)',
                borderRadius: '6px',
                color: 'var(--text-primary)',
              }}
            >
              {symbols.map(s => (
                <option key={s} value={s}>{s}</option>
              ))}
            </select>
          </div>

          {/* Start Date */}
          <div>
            <label style={{ display: 'block', fontSize: '0.875rem', marginBottom: '0.5rem', color: 'var(--text-secondary)' }}>
              Start Date
            </label>
            <input
              type="date"
              value={startDate}
              onChange={(e) => setStartDate(e.target.value)}
              style={{
                width: '100%',
                padding: '0.5rem',
                backgroundColor: 'var(--bg-tertiary)',
                border: '1px solid var(--border)',
                borderRadius: '6px',
                color: 'var(--text-primary)',
              }}
            />
          </div>

          {/* End Date */}
          <div>
            <label style={{ display: 'block', fontSize: '0.875rem', marginBottom: '0.5rem', color: 'var(--text-secondary)' }}>
              End Date
            </label>
            <input
              type="date"
              value={endDate}
              onChange={(e) => setEndDate(e.target.value)}
              style={{
                width: '100%',
                padding: '0.5rem',
                backgroundColor: 'var(--bg-tertiary)',
                border: '1px solid var(--border)',
                borderRadius: '6px',
                color: 'var(--text-primary)',
              }}
            />
          </div>

          {/* Initial Capital */}
          <div>
            <label style={{ display: 'block', fontSize: '0.875rem', marginBottom: '0.5rem', color: 'var(--text-secondary)' }}>
              Initial Capital ($)
            </label>
            <input
              type="number"
              value={initialCapital}
              onChange={(e) => setInitialCapital(e.target.value)}
              style={{
                width: '100%',
                padding: '0.5rem',
                backgroundColor: 'var(--bg-tertiary)',
                border: '1px solid var(--border)',
                borderRadius: '6px',
                color: 'var(--text-primary)',
              }}
            />
          </div>

          {/* Run Button */}
          <div style={{ display: 'flex', alignItems: 'flex-end' }}>
            <button
              onClick={handleRunBacktest}
              disabled={running}
              className="btn btn-primary"
              style={{
                width: '100%',
                display: 'flex',
                alignItems: 'center',
                justifyContent: 'center',
                gap: '0.5rem',
              }}
            >
              <Play size={16} />
              {running ? 'Running...' : 'Run Backtest'}
            </button>
          </div>
        </div>
      </div>

      {/* Results Summary */}
      {selectedResult && (
        <>
          <div className="grid grid-cols-4" style={{ gridTemplateColumns: 'repeat(auto-fit, minmax(200px, 1fr))' }}>
            <div className="card">
              <div style={{ display: 'flex', alignItems: 'center', gap: '0.5rem', marginBottom: '0.5rem' }}>
                <TrendingUp size={20} color="var(--success)" />
                <span style={{ fontSize: '0.875rem', color: 'var(--text-secondary)' }}>Total Return</span>
              </div>
              <div style={{
                fontSize: '1.75rem',
                fontWeight: 600,
                color: selectedResult.totalReturn >= 0 ? 'var(--success)' : 'var(--danger)'
              }}>
                {selectedResult.totalReturn >= 0 ? '+' : ''}{selectedResult.totalReturn.toFixed(2)}%
              </div>
              <div style={{ fontSize: '0.875rem', color: 'var(--text-secondary)', marginTop: '0.25rem' }}>
                ${selectedResult.finalValue.toLocaleString(undefined, { minimumFractionDigits: 2, maximumFractionDigits: 2 })} final value
              </div>
            </div>

            <div className="card">
              <div style={{ display: 'flex', alignItems: 'center', gap: '0.5rem', marginBottom: '0.5rem' }}>
                <Activity size={20} color="var(--primary)" />
                <span style={{ fontSize: '0.875rem', color: 'var(--text-secondary)' }}>Sharpe Ratio</span>
              </div>
              <div style={{ fontSize: '1.75rem', fontWeight: 600 }}>
                {selectedResult.sharpeRatio.toFixed(2)}
              </div>
              <div style={{ fontSize: '0.875rem', color: 'var(--text-secondary)', marginTop: '0.25rem' }}>
                Risk-adjusted return
              </div>
            </div>

            <div className="card">
              <div style={{ display: 'flex', alignItems: 'center', gap: '0.5rem', marginBottom: '0.5rem' }}>
                <Calendar size={20} color="var(--warning)" />
                <span style={{ fontSize: '0.875rem', color: 'var(--text-secondary)' }}>Win Rate</span>
              </div>
              <div style={{ fontSize: '1.75rem', fontWeight: 600 }}>
                {selectedResult.winRate.toFixed(1)}%
              </div>
              <div style={{ fontSize: '0.875rem', color: 'var(--text-secondary)', marginTop: '0.25rem' }}>
                {selectedResult.totalTrades} total trades
              </div>
            </div>

            <div className="card">
              <div style={{ display: 'flex', alignItems: 'center', gap: '0.5rem', marginBottom: '0.5rem' }}>
                <TrendingUp size={20} color="var(--danger)" />
                <span style={{ fontSize: '0.875rem', color: 'var(--text-secondary)' }}>Max Drawdown</span>
              </div>
              <div style={{ fontSize: '1.75rem', fontWeight: 600, color: 'var(--danger)' }}>
                {selectedResult.maxDrawdown.toFixed(2)}%
              </div>
              <div style={{ fontSize: '0.875rem', color: 'var(--text-secondary)', marginTop: '0.25rem' }}>
                Maximum decline
              </div>
            </div>
          </div>

          {/* Equity Curve */}
          <div className="card">
            <div className="card-header">
              <h3 className="card-title">📈 Equity Curve</h3>
              <button className="btn btn-primary" style={{ display: 'flex', alignItems: 'center', gap: '0.5rem' }}>
                <Download size={16} />
                Export Report
              </button>
            </div>

            <ResponsiveContainer width="100%" height={300}>
              <LineChart data={selectedResult.equityCurve}>
                <CartesianGrid strokeDasharray="3 3" stroke="#475569" />
                <XAxis dataKey="date" stroke="#cbd5e1" />
                <YAxis stroke="#cbd5e1" />
                <Tooltip
                  contentStyle={{
                    backgroundColor: '#1e293b',
                    border: '1px solid #475569',
                    borderRadius: '6px',
                  }}
                />
                <Line type="monotone" dataKey="value" stroke="#3b82f6" strokeWidth={2} dot={false} />
              </LineChart>
            </ResponsiveContainer>
          </div>

          {/* Performance Metrics */}
          <div className="card">
            <div className="card-header">
              <h3 className="card-title">📊 Performance Metrics</h3>
            </div>

            <div style={{ display: 'grid', gridTemplateColumns: 'repeat(auto-fit, minmax(250px, 1fr))', gap: '1.5rem' }}>
              {performanceMetrics.map((metric, i) => (
                <div key={i} style={{
                  padding: '1rem',
                  backgroundColor: 'var(--bg-tertiary)',
                  borderRadius: '6px',
                  display: 'flex',
                  justifyContent: 'space-between',
                  alignItems: 'center',
                }}>
                  <span style={{ color: 'var(--text-secondary)' }}>{metric.name}</span>
                  <span style={{ fontSize: '1.25rem', fontWeight: 600 }}>{metric.value}</span>
                </div>
              ))}
            </div>

            <div style={{ marginTop: '1.5rem', padding: '1rem', backgroundColor: 'var(--bg-tertiary)', borderRadius: '6px' }}>
              <div style={{ display: 'grid', gridTemplateColumns: 'auto 1fr', gap: '0.5rem 1rem', fontSize: '0.875rem' }}>
                <span style={{ color: 'var(--text-secondary)' }}>Strategy:</span>
                <span style={{ fontWeight: 600 }}>{selectedResult.strategy}</span>

                <span style={{ color: 'var(--text-secondary)' }}>Symbol:</span>
                <span style={{ fontWeight: 600 }}>{selectedResult.symbol}</span>

                <span style={{ color: 'var(--text-secondary)' }}>Period:</span>
                <span style={{ fontWeight: 600 }}>{selectedResult.startDate} to {selectedResult.endDate}</span>

                <span style={{ color: 'var(--text-secondary)' }}>Initial Capital:</span>
                <span style={{ fontWeight: 600 }}>${selectedResult.initialCapital.toLocaleString()}</span>

                <span style={{ color: 'var(--text-secondary)' }}>Final Value:</span>
                <span style={{ fontWeight: 600, color: 'var(--success)' }}>
                  ${selectedResult.finalValue.toLocaleString(undefined, { minimumFractionDigits: 2, maximumFractionDigits: 2 })}
                </span>
              </div>
            </div>
          </div>
        </>
      )}

      {/* Recent Backtests */}
      <div className="card">
        <div className="card-header">
          <h3 className="card-title">📋 Recent Backtests</h3>
          <span className="badge badge-success">{results.length} results</span>
        </div>

        {results.length > 0 ? (
          <div style={{ overflowX: 'auto' }}>
            <table style={{ width: '100%', borderCollapse: 'collapse' }}>
              <thead>
                <tr style={{ borderBottom: '1px solid var(--border)' }}>
                  <th style={{ padding: '0.75rem', textAlign: 'left', fontSize: '0.875rem', color: 'var(--text-secondary)' }}>Strategy</th>
                  <th style={{ padding: '0.75rem', textAlign: 'left', fontSize: '0.875rem', color: 'var(--text-secondary)' }}>Symbol</th>
                  <th style={{ padding: '0.75rem', textAlign: 'left', fontSize: '0.875rem', color: 'var(--text-secondary)' }}>Period</th>
                  <th style={{ padding: '0.75rem', textAlign: 'right', fontSize: '0.875rem', color: 'var(--text-secondary)' }}>Return</th>
                  <th style={{ padding: '0.75rem', textAlign: 'right', fontSize: '0.875rem', color: 'var(--text-secondary)' }}>Sharpe</th>
                  <th style={{ padding: '0.75rem', textAlign: 'right', fontSize: '0.875rem', color: 'var(--text-secondary)' }}>Trades</th>
                  <th style={{ padding: '0.75rem', textAlign: 'center', fontSize: '0.875rem', color: 'var(--text-secondary)' }}>Status</th>
                  <th style={{ padding: '0.75rem', textAlign: 'center', fontSize: '0.875rem', color: 'var(--text-secondary)' }}>Actions</th>
                </tr>
              </thead>
              <tbody>
                {results.map((result) => (
                  <tr key={result.id} style={{ borderBottom: '1px solid var(--border)' }}>
                    <td style={{ padding: '0.75rem', fontWeight: 600 }}>{result.strategy}</td>
                    <td style={{ padding: '0.75rem' }}>{result.symbol}</td>
                    <td style={{ padding: '0.75rem', fontSize: '0.875rem' }}>
                      {result.startDate} - {result.endDate}
                    </td>
                    <td style={{
                      padding: '0.75rem',
                      textAlign: 'right',
                      fontWeight: 600,
                      color: result.totalReturn >= 0 ? 'var(--success)' : 'var(--danger)'
                    }}>
                      {result.totalReturn >= 0 ? '+' : ''}{result.totalReturn.toFixed(2)}%
                    </td>
                    <td style={{ padding: '0.75rem', textAlign: 'right' }}>
                      {result.sharpeRatio.toFixed(2)}
                    </td>
                    <td style={{ padding: '0.75rem', textAlign: 'right' }}>
                      {result.totalTrades}
                    </td>
                    <td style={{ padding: '0.75rem', textAlign: 'center' }}>
                      <span className={`badge badge-${result.status === 'completed' ? 'success' : result.status === 'running' ? 'warning' : 'danger'}`}>
                        {result.status}
                      </span>
                    </td>
                    <td style={{ padding: '0.75rem', textAlign: 'center' }}>
                      <button
                        onClick={() => setSelectedResult(result)}
                        className="btn btn-primary"
                        style={{ fontSize: '0.75rem', padding: '0.25rem 0.5rem' }}
                      >
                        View
                      </button>
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        ) : (
          <div style={{ textAlign: 'center', padding: '3rem', color: 'var(--text-secondary)' }}>
            <p>No backtests run yet. Configure and run your first backtest above.</p>
          </div>
        )}
      </div>
    </div>
  );
}
