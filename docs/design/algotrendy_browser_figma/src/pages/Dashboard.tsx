/**
 * Dashboard Page
 * Portfolio overview, P&L, and key metrics
 */

import { useEffect, useState } from 'react';
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '../components/ui/card';
import { TrendingUp, TrendingDown, Wallet, DollarSign, Activity } from 'lucide-react';
import api, { Portfolio, Position } from '../lib/api-client';
import { signalRClient, PositionUpdate } from '../lib/signalr-client';

export default function Dashboard() {
  const [portfolio, setPortfolio] = useState<Portfolio | null>(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  // Fetch portfolio data
  useEffect(() => {
    const fetchPortfolio = async () => {
      try {
        setLoading(true);
        const response = await api.portfolio.get();
        setPortfolio(response.data);
        setError(null);
      } catch (err) {
        console.error('Error fetching portfolio:', err);
        setError('Failed to load portfolio data. Please ensure the backend API is running.');
      } finally {
        setLoading(false);
      }
    };

    fetchPortfolio();
  }, []);

  // Subscribe to real-time position updates
  useEffect(() => {
    if (!signalRClient.isConnected) return;

    const handlePositionUpdate = (update: PositionUpdate) => {
      setPortfolio((prev) => {
        if (!prev) return prev;

        const updatedPositions = prev.positions.map((pos) =>
          pos.symbol === update.symbol
            ? {
                ...pos,
                currentPrice: update.currentPrice,
                unrealizedPnl: update.unrealizedPnl,
              }
            : pos
        );

        const totalUnrealizedPnl = updatedPositions.reduce(
          (sum, pos) => sum + pos.unrealizedPnl,
          0
        );

        return {
          ...prev,
          positions: updatedPositions,
          unrealizedPnl: totalUnrealizedPnl,
          totalValue: prev.cash + prev.equity + totalUnrealizedPnl,
        };
      });
    };

    signalRClient.onPositionUpdate(handlePositionUpdate);

    return () => {
      signalRClient.off('PositionUpdate', handlePositionUpdate);
    };
  }, [signalRClient.isConnected]);

  if (loading) {
    return (
      <div className="p-8">
        <div className="flex items-center justify-center h-96">
          <div className="text-center">
            <Activity className="w-12 h-12 text-blue-500 animate-spin mx-auto mb-4" />
            <p className="text-gray-400">Loading portfolio data...</p>
          </div>
        </div>
      </div>
    );
  }

  if (!portfolio) {
    return (
      <div className="p-8">
        <div className="text-center text-red-400">
          {error || 'Failed to load portfolio'}
        </div>
      </div>
    );
  }

  const isProfitable = portfolio.todayPnl >= 0;

  return (
    <div className="p-8">
      <div className="mb-8">
        <h1 className="text-3xl font-bold text-white mb-2">Portfolio Dashboard</h1>
        <p className="text-gray-400">Real-time overview of your trading portfolio</p>
      </div>

      {/* Key Metrics */}
      <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-4 gap-6 mb-8">
        {/* Total Value */}
        <Card className="bg-slate-900 border-slate-800">
          <CardHeader className="flex flex-row items-center justify-between pb-2">
            <CardTitle className="text-sm font-medium text-gray-400">
              Total Portfolio Value
            </CardTitle>
            <Wallet className="h-4 w-4 text-blue-500" />
          </CardHeader>
          <CardContent>
            <div className="text-2xl font-bold text-white">
              ${portfolio.totalValue.toLocaleString('en-US', { minimumFractionDigits: 2 })}
            </div>
            <p className="text-xs text-gray-400 mt-1">
              Cash: ${portfolio.cash.toLocaleString('en-US', { minimumFractionDigits: 2 })}
            </p>
          </CardContent>
        </Card>

        {/* Today's P&L */}
        <Card className="bg-slate-900 border-slate-800">
          <CardHeader className="flex flex-row items-center justify-between pb-2">
            <CardTitle className="text-sm font-medium text-gray-400">
              Today's P&L
            </CardTitle>
            {isProfitable ? (
              <TrendingUp className="h-4 w-4 text-green-500" />
            ) : (
              <TrendingDown className="h-4 w-4 text-red-500" />
            )}
          </CardHeader>
          <CardContent>
            <div className={`text-2xl font-bold ${isProfitable ? 'text-green-500' : 'text-red-500'}`}>
              {isProfitable ? '+' : ''}
              ${portfolio.todayPnl.toLocaleString('en-US', { minimumFractionDigits: 2 })}
            </div>
            <p className={`text-xs ${isProfitable ? 'text-green-400' : 'text-red-400'} mt-1`}>
              {isProfitable ? '+' : ''}
              {portfolio.todayPnlPercent.toFixed(2)}%
            </p>
          </CardContent>
        </Card>

        {/* Unrealized P&L */}
        <Card className="bg-slate-900 border-slate-800">
          <CardHeader className="flex flex-row items-center justify-between pb-2">
            <CardTitle className="text-sm font-medium text-gray-400">
              Unrealized P&L
            </CardTitle>
            <DollarSign className="h-4 w-4 text-yellow-500" />
          </CardHeader>
          <CardContent>
            <div className={`text-2xl font-bold ${portfolio.unrealizedPnl >= 0 ? 'text-green-500' : 'text-red-500'}`}>
              {portfolio.unrealizedPnl >= 0 ? '+' : ''}
              ${portfolio.unrealizedPnl.toLocaleString('en-US', { minimumFractionDigits: 2 })}
            </div>
            <p className="text-xs text-gray-400 mt-1">Open positions</p>
          </CardContent>
        </Card>

        {/* Realized P&L */}
        <Card className="bg-slate-900 border-slate-800">
          <CardHeader className="flex flex-row items-center justify-between pb-2">
            <CardTitle className="text-sm font-medium text-gray-400">
              Realized P&L
            </CardTitle>
            <Activity className="h-4 w-4 text-blue-500" />
          </CardHeader>
          <CardContent>
            <div className={`text-2xl font-bold ${portfolio.realizedPnl >= 0 ? 'text-green-500' : 'text-red-500'}`}>
              {portfolio.realizedPnl >= 0 ? '+' : ''}
              ${portfolio.realizedPnl.toLocaleString('en-US', { minimumFractionDigits: 2 })}
            </div>
            <p className="text-xs text-gray-400 mt-1">Closed positions</p>
          </CardContent>
        </Card>
      </div>

      {/* Open Positions */}
      <Card className="bg-slate-900 border-slate-800">
        <CardHeader>
          <CardTitle className="text-white">Open Positions</CardTitle>
          <CardDescription>Active trading positions</CardDescription>
        </CardHeader>
        <CardContent>
          {portfolio.positions.length === 0 ? (
            <div className="text-center py-8 text-gray-400">
              No open positions
            </div>
          ) : (
            <div className="overflow-x-auto">
              <table className="w-full">
                <thead>
                  <tr className="border-b border-slate-800">
                    <th className="text-left py-3 px-4 text-sm font-medium text-gray-400">Symbol</th>
                    <th className="text-left py-3 px-4 text-sm font-medium text-gray-400">Side</th>
                    <th className="text-right py-3 px-4 text-sm font-medium text-gray-400">Quantity</th>
                    <th className="text-right py-3 px-4 text-sm font-medium text-gray-400">Entry</th>
                    <th className="text-right py-3 px-4 text-sm font-medium text-gray-400">Current</th>
                    <th className="text-right py-3 px-4 text-sm font-medium text-gray-400">P&L</th>
                    <th className="text-center py-3 px-4 text-sm font-medium text-gray-400">Leverage</th>
                  </tr>
                </thead>
                <tbody>
                  {portfolio.positions.map((position) => (
                    <tr key={position.id} className="border-b border-slate-800 hover:bg-slate-800/50">
                      <td className="py-3 px-4 text-white font-medium">{position.symbol}</td>
                      <td className="py-3 px-4">
                        <span
                          className={`px-2 py-1 rounded text-xs font-medium ${
                            position.side === 'Long'
                              ? 'bg-green-500/20 text-green-400'
                              : 'bg-red-500/20 text-red-400'
                          }`}
                        >
                          {position.side}
                        </span>
                      </td>
                      <td className="py-3 px-4 text-right text-gray-300">{position.quantity}</td>
                      <td className="py-3 px-4 text-right text-gray-300">
                        ${position.entryPrice.toLocaleString()}
                      </td>
                      <td className="py-3 px-4 text-right text-white font-medium">
                        ${position.currentPrice.toLocaleString()}
                      </td>
                      <td className={`py-3 px-4 text-right font-medium ${position.unrealizedPnl >= 0 ? 'text-green-500' : 'text-red-500'}`}>
                        {position.unrealizedPnl >= 0 ? '+' : ''}
                        ${position.unrealizedPnl.toLocaleString('en-US', { minimumFractionDigits: 2 })}
                      </td>
                      <td className="py-3 px-4 text-center text-gray-300">{position.leverage}x</td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </div>
          )}
        </CardContent>
      </Card>
    </div>
  );
}
