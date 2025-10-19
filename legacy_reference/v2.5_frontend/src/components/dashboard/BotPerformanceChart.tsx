// COMPLETED: Bot performance comparison bar chart
// COMPLETED: Multi-bot performance visualization
// COMPLETED: Responsive chart with tooltips
// COMPLETED: Color-coded performance indicators
// TODO: Add interactive bot selection/filtering
// TODO: Add drill-down to individual bot details
// TODO: Add historical performance trends
// TODO: Add win rate vs profit scatter plot
import React from 'react';
import {
  BarChart,
  Bar,
  XAxis,
  YAxis,
  CartesianGrid,
  Tooltip,
  Legend,
  ResponsiveContainer,
  Cell,
} from 'recharts';
import { Bot, TrendingUp } from 'lucide-react';
import { EmptyState } from '@/components/ui/EmptyState';

interface BotPerformanceChartProps {
  bots?: Array<{
    name: string;
    profit: number;
    trades: number;
    win_rate: number;
    status: string;
  }>;
  height?: number;
  showEmptyState?: boolean;
}

export const BotPerformanceChart: React.FC<BotPerformanceChartProps> = ({
  bots = [],
  height = 300,
  showEmptyState = false,
}) => {
  // Show empty state if no bots and flag is set
  if (showEmptyState && bots.length === 0) {
    return (
      <div className="bg-white rounded-lg shadow-md">
        <EmptyState
          icon={Bot}
          title="No Trading Bots"
          description="Configure and start your trading bots to see performance comparison here."
          actionLabel="Configure Bots"
          onAction={() => window.location.href = '/settings?tab=api'}
        />
      </div>
    );
  }

  // Mock data if no bots provided
  const mockData = bots.length > 0 ? bots : [
    { name: 'Momentum Bot', profit: 1247.80, trades: 45, win_rate: 72.3, status: 'online' },
    { name: 'RSI Bot', profit: 892.40, trades: 38, win_rate: 68.9, status: 'online' },
    { name: 'MACD Bot', profit: -156.20, trades: 21, win_rate: 45.2, status: 'offline' },
    { name: 'Scalper Bot', profit: 643.50, trades: 112, win_rate: 61.8, status: 'online' },
    { name: 'Arbitrage Bot', profit: 423.10, trades: 67, win_rate: 58.2, status: 'online' },
  ];

  const chartData = mockData.map(bot => ({
    name: bot.name.split(' ')[0], // Shortened name for chart
    fullName: bot.name,
    profit: bot.profit,
    winRate: bot.win_rate,
    trades: bot.trades,
    status: bot.status,
  }));

  const totalProfit = chartData.reduce((sum, bot) => sum + bot.profit, 0);
  const avgWinRate = chartData.reduce((sum, bot) => sum + bot.winRate, 0) / chartData.length;

  const getBarColor = (profit: number) => {
    if (profit > 1000) return '#10b981'; // Green
    if (profit > 500) return '#3b82f6'; // Blue
    if (profit > 0) return '#f59e0b'; // Yellow
    return '#ef4444'; // Red
  };

  return (
    <div className="bg-white rounded-lg shadow-md p-6">
      {/* Header */}
      <div className="flex items-center justify-between mb-6">
        <div>
          <div className="flex items-center gap-2">
            <Bot size={20} className="text-primary" />
            <h3 className="text-lg font-semibold text-primary">Bot Performance Comparison</h3>
          </div>
          <p className="text-sm text-neutral mt-1">Profit by trading bot (USD)</p>
        </div>

        <div className="flex gap-4">
          <div className="text-right">
            <p className="text-xs text-neutral">Total Profit</p>
            <p className={`text-xl font-bold ${totalProfit >= 0 ? 'text-green-600' : 'text-red-600'}`}>
              ${totalProfit.toLocaleString()}
            </p>
          </div>
          <div className="text-right">
            <p className="text-xs text-neutral">Avg Win Rate</p>
            <p className="text-xl font-bold text-primary">{avgWinRate.toFixed(1)}%</p>
          </div>
        </div>
      </div>

      {/* Chart */}
      <ResponsiveContainer width="100%" height={height}>
        <BarChart data={chartData}>
          <CartesianGrid strokeDasharray="3 3" stroke="#e5e7eb" />
          <XAxis
            dataKey="name"
            stroke="#6b7280"
            style={{ fontSize: '12px' }}
          />
          <YAxis
            stroke="#6b7280"
            style={{ fontSize: '12px' }}
            tickFormatter={(value) => `$${value}`}
          />
          <Tooltip
            contentStyle={{
              backgroundColor: '#1f2937',
              border: 'none',
              borderRadius: '8px',
              color: '#fff',
            }}
            formatter={(value: any, name: string, props: any) => {
              if (name === 'profit') {
                return [
                  <div key="tooltip" className="space-y-1">
                    <div className="font-semibold">{props.payload.fullName}</div>
                    <div>Profit: ${value.toLocaleString()}</div>
                    <div>Win Rate: {props.payload.winRate}%</div>
                    <div>Trades: {props.payload.trades}</div>
                    <div className="flex items-center gap-1">
                      Status:
                      <span className={props.payload.status === 'online' ? 'text-green-400' : 'text-red-400'}>
                        {props.payload.status}
                      </span>
                    </div>
                  </div>,
                  '',
                ];
              }
              return [value, name];
            }}
          />
          <Legend />
          <Bar dataKey="profit" fill="#3b82f6" radius={[8, 8, 0, 0]}>
            {chartData.map((entry, index) => (
              <Cell key={`cell-${index}`} fill={getBarColor(entry.profit)} />
            ))}
          </Bar>
        </BarChart>
      </ResponsiveContainer>

      {/* Performance Summary */}
      <div className="mt-4 pt-4 border-t">
        <div className="flex items-center gap-2 mb-3">
          <TrendingUp size={16} className="text-green-600" />
          <span className="text-sm font-medium text-primary">Top Performers</span>
        </div>
        <div className="grid grid-cols-3 gap-4">
          {chartData
            .sort((a, b) => b.profit - a.profit)
            .slice(0, 3)
            .map((bot, index) => (
              <div key={index} className="bg-gray-50 rounded-lg p-3">
                <div className="flex items-center gap-2 mb-1">
                  <span className="text-2xl">#{index + 1}</span>
                  <div className={`w-2 h-2 rounded-full ${bot.status === 'online' ? 'bg-green-500' : 'bg-red-500'}`} />
                </div>
                <p className="font-semibold text-sm text-primary truncate">{bot.fullName}</p>
                <p className="text-green-600 font-bold">${bot.profit.toLocaleString()}</p>
                <p className="text-xs text-neutral">{bot.winRate}% win rate</p>
              </div>
            ))}
        </div>
      </div>
    </div>
  );
};
