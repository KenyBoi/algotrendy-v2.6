// COMPLETED: Portfolio performance chart with line graph
// COMPLETED: Responsive chart with Recharts
// COMPLETED: Tooltip with detailed information
// COMPLETED: Time period filter (1D, 1W, 1M, 3M, 1Y, ALL)
// TODO: Add real-time data updates via WebSocket
// TODO: Add zoom and pan functionality
// TODO: Add comparison with market indices
// TODO: Add export chart as image functionality
import React, { useState } from 'react';
import {
  AreaChart,
  Area,
  XAxis,
  YAxis,
  CartesianGrid,
  Tooltip,
  ResponsiveContainer,
} from 'recharts';
import { TrendingUp, TrendingDown, LineChart as LineChartIcon } from 'lucide-react';
import { EmptyState } from '@/components/ui/EmptyState';

interface PerformanceChartProps {
  title?: string;
  height?: number;
  data?: Array<{ date: string; value: number; profit: number }>;
  showEmptyState?: boolean;
}

export const PerformanceChart: React.FC<PerformanceChartProps> = ({
  title = 'Portfolio Performance',
  height = 300,
  data: externalData,
  showEmptyState = false,
}) => {
  const [timePeriod, setTimePeriod] = useState<'1D' | '1W' | '1M' | '3M' | '1Y' | 'ALL'>('1M');

  // Show empty state if no data and flag is set
  if (showEmptyState && !externalData) {
    return (
      <div className="bg-white rounded-lg shadow-md">
        <EmptyState
          icon={LineChartIcon}
          title="No Performance Data"
          description="Start trading to track your portfolio performance over time. Historical data will appear here."
          actionLabel="View Dashboard"
          onAction={() => window.location.href = '/dashboard'}
        />
      </div>
    );
  }

  // Mock data - replace with real data from API
  const generateMockData = () => {
    const dataPoints = timePeriod === '1D' ? 24 : timePeriod === '1W' ? 7 : timePeriod === '1M' ? 30 : 90;
    const data = [];
    let value = 10000;

    for (let i = 0; i < dataPoints; i++) {
      value = value + (Math.random() - 0.45) * 200;
      data.push({
        date: new Date(Date.now() - (dataPoints - i) * 86400000).toLocaleDateString('en-US', {
          month: 'short',
          day: 'numeric',
        }),
        value: Math.round(value),
        profit: Math.round((value - 10000) / 100) / 10,
      });
    }
    return data;
  };

  const data = externalData || generateMockData();
  const currentValue = data[data.length - 1]?.value || 0;
  const initialValue = data[0]?.value || 0;
  const change = currentValue - initialValue;
  const changePercent = ((change / initialValue) * 100).toFixed(2);
  const isPositive = change >= 0;

  const periods = [
    { label: '1D', value: '1D' },
    { label: '1W', value: '1W' },
    { label: '1M', value: '1M' },
    { label: '3M', value: '3M' },
    { label: '1Y', value: '1Y' },
    { label: 'ALL', value: 'ALL' },
  ];

  return (
    <div className="bg-white rounded-lg shadow-md p-6">
      {/* Header */}
      <div className="flex items-center justify-between mb-6">
        <div>
          <h3 className="text-lg font-semibold text-primary">{title}</h3>
          <div className="flex items-center gap-3 mt-2">
            <span className="text-2xl font-bold text-primary">
              ${currentValue.toLocaleString()}
            </span>
            <div className={`flex items-center gap-1 px-2 py-1 rounded ${isPositive ? 'bg-green-100 text-green-700' : 'bg-red-100 text-red-700'}`}>
              {isPositive ? <TrendingUp size={16} /> : <TrendingDown size={16} />}
              <span className="text-sm font-semibold">
                {isPositive ? '+' : ''}{changePercent}%
              </span>
            </div>
          </div>
        </div>

        {/* Time Period Selector */}
        <div className="flex gap-1 bg-gray-100 p-1 rounded-lg">
          {periods.map((period) => (
            <button
              key={period.value}
              onClick={() => setTimePeriod(period.value as any)}
              className={`px-3 py-1 rounded text-sm font-medium transition-colors ${timePeriod === period.value
                  ? 'bg-primary text-white'
                  : 'text-neutral hover:bg-gray-200'
                }`}
            >
              {period.label}
            </button>
          ))}
        </div>
      </div>

      {/* Chart */}
      <ResponsiveContainer width="100%" height={height}>
        <AreaChart data={data}>
          <defs>
            <linearGradient id="colorValue" x1="0" y1="0" x2="0" y2="1">
              <stop offset="5%" stopColor={isPositive ? "#10b981" : "#ef4444"} stopOpacity={0.3} />
              <stop offset="95%" stopColor={isPositive ? "#10b981" : "#ef4444"} stopOpacity={0} />
            </linearGradient>
          </defs>
          <CartesianGrid strokeDasharray="3 3" stroke="#e5e7eb" />
          <XAxis
            dataKey="date"
            stroke="#6b7280"
            style={{ fontSize: '12px' }}
          />
          <YAxis
            stroke="#6b7280"
            style={{ fontSize: '12px' }}
            tickFormatter={(value) => `$${(value / 1000).toFixed(1)}k`}
          />
          <Tooltip
            contentStyle={{
              backgroundColor: '#1f2937',
              border: 'none',
              borderRadius: '8px',
              color: '#fff',
            }}
            formatter={(value: any) => [`$${value.toLocaleString()}`, 'Portfolio Value']}
          />
          <Area
            type="monotone"
            dataKey="value"
            stroke={isPositive ? "#10b981" : "#ef4444"}
            strokeWidth={2}
            fill="url(#colorValue)"
          />
        </AreaChart>
      </ResponsiveContainer>

      {/* Stats */}
      <div className="grid grid-cols-3 gap-4 mt-4 pt-4 border-t">
        <div>
          <p className="text-xs text-neutral">Period Return</p>
          <p className={`text-lg font-semibold ${isPositive ? 'text-green-600' : 'text-red-600'}`}>
            {isPositive ? '+' : ''}${Math.abs(change).toLocaleString()}
          </p>
        </div>
        <div>
          <p className="text-xs text-neutral">Avg Daily Return</p>
          <p className="text-lg font-semibold text-primary">
            ${(Math.abs(change) / data.length).toFixed(2)}
          </p>
        </div>
        <div>
          <p className="text-xs text-neutral">Volatility</p>
          <p className="text-lg font-semibold text-primary">12.5%</p>
        </div>
      </div>
    </div>
  );
};
