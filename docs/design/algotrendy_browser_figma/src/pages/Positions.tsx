/**
 * Positions Page
 * View and manage active trading positions
 */

import { useEffect, useState } from 'react';
import { Card, CardContent, CardHeader, CardTitle } from '../components/ui/card';
import { Button } from '../components/ui/button';
import api, { Position } from '../lib/api-client';
import { signalRClient, PositionUpdate } from '../lib/signalr-client';
import { X } from 'lucide-react';
import { useToast } from '../components/ui/use-toast';

export default function Positions() {
  const [positions, setPositions] = useState<Position[]>([]);
  const [loading, setLoading] = useState(true);
  const { toast } = useToast();

  useEffect(() => {
    fetchPositions();
  }, []);

  const fetchPositions = async () => {
    try {
      setLoading(true);
      const response = await api.positions.getAll();
      setPositions(response.data);
    } catch (error) {
      console.error('Error fetching positions:', error);
      // Mock data
      setPositions([
        {
          id: '1',
          symbol: 'BTC-USD',
          side: 'Long',
          quantity: 0.5,
          entryPrice: 42000,
          currentPrice: 45250,
          unrealizedPnl: 1625,
          realizedPnl: 0,
          leverage: 2,
          openedAt: new Date().toISOString(),
        },
      ]);
    } finally {
      setLoading(false);
    }
  };

  // Real-time updates
  useEffect(() => {
    if (!signalRClient.isConnected) return;

    const handleUpdate = (update: PositionUpdate) => {
      setPositions((prev) =>
        prev.map((pos) =>
          pos.symbol === update.symbol
            ? { ...pos, currentPrice: update.currentPrice, unrealizedPnl: update.unrealizedPnl }
            : pos
        )
      );
    };

    signalRClient.onPositionUpdate(handleUpdate);
    return () => signalRClient.off('PositionUpdate', handleUpdate);
  }, [signalRClient.isConnected]);

  const handleClosePosition = async (symbol: string) => {
    try {
      await api.positions.close(symbol);
      toast({ title: 'Position Closed', description: `Closed position for ${symbol}` });
      fetchPositions();
    } catch (error) {
      toast({ title: 'Error', description: 'Failed to close position', variant: 'destructive' });
    }
  };

  return (
    <div className="p-8">
      <div className="mb-8">
        <h1 className="text-3xl font-bold text-white mb-2">Open Positions</h1>
        <p className="text-gray-400">Active trading positions with real-time P&L</p>
      </div>

      <Card className="bg-slate-900 border-slate-800">
        <CardHeader>
          <CardTitle className="text-white">Positions</CardTitle>
        </CardHeader>
        <CardContent>
          {loading ? (
            <div className="text-center py-8 text-gray-400">Loading positions...</div>
          ) : positions.length === 0 ? (
            <div className="text-center py-8 text-gray-400">No open positions</div>
          ) : (
            <div className="overflow-x-auto">
              <table className="w-full">
                <thead>
                  <tr className="border-b border-slate-800">
                    <th className="text-left py-3 px-4 text-sm font-medium text-gray-400">Symbol</th>
                    <th className="text-left py-3 px-4 text-sm font-medium text-gray-400">Side</th>
                    <th className="text-right py-3 px-4 text-sm font-medium text-gray-400">Quantity</th>
                    <th className="text-right py-3 px-4 text-sm font-medium text-gray-400">Entry Price</th>
                    <th className="text-right py-3 px-4 text-sm font-medium text-gray-400">Current Price</th>
                    <th className="text-right py-3 px-4 text-sm font-medium text-gray-400">P&L</th>
                    <th className="text-center py-3 px-4 text-sm font-medium text-gray-400">Leverage</th>
                    <th className="text-center py-3 px-4 text-sm font-medium text-gray-400">Action</th>
                  </tr>
                </thead>
                <tbody>
                  {positions.map((position) => (
                    <tr key={position.id} className="border-b border-slate-800 hover:bg-slate-800/50">
                      <td className="py-3 px-4 text-white font-medium">{position.symbol}</td>
                      <td className="py-3 px-4">
                        <span className={`px-2 py-1 rounded text-xs font-medium ${
                          position.side === 'Long' ? 'bg-green-500/20 text-green-400' : 'bg-red-500/20 text-red-400'
                        }`}>
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
                      <td className={`py-3 px-4 text-right font-bold ${position.unrealizedPnl >= 0 ? 'text-green-500' : 'text-red-500'}`}>
                        {position.unrealizedPnl >= 0 ? '+' : ''}
                        ${position.unrealizedPnl.toLocaleString('en-US', { minimumFractionDigits: 2 })}
                      </td>
                      <td className="py-3 px-4 text-center text-gray-300">{position.leverage}x</td>
                      <td className="py-3 px-4 text-center">
                        <Button
                          variant="outline"
                          size="sm"
                          onClick={() => handleClosePosition(position.symbol)}
                          className="text-red-400 hover:text-red-300"
                        >
                          <X className="w-4 h-4 mr-1" />
                          Close
                        </Button>
                      </td>
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
