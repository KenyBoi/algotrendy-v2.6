import { useState } from 'react';
import { Card } from './ui/card';
import { Button } from './ui/button';
import { Input } from './ui/input';
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from './ui/select';
import { Badge } from './ui/badge';
import { Tabs, TabsContent, TabsList, TabsTrigger } from './ui/tabs';
import { Plus, Trash2, Play, Save, Download } from 'lucide-react';

interface Condition {
  id: string;
  indicator: string;
  operator: string;
  value: string;
}

interface Strategy {
  name: string;
  conditions: Condition[];
  timeframe: string;
  assetClass: string;
}

export function StrategyBuilder() {
  const [strategy, setStrategy] = useState<Strategy>({
    name: 'New Strategy',
    conditions: [
      { id: '1', indicator: 'RSI', operator: '<', value: '30' },
      { id: '2', indicator: 'MACD', operator: '>', value: '0' }
    ],
    timeframe: '1D',
    assetClass: 'Stocks'
  });

  const [backtestResults, setBacktestResults] = useState({
    totalReturn: '+145.23%',
    sharpeRatio: '2.34',
    maxDrawdown: '-12.45%',
    winRate: '64.5%',
    trades: 156
  });

  const addCondition = () => {
    const newCondition: Condition = {
      id: Date.now().toString(),
      indicator: 'SMA',
      operator: '>',
      value: '0'
    };
    setStrategy({
      ...strategy,
      conditions: [...strategy.conditions, newCondition]
    });
  };

  const removeCondition = (id: string) => {
    setStrategy({
      ...strategy,
      conditions: strategy.conditions.filter(c => c.id !== id)
    });
  };

  const updateCondition = (id: string, field: keyof Condition, value: string) => {
    setStrategy({
      ...strategy,
      conditions: strategy.conditions.map(c =>
        c.id === id ? { ...c, [field]: value } : c
      )
    });
  };

  return (
    <div className="flex flex-col gap-6">
      <div className="flex items-center justify-between">
        <div>
          <h2>Strategy Builder</h2>
          <p className="text-gray-600 mt-1">Build and test your algorithmic trading strategies</p>
        </div>
        <div className="flex gap-2">
          <Button variant="outline"><Save className="mr-2 h-4 w-4" /> Save Strategy</Button>
          <Button variant="outline"><Download className="mr-2 h-4 w-4" /> Export</Button>
          <Button><Play className="mr-2 h-4 w-4" /> Run Backtest</Button>
        </div>
      </div>

      <Tabs defaultValue="builder" className="w-full">
        <TabsList>
          <TabsTrigger value="builder">Builder</TabsTrigger>
          <TabsTrigger value="backtest">Backtest Results</TabsTrigger>
          <TabsTrigger value="optimization">Optimization</TabsTrigger>
        </TabsList>

        <TabsContent value="builder" className="space-y-6">
          {/* Strategy Configuration */}
          <Card className="p-6">
            <h3 className="mb-4">Strategy Configuration</h3>
            <div className="grid grid-cols-3 gap-4">
              <div>
                <label className="text-sm text-gray-600 mb-2 block">Strategy Name</label>
                <Input
                  value={strategy.name}
                  onChange={(e) => setStrategy({ ...strategy, name: e.target.value })}
                />
              </div>
              <div>
                <label className="text-sm text-gray-600 mb-2 block">Timeframe</label>
                <Select
                  value={strategy.timeframe}
                  onValueChange={(value) => setStrategy({ ...strategy, timeframe: value })}
                >
                  <SelectTrigger>
                    <SelectValue />
                  </SelectTrigger>
                  <SelectContent>
                    <SelectItem value="1m">1 Minute</SelectItem>
                    <SelectItem value="5m">5 Minutes</SelectItem>
                    <SelectItem value="15m">15 Minutes</SelectItem>
                    <SelectItem value="1H">1 Hour</SelectItem>
                    <SelectItem value="1D">1 Day</SelectItem>
                    <SelectItem value="1W">1 Week</SelectItem>
                  </SelectContent>
                </Select>
              </div>
              <div>
                <label className="text-sm text-gray-600 mb-2 block">Asset Class</label>
                <Select
                  value={strategy.assetClass}
                  onValueChange={(value) => setStrategy({ ...strategy, assetClass: value })}
                >
                  <SelectTrigger>
                    <SelectValue />
                  </SelectTrigger>
                  <SelectContent>
                    <SelectItem value="Stocks">Stocks</SelectItem>
                    <SelectItem value="Crypto">Crypto</SelectItem>
                    <SelectItem value="Forex">Forex</SelectItem>
                    <SelectItem value="Futures">Futures</SelectItem>
                  </SelectContent>
                </Select>
              </div>
            </div>
          </Card>

          {/* Entry Conditions */}
          <Card className="p-6">
            <div className="flex items-center justify-between mb-4">
              <h3>Entry Conditions</h3>
              <Button onClick={addCondition} size="sm">
                <Plus className="mr-2 h-4 w-4" /> Add Condition
              </Button>
            </div>
            <div className="space-y-3">
              {strategy.conditions.map((condition) => (
                <div key={condition.id} className="flex items-center gap-3">
                  <Select
                    value={condition.indicator}
                    onValueChange={(value) => updateCondition(condition.id, 'indicator', value)}
                  >
                    <SelectTrigger className="w-48">
                      <SelectValue />
                    </SelectTrigger>
                    <SelectContent>
                      <SelectItem value="RSI">RSI (Relative Strength Index)</SelectItem>
                      <SelectItem value="MACD">MACD</SelectItem>
                      <SelectItem value="SMA">SMA (Simple Moving Average)</SelectItem>
                      <SelectItem value="EMA">EMA (Exponential Moving Average)</SelectItem>
                      <SelectItem value="BB">Bollinger Bands</SelectItem>
                      <SelectItem value="Volume">Volume</SelectItem>
                      <SelectItem value="ATR">ATR (Average True Range)</SelectItem>
                    </SelectContent>
                  </Select>
                  <Select
                    value={condition.operator}
                    onValueChange={(value) => updateCondition(condition.id, 'operator', value)}
                  >
                    <SelectTrigger className="w-32">
                      <SelectValue />
                    </SelectTrigger>
                    <SelectContent>
                      <SelectItem value=">">Greater than</SelectItem>
                      <SelectItem value="<">Less than</SelectItem>
                      <SelectItem value=">=">Greater or equal</SelectItem>
                      <SelectItem value="<=">Less or equal</SelectItem>
                      <SelectItem value="=">Equal to</SelectItem>
                      <SelectItem value="crosses_above">Crosses above</SelectItem>
                      <SelectItem value="crosses_below">Crosses below</SelectItem>
                    </SelectContent>
                  </Select>
                  <Input
                    value={condition.value}
                    onChange={(e) => updateCondition(condition.id, 'value', e.target.value)}
                    className="w-32"
                    placeholder="Value"
                  />
                  <Button
                    variant="ghost"
                    size="icon"
                    onClick={() => removeCondition(condition.id)}
                  >
                    <Trash2 className="h-4 w-4 text-red-500" />
                  </Button>
                </div>
              ))}
            </div>
          </Card>

          {/* Exit Conditions */}
          <Card className="p-6">
            <div className="flex items-center justify-between mb-4">
              <h3>Exit Conditions</h3>
              <Button size="sm">
                <Plus className="mr-2 h-4 w-4" /> Add Condition
              </Button>
            </div>
            <div className="space-y-3">
              <div className="flex items-center gap-3">
                <Select defaultValue="profit">
                  <SelectTrigger className="w-48">
                    <SelectValue />
                  </SelectTrigger>
                  <SelectContent>
                    <SelectItem value="profit">Take Profit</SelectItem>
                    <SelectItem value="loss">Stop Loss</SelectItem>
                    <SelectItem value="trailing">Trailing Stop</SelectItem>
                    <SelectItem value="time">Time Based</SelectItem>
                  </SelectContent>
                </Select>
                <Input defaultValue="5" className="w-32" placeholder="%" />
              </div>
            </div>
          </Card>
        </TabsContent>

        <TabsContent value="backtest" className="space-y-6">
          {/* Performance Metrics */}
          <Card className="p-6">
            <h3 className="mb-4">Performance Metrics</h3>
            <div className="grid grid-cols-5 gap-6">
              <div>
                <div className="text-sm text-gray-600">Total Return</div>
                <div className="text-2xl mt-1 text-green-600">{backtestResults.totalReturn}</div>
              </div>
              <div>
                <div className="text-sm text-gray-600">Sharpe Ratio</div>
                <div className="text-2xl mt-1">{backtestResults.sharpeRatio}</div>
              </div>
              <div>
                <div className="text-sm text-gray-600">Max Drawdown</div>
                <div className="text-2xl mt-1 text-red-600">{backtestResults.maxDrawdown}</div>
              </div>
              <div>
                <div className="text-sm text-gray-600">Win Rate</div>
                <div className="text-2xl mt-1">{backtestResults.winRate}</div>
              </div>
              <div>
                <div className="text-sm text-gray-600">Total Trades</div>
                <div className="text-2xl mt-1">{backtestResults.trades}</div>
              </div>
            </div>
          </Card>

          {/* Trade History */}
          <Card className="p-6">
            <h3 className="mb-4">Recent Trades</h3>
            <div className="overflow-x-auto">
              <table className="w-full">
                <thead>
                  <tr className="border-b">
                    <th className="text-left py-3 px-4">Date</th>
                    <th className="text-left py-3 px-4">Type</th>
                    <th className="text-right py-3 px-4">Entry Price</th>
                    <th className="text-right py-3 px-4">Exit Price</th>
                    <th className="text-right py-3 px-4">Return</th>
                  </tr>
                </thead>
                <tbody>
                  {[
                    { date: '2024-01-15', type: 'Long', entry: '$135.50', exit: '$142.30', return: '+5.02%' },
                    { date: '2024-01-12', type: 'Long', entry: '$132.80', exit: '$135.20', return: '+1.81%' },
                    { date: '2024-01-08', type: 'Long', entry: '$128.90', exit: '$126.50', return: '-1.86%' }
                  ].map((trade, idx) => (
                    <tr key={idx} className="border-b hover:bg-gray-50">
                      <td className="py-2 px-4 text-sm">{trade.date}</td>
                      <td className="py-2 px-4">
                        <Badge variant={trade.type === 'Long' ? 'default' : 'secondary'}>
                          {trade.type}
                        </Badge>
                      </td>
                      <td className="text-right py-2 px-4 text-sm">{trade.entry}</td>
                      <td className="text-right py-2 px-4 text-sm">{trade.exit}</td>
                      <td className={`text-right py-2 px-4 text-sm ${trade.return.startsWith('+') ? 'text-green-600' : 'text-red-600'}`}>
                        {trade.return}
                      </td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </div>
          </Card>
        </TabsContent>
      </Tabs>
    </div>
  );
}
