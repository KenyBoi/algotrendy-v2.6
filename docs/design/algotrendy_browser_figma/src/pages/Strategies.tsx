/**
 * Strategies Page
 * Backtest trading strategies
 */

import { useState } from 'react';
import { Card, CardContent, CardHeader, CardTitle } from '../components/ui/card';
import { Button } from '../components/ui/button';
import { Input } from '../components/ui/input';
import { Label } from '../components/ui/label';
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from '../components/ui/select';
import api, { BacktestRequest, BacktestResult } from '../lib/api-client';
import { TrendingUp, TrendingDown, Activity } from 'lucide-react';
import { useToast } from '../components/ui/use-toast';

export default function Strategies() {
  const [formData, setFormData] = useState<BacktestRequest>({
    strategyName: 'MomentumStrategy',
    symbol: 'BTC-USD',
    startDate: '2024-01-01',
    endDate: '2024-12-31',
    initialCapital: 10000,
    parameters: {},
  });
  const [result, setResult] = useState<BacktestResult | null>(null);
  const [loading, setLoading] = useState(false);
  const { toast } = useToast();

  const handleRunBacktest = async (e: React.FormEvent) => {
    e.preventDefault();
    try {
      setLoading(true);
      const response = await api.backtest.run(formData);
      const resultResponse = await api.backtest.getResults(response.data.id);
      setResult(resultResponse.data);
      toast({ title: 'Backtest Complete', description: 'Results are ready' });
    } catch (error) {
      console.error('Backtest error:', error);
      toast({
        title: 'Backtest Failed',
        description: 'Failed to run backtest. Please ensure the backend API is running.',
        variant: 'destructive'
      });
    } finally {
      setLoading(false);
    }
  };

  return (
    <div className="p-8">
      <div className="mb-8">
        <h1 className="text-3xl font-bold text-white mb-2">Strategy Backtesting</h1>
        <p className="text-gray-400">Test trading strategies on historical data</p>
      </div>

      {/* Backtest Form */}
      <Card className="bg-slate-900 border-slate-800 mb-6">
        <CardHeader>
          <CardTitle className="text-white">Backtest Configuration</CardTitle>
        </CardHeader>
        <CardContent>
          <form onSubmit={handleRunBacktest} className="space-y-4">
            <div className="grid grid-cols-2 gap-4">
              <div>
                <Label className="text-gray-300">Strategy</Label>
                <Select
                  value={formData.strategyName}
                  onValueChange={(value) => setFormData({ ...formData, strategyName: value })}
                >
                  <SelectTrigger className="bg-slate-800 border-slate-700 text-white">
                    <SelectValue />
                  </SelectTrigger>
                  <SelectContent>
                    <SelectItem value="MomentumStrategy">Momentum Strategy</SelectItem>
                    <SelectItem value="RSIStrategy">RSI Strategy</SelectItem>
                    <SelectItem value="MACDStrategy">MACD Strategy</SelectItem>
                    <SelectItem value="MFIStrategy">MFI Strategy</SelectItem>
                    <SelectItem value="VWAPStrategy">VWAP Strategy</SelectItem>
                  </SelectContent>
                </Select>
              </div>
              <div>
                <Label className="text-gray-300">Symbol</Label>
                <Input
                  value={formData.symbol}
                  onChange={(e) => setFormData({ ...formData, symbol: e.target.value })}
                  className="bg-slate-800 border-slate-700 text-white"
                />
              </div>
              <div>
                <Label className="text-gray-300">Start Date</Label>
                <Input
                  type="date"
                  value={formData.startDate}
                  onChange={(e) => setFormData({ ...formData, startDate: e.target.value })}
                  className="bg-slate-800 border-slate-700 text-white"
                />
              </div>
              <div>
                <Label className="text-gray-300">End Date</Label>
                <Input
                  type="date"
                  value={formData.endDate}
                  onChange={(e) => setFormData({ ...formData, endDate: e.target.value })}
                  className="bg-slate-800 border-slate-700 text-white"
                />
              </div>
              <div>
                <Label className="text-gray-300">Initial Capital</Label>
                <Input
                  type="number"
                  value={formData.initialCapital}
                  onChange={(e) => setFormData({ ...formData, initialCapital: parseFloat(e.target.value) })}
                  className="bg-slate-800 border-slate-700 text-white"
                />
              </div>
            </div>
            <Button type="submit" disabled={loading} className="bg-blue-600 hover:bg-blue-700">
              {loading ? 'Running Backtest...' : 'Run Backtest'}
            </Button>
          </form>
        </CardContent>
      </Card>

      {/* Results */}
      {result && (
        <>
          <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-4 gap-6 mb-6">
            <Card className="bg-slate-900 border-slate-800">
              <CardHeader className="pb-2">
                <div className="flex items-center justify-between">
                  <CardTitle className="text-sm text-gray-400">Total Return</CardTitle>
                  {result.totalReturn >= 0 ? (
                    <TrendingUp className="w-4 h-4 text-green-500" />
                  ) : (
                    <TrendingDown className="w-4 h-4 text-red-500" />
                  )}
                </div>
              </CardHeader>
              <CardContent>
                <div className={`text-2xl font-bold ${result.totalReturn >= 0 ? 'text-green-500' : 'text-red-500'}`}>
                  {result.totalReturn >= 0 ? '+' : ''}${result.totalReturn.toLocaleString()}
                </div>
                <p className="text-xs text-gray-400">{result.totalReturnPercent.toFixed(2)}%</p>
              </CardContent>
            </Card>

            <Card className="bg-slate-900 border-slate-800">
              <CardHeader className="pb-2">
                <CardTitle className="text-sm text-gray-400">Sharpe Ratio</CardTitle>
              </CardHeader>
              <CardContent>
                <div className="text-2xl font-bold text-white">{result.sharpeRatio.toFixed(2)}</div>
                <p className="text-xs text-gray-400">Risk-adjusted return</p>
              </CardContent>
            </Card>

            <Card className="bg-slate-900 border-slate-800">
              <CardHeader className="pb-2">
                <CardTitle className="text-sm text-gray-400">Win Rate</CardTitle>
              </CardHeader>
              <CardContent>
                <div className="text-2xl font-bold text-white">{result.winRate.toFixed(1)}%</div>
                <p className="text-xs text-gray-400">{result.profitableTrades}/{result.totalTrades} trades</p>
              </CardContent>
            </Card>

            <Card className="bg-slate-900 border-slate-800">
              <CardHeader className="pb-2">
                <CardTitle className="text-sm text-gray-400">Max Drawdown</CardTitle>
              </CardHeader>
              <CardContent>
                <div className="text-2xl font-bold text-red-500">{result.maxDrawdown.toFixed(2)}%</div>
                <p className="text-xs text-gray-400">Worst decline</p>
              </CardContent>
            </Card>
          </div>

          <Card className="bg-slate-900 border-slate-800">
            <CardHeader>
              <CardTitle className="text-white">Backtest Summary</CardTitle>
            </CardHeader>
            <CardContent>
              <div className="grid grid-cols-2 gap-4 text-sm">
                <div>
                  <span className="text-gray-400">Initial Capital:</span>
                  <span className="text-white ml-2">${result.initialCapital.toLocaleString()}</span>
                </div>
                <div>
                  <span className="text-gray-400">Final Capital:</span>
                  <span className="text-white ml-2">${result.finalCapital.toLocaleString()}</span>
                </div>
                <div>
                  <span className="text-gray-400">Total Trades:</span>
                  <span className="text-white ml-2">{result.totalTrades}</span>
                </div>
                <div>
                  <span className="text-gray-400">Average Win:</span>
                  <span className="text-green-500 ml-2">${result.averageWin.toFixed(2)}</span>
                </div>
                <div>
                  <span className="text-gray-400">Largest Win:</span>
                  <span className="text-green-500 ml-2">${result.largestWin.toFixed(2)}</span>
                </div>
                <div>
                  <span className="text-gray-400">Average Loss:</span>
                  <span className="text-red-500 ml-2">${result.averageLoss.toFixed(2)}</span>
                </div>
              </div>
            </CardContent>
          </Card>
        </>
      )}
    </div>
  );
}
