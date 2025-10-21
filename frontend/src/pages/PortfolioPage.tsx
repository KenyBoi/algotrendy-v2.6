import { useState, useEffect } from 'react';
import { Wallet, TrendingUp, TrendingDown, PieChart as PieChartIcon, BarChart3 } from 'lucide-react';
import { PieChart, Pie, Cell, BarChart, Bar, XAxis, YAxis, CartesianGrid, Tooltip, Legend, ResponsiveContainer } from 'recharts';
import { tradingApi } from '../lib/tradingApi';

interface Position {
  symbol: string;
  quantity: number;
  avgPrice: number;
  currentPrice: number;
  value: number;
  pnl: number;
  pnlPercent: number;
  allocation: number;
}

interface PortfolioMetrics {
  totalValue: number;
  totalPnL: number;
  totalPnLPercent: number;
  totalInvested: number;
  cash: number;
}

export default function PortfolioPage() {
  const [positions, setPositions] = useState<Position[]>([]);
  const [metrics, setMetrics] = useState<PortfolioMetrics>({
    totalValue: 0,
    totalPnL: 0,
    totalPnLPercent: 0,
    totalInvested: 0,
    cash: 0,
  });
  const [loading, setLoading] = useState(true);

  useEffect(() => {
    loadPortfolio();
    const interval = setInterval(loadPortfolio, 10000);
    return () => clearInterval(interval);
  }, []);

  const loadPortfolio = async () => {
    try {
      const [portfolioData, analyticsData] = await Promise.all([
        tradingApi.getPortfolio(),
        tradingApi.getPortfolioAnalytics(),
      ]);

      // Transform API response to match our interface
      const positionsData: Position[] = portfolioData.positions || [];

      setPositions(positionsData);
      setMetrics({
        totalValue: analyticsData.totalValue || 0,
        totalPnL: analyticsData.totalPnL || 0,
        totalPnLPercent: analyticsData.totalPnLPercent || 0,
        totalInvested: analyticsData.totalInvested || 0,
        cash: analyticsData.cash || 0,
      });
      setLoading(false);
    } catch (error) {
      console.error('Error loading portfolio:', error);
      setLoading(false);
    }
  };

  const COLORS = ['#3b82f6', '#10b981', '#f59e0b', '#8b5cf6', '#ef4444', '#06b6d4'];

  const allocationData = positions.map((p, i) => ({
    name: p.symbol,
    value: p.allocation,
    fill: COLORS[i % COLORS.length],
  }));

  if (loading) {
    return (
      <div style={{ textAlign: 'center', padding: '4rem' }}>
        <h2>Loading Portfolio...</h2>
      </div>
    );
  }

  return (
    <div className="dashboard">
      {/* Portfolio Summary */}
      <div className="grid grid-cols-4" style={{ gridTemplateColumns: 'repeat(auto-fit, minmax(200px, 1fr))' }}>
        <div className="card">
          <div style={{ display: 'flex', alignItems: 'center', gap: '0.5rem', marginBottom: '0.5rem' }}>
            <Wallet size={20} color="var(--primary)" />
            <span style={{ fontSize: '0.875rem', color: 'var(--text-secondary)' }}>Total Value</span>
          </div>
          <div style={{ fontSize: '1.75rem', fontWeight: 600 }}>
            ${metrics.totalValue.toLocaleString(undefined, { minimumFractionDigits: 2, maximumFractionDigits: 2 })}
          </div>
        </div>

        <div className="card">
          <div style={{ display: 'flex', alignItems: 'center', gap: '0.5rem', marginBottom: '0.5rem' }}>
            {metrics.totalPnL >= 0 ? (
              <TrendingUp size={20} color="var(--success)" />
            ) : (
              <TrendingDown size={20} color="var(--danger)" />
            )}
            <span style={{ fontSize: '0.875rem', color: 'var(--text-secondary)' }}>Total P&L</span>
          </div>
          <div style={{
            fontSize: '1.75rem',
            fontWeight: 600,
            color: metrics.totalPnL >= 0 ? 'var(--success)' : 'var(--danger)'
          }}>
            {metrics.totalPnL >= 0 ? '+' : ''}${metrics.totalPnL.toLocaleString(undefined, { minimumFractionDigits: 2, maximumFractionDigits: 2 })}
          </div>
          <div style={{
            fontSize: '0.875rem',
            color: metrics.totalPnL >= 0 ? 'var(--success)' : 'var(--danger)',
            marginTop: '0.25rem'
          }}>
            {metrics.totalPnLPercent >= 0 ? '+' : ''}{metrics.totalPnLPercent.toFixed(2)}%
          </div>
        </div>

        <div className="card">
          <div style={{ display: 'flex', alignItems: 'center', gap: '0.5rem', marginBottom: '0.5rem' }}>
            <BarChart3 size={20} color="var(--warning)" />
            <span style={{ fontSize: '0.875rem', color: 'var(--text-secondary)' }}>Invested</span>
          </div>
          <div style={{ fontSize: '1.75rem', fontWeight: 600 }}>
            ${metrics.totalInvested.toLocaleString(undefined, { minimumFractionDigits: 2, maximumFractionDigits: 2 })}
          </div>
        </div>

        <div className="card">
          <div style={{ display: 'flex', alignItems: 'center', gap: '0.5rem', marginBottom: '0.5rem' }}>
            <PieChartIcon size={20} color="var(--success)" />
            <span style={{ fontSize: '0.875rem', color: 'var(--text-secondary)' }}>Cash Available</span>
          </div>
          <div style={{ fontSize: '1.75rem', fontWeight: 600 }}>
            ${metrics.cash.toLocaleString(undefined, { minimumFractionDigits: 2, maximumFractionDigits: 2 })}
          </div>
        </div>
      </div>

      <div className="grid grid-cols-2">
        {/* Allocation Chart */}
        <div className="card">
          <div className="card-header">
            <h3 className="card-title">📊 Portfolio Allocation</h3>
          </div>

          <ResponsiveContainer width="100%" height={300}>
            <PieChart>
              <Pie
                data={allocationData}
                cx="50%"
                cy="50%"
                labelLine={false}
                label={({ name, value }) => `${name}: ${value.toFixed(1)}%`}
                outerRadius={80}
                fill="#8884d8"
                dataKey="value"
              >
                {allocationData.map((entry, index) => (
                  <Cell key={`cell-${index}`} fill={entry.fill} />
                ))}
              </Pie>
              <Tooltip
                contentStyle={{
                  backgroundColor: '#1e293b',
                  border: '1px solid #475569',
                  borderRadius: '6px',
                }}
              />
            </PieChart>
          </ResponsiveContainer>
        </div>

        {/* Performance Chart */}
        <div className="card">
          <div className="card-header">
            <h3 className="card-title">📈 P&L by Asset</h3>
          </div>

          <ResponsiveContainer width="100%" height={300}>
            <BarChart data={positions}>
              <CartesianGrid strokeDasharray="3 3" stroke="#475569" />
              <XAxis dataKey="symbol" stroke="#cbd5e1" />
              <YAxis stroke="#cbd5e1" />
              <Tooltip
                contentStyle={{
                  backgroundColor: '#1e293b',
                  border: '1px solid #475569',
                  borderRadius: '6px',
                }}
              />
              <Legend />
              <Bar dataKey="pnl" fill="#3b82f6" name="P&L ($)" />
            </BarChart>
          </ResponsiveContainer>
        </div>
      </div>

      {/* Positions Table */}
      <div className="card">
        <div className="card-header">
          <h3 className="card-title">💼 Active Positions</h3>
          <span className="badge badge-success">{positions.length} positions</span>
        </div>

        <div style={{ overflowX: 'auto' }}>
          <table style={{ width: '100%', borderCollapse: 'collapse' }}>
            <thead>
              <tr style={{ borderBottom: '1px solid var(--border)' }}>
                <th style={{ padding: '0.75rem', textAlign: 'left', fontSize: '0.875rem', color: 'var(--text-secondary)' }}>Symbol</th>
                <th style={{ padding: '0.75rem', textAlign: 'right', fontSize: '0.875rem', color: 'var(--text-secondary)' }}>Quantity</th>
                <th style={{ padding: '0.75rem', textAlign: 'right', fontSize: '0.875rem', color: 'var(--text-secondary)' }}>Avg Price</th>
                <th style={{ padding: '0.75rem', textAlign: 'right', fontSize: '0.875rem', color: 'var(--text-secondary)' }}>Current Price</th>
                <th style={{ padding: '0.75rem', textAlign: 'right', fontSize: '0.875rem', color: 'var(--text-secondary)' }}>Value</th>
                <th style={{ padding: '0.75rem', textAlign: 'right', fontSize: '0.875rem', color: 'var(--text-secondary)' }}>P&L</th>
                <th style={{ padding: '0.75rem', textAlign: 'right', fontSize: '0.875rem', color: 'var(--text-secondary)' }}>P&L %</th>
                <th style={{ padding: '0.75rem', textAlign: 'right', fontSize: '0.875rem', color: 'var(--text-secondary)' }}>Allocation</th>
              </tr>
            </thead>
            <tbody>
              {positions.map((position) => (
                <tr key={position.symbol} style={{ borderBottom: '1px solid var(--border)' }}>
                  <td style={{ padding: '0.75rem', fontWeight: 600 }}>{position.symbol}</td>
                  <td style={{ padding: '0.75rem', textAlign: 'right' }}>{position.quantity.toLocaleString()}</td>
                  <td style={{ padding: '0.75rem', textAlign: 'right' }}>
                    ${position.avgPrice.toLocaleString(undefined, { minimumFractionDigits: 2, maximumFractionDigits: 2 })}
                  </td>
                  <td style={{ padding: '0.75rem', textAlign: 'right' }}>
                    ${position.currentPrice.toLocaleString(undefined, { minimumFractionDigits: 2, maximumFractionDigits: 2 })}
                  </td>
                  <td style={{ padding: '0.75rem', textAlign: 'right', fontWeight: 600 }}>
                    ${position.value.toLocaleString(undefined, { minimumFractionDigits: 2, maximumFractionDigits: 2 })}
                  </td>
                  <td style={{
                    padding: '0.75rem',
                    textAlign: 'right',
                    color: position.pnl >= 0 ? 'var(--success)' : 'var(--danger)',
                    fontWeight: 600
                  }}>
                    {position.pnl >= 0 ? '+' : ''}${position.pnl.toLocaleString(undefined, { minimumFractionDigits: 2, maximumFractionDigits: 2 })}
                  </td>
                  <td style={{
                    padding: '0.75rem',
                    textAlign: 'right',
                    color: position.pnlPercent >= 0 ? 'var(--success)' : 'var(--danger)',
                    fontWeight: 600
                  }}>
                    {position.pnlPercent >= 0 ? '+' : ''}{position.pnlPercent.toFixed(2)}%
                  </td>
                  <td style={{ padding: '0.75rem', textAlign: 'right' }}>{position.allocation.toFixed(1)}%</td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      </div>
    </div>
  );
}
