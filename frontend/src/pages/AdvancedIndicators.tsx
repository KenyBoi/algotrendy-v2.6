import React, { useState } from 'react';
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card';
import { Button } from '@/components/ui/button';
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from '@/components/ui/select';
import { Badge } from '@/components/ui/badge';
import { Alert, AlertDescription } from '@/components/ui/alert';
import { Loader2, TrendingUp, TrendingDown, Activity, AlertTriangle } from 'lucide-react';
import { advancedIndicatorsApi } from '@/lib/advancedIndicatorsApi';

interface AdvancedIndicatorsPageProps {}

const AdvancedIndicatorsPage: React.FC<AdvancedIndicatorsPageProps> = () => {
  const [symbol, setSymbol] = useState('BTC/USDT');
  const [timeframe, setTimeframe] = useState('1h');
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [results, setResults] = useState<any | null>(null);

  const symbols = [
    'BTC/USDT', 'ETH/USDT', 'BNB/USDT', 'SOL/USDT', 'XRP/USDT',
    'ADA/USDT', 'DOGE/USDT', 'DOT/USDT', 'MATIC/USDT', 'LINK/USDT'
  ];

  const timeframes = [
    { value: '1m', label: '1 Minute' },
    { value: '5m', label: '5 Minutes' },
    { value: '15m', label: '15 Minutes' },
    { value: '30m', label: '30 Minutes' },
    { value: '1h', label: '1 Hour' },
    { value: '4h', label: '4 Hours' },
    { value: '1d', label: '1 Day' },
    { value: '1w', label: '1 Week' }
  ];

  const handleCalculate = async () => {
    setLoading(true);
    setError(null);

    try {
      const data = await advancedIndicatorsApi.calculateAll(symbol, timeframe);
      setResults(data);
    } catch (err: any) {
      setError(err.message || 'Failed to calculate indicators');
      console.error('Error calculating indicators:', err);
    } finally {
      setLoading(false);
    }
  };

  const getSignalColor = (signal: string) => {
    if (signal.includes('BUY') || signal.includes('BULLISH')) return 'bg-green-500';
    if (signal.includes('SELL') || signal.includes('BEARISH')) return 'bg-red-500';
    if (signal.includes('SQUEEZE')) return 'bg-yellow-500';
    return 'bg-gray-500';
  };

  const getSignalIcon = (signal: string) => {
    if (signal.includes('BUY') || signal.includes('BULLISH')) return <TrendingUp className="w-4 h-4" />;
    if (signal.includes('SELL') || signal.includes('BEARISH')) return <TrendingDown className="w-4 h-4" />;
    if (signal.includes('SQUEEZE')) return <Activity className="w-4 h-4" />;
    return <AlertTriangle className="w-4 h-4" />;
  };

  return (
    <div className="container mx-auto p-6 space-y-6">
      {/* Header */}
      <div className="flex justify-between items-center">
        <div>
          <h1 className="text-3xl font-bold">Advanced Indicators</h1>
          <p className="text-gray-600 mt-1">Professional-grade technical analysis tools</p>
        </div>
        <Badge variant="outline" className="text-sm">
          18 Advanced Indicators
        </Badge>
      </div>

      {/* Selection Panel */}
      <Card>
        <CardHeader>
          <CardTitle>Select Symbol & Timeframe</CardTitle>
        </CardHeader>
        <CardContent>
          <div className="grid grid-cols-1 md:grid-cols-3 gap-4">
            {/* Symbol Selection */}
            <div>
              <label className="block text-sm font-medium mb-2">Trading Symbol</label>
              <Select value={symbol} onValueChange={setSymbol}>
                <SelectTrigger>
                  <SelectValue placeholder="Select symbol" />
                </SelectTrigger>
                <SelectContent>
                  {symbols.map((sym) => (
                    <SelectItem key={sym} value={sym}>
                      {sym}
                    </SelectItem>
                  ))}
                </SelectContent>
              </Select>
            </div>

            {/* Timeframe Selection */}
            <div>
              <label className="block text-sm font-medium mb-2">Timeframe</label>
              <Select value={timeframe} onValueChange={setTimeframe}>
                <SelectTrigger>
                  <SelectValue placeholder="Select timeframe" />
                </SelectTrigger>
                <SelectContent>
                  {timeframes.map((tf) => (
                    <SelectItem key={tf.value} value={tf.value}>
                      {tf.label}
                    </SelectItem>
                  ))}
                </SelectContent>
              </Select>
            </div>

            {/* Calculate Button */}
            <div className="flex items-end">
              <Button
                onClick={handleCalculate}
                disabled={loading}
                className="w-full"
                size="lg"
              >
                {loading && <Loader2 className="mr-2 h-4 w-4 animate-spin" />}
                Calculate Indicators
              </Button>
            </div>
          </div>

          {error && (
            <Alert variant="destructive" className="mt-4">
              <AlertDescription>{error}</AlertDescription>
            </Alert>
          )}
        </CardContent>
      </Card>

      {/* Results Display */}
      {results && (
        <>
          {/* Overall Signal */}
          <Card className="bg-gradient-to-r from-blue-50 to-indigo-50">
            <CardContent className="pt-6">
              <div className="flex items-center justify-between">
                <div>
                  <p className="text-sm text-gray-600">Overall Signal</p>
                  <div className="flex items-center gap-3 mt-2">
                    <Badge className={`${getSignalColor(results.overallSignal)} text-white text-lg px-4 py-2`}>
                      {results.overallSignal}
                    </Badge>
                    <span className="text-2xl font-bold">{results.signalStrength.toFixed(1)}%</span>
                    <span className="text-gray-600">Strength</span>
                  </div>
                </div>
                <div className="text-right">
                  <p className="text-sm text-gray-600">Symbol</p>
                  <p className="text-2xl font-bold">{results.symbol}</p>
                  <p className="text-sm text-gray-500">{results.timeframe}</p>
                </div>
              </div>
            </CardContent>
          </Card>

          {/* Advanced Momentum Indicators */}
          <Card>
            <CardHeader>
              <CardTitle className="flex items-center gap-2">
                <Activity className="w-5 h-5" />
                Advanced Momentum Indicators
              </CardTitle>
            </CardHeader>
            <CardContent>
              <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-4">

                {/* Fisher Transform */}
                {results.fisherTransform && (
                  <div className="border rounded-lg p-4 hover:shadow-md transition-shadow">
                    <div className="flex justify-between items-start mb-2">
                      <h3 className="font-semibold">Ehlers Fisher Transform</h3>
                      <Badge className={getSignalColor(results.fisherTransform.signal)}>
                        {results.fisherTransform.signal}
                      </Badge>
                    </div>
                    <div className="space-y-1 text-sm">
                      <div className="flex justify-between">
                        <span className="text-gray-600">Fisher:</span>
                        <span className="font-mono">{results.fisherTransform.fisher.toFixed(4)}</span>
                      </div>
                      <div className="flex justify-between">
                        <span className="text-gray-600">Trigger:</span>
                        <span className="font-mono">{results.fisherTransform.trigger.toFixed(4)}</span>
                      </div>
                    </div>
                    <p className="text-xs text-gray-500 mt-2">Gaussian price distribution</p>
                  </div>
                )}

                {/* Laguerre RSI */}
                {results.laguerreRSI !== null && (
                  <div className="border rounded-lg p-4 hover:shadow-md transition-shadow">
                    <div className="flex justify-between items-start mb-2">
                      <h3 className="font-semibold">Laguerre RSI</h3>
                      <Badge className={results.laguerreRSI < 0.2 ? 'bg-green-500' : results.laguerreRSI > 0.8 ? 'bg-red-500' : 'bg-gray-500'}>
                        {results.laguerreRSI < 0.2 ? 'OVERSOLD' : results.laguerreRSI > 0.8 ? 'OVERBOUGHT' : 'NEUTRAL'}
                      </Badge>
                    </div>
                    <div className="space-y-1 text-sm">
                      <div className="flex justify-between">
                        <span className="text-gray-600">Value:</span>
                        <span className="font-mono">{results.laguerreRSI.toFixed(4)}</span>
                      </div>
                      <div className="w-full bg-gray-200 rounded-full h-2 mt-2">
                        <div
                          className="bg-blue-500 h-2 rounded-full transition-all"
                          style={{ width: `${results.laguerreRSI * 100}%` }}
                        />
                      </div>
                    </div>
                    <p className="text-xs text-gray-500 mt-2">Low-lag RSI variant</p>
                  </div>
                )}

                {/* Connors RSI */}
                {results.connorsRSI !== null && (
                  <div className="border rounded-lg p-4 hover:shadow-md transition-shadow">
                    <div className="flex justify-between items-start mb-2">
                      <h3 className="font-semibold">Connors RSI</h3>
                      <Badge className={results.connorsRSI < 10 ? 'bg-green-500' : results.connorsRSI > 90 ? 'bg-red-500' : 'bg-gray-500'}>
                        {results.connorsRSI < 10 ? 'EXTREME OVERSOLD' : results.connorsRSI > 90 ? 'EXTREME OVERBOUGHT' : 'NEUTRAL'}
                      </Badge>
                    </div>
                    <div className="space-y-1 text-sm">
                      <div className="flex justify-between">
                        <span className="text-gray-600">CRSI:</span>
                        <span className="font-mono">{results.connorsRSI.toFixed(2)}</span>
                      </div>
                      <div className="w-full bg-gray-200 rounded-full h-2 mt-2">
                        <div
                          className="bg-purple-500 h-2 rounded-full transition-all"
                          style={{ width: `${results.connorsRSI}%` }}
                        />
                      </div>
                    </div>
                    <p className="text-xs text-gray-500 mt-2">Short-term mean reversion</p>
                  </div>
                )}

                {/* Squeeze Momentum */}
                {results.squeezeMomentum && (
                  <div className="border rounded-lg p-4 hover:shadow-md transition-shadow">
                    <div className="flex justify-between items-start mb-2">
                      <h3 className="font-semibold">Squeeze Momentum</h3>
                      <Badge className={getSignalColor(results.squeezeMomentum.signal)}>
                        {results.squeezeMomentum.signal}
                      </Badge>
                    </div>
                    <div className="space-y-1 text-sm">
                      <div className="flex justify-between">
                        <span className="text-gray-600">Squeeze:</span>
                        <span className={`font-semibold ${results.squeezeMomentum.isSqueeze ? 'text-yellow-600' : 'text-gray-600'}`}>
                          {results.squeezeMomentum.isSqueeze ? 'ACTIVE ⚡' : 'NONE'}
                        </span>
                      </div>
                      <div className="flex justify-between">
                        <span className="text-gray-600">Momentum:</span>
                        <span className="font-mono">{results.squeezeMomentum.momentum.toFixed(4)}</span>
                      </div>
                    </div>
                    <p className="text-xs text-gray-500 mt-2">BBands + Keltner squeeze</p>
                  </div>
                )}

                {/* Wave Trend */}
                {results.waveTrend && (
                  <div className="border rounded-lg p-4 hover:shadow-md transition-shadow">
                    <div className="flex justify-between items-start mb-2">
                      <h3 className="font-semibold">Wave Trend Oscillator</h3>
                      <Badge className={getSignalColor(results.waveTrend.signal)}>
                        {results.waveTrend.signal}
                      </Badge>
                    </div>
                    <div className="space-y-1 text-sm">
                      <div className="flex justify-between">
                        <span className="text-gray-600">WT1:</span>
                        <span className="font-mono">{results.waveTrend.wt1.toFixed(4)}</span>
                      </div>
                      <div className="flex justify-between">
                        <span className="text-gray-600">WT2:</span>
                        <span className="font-mono">{results.waveTrend.wt2.toFixed(4)}</span>
                      </div>
                    </div>
                    <p className="text-xs text-gray-500 mt-2">TCI + MF combination</p>
                  </div>
                )}

                {/* RVI */}
                {results.rvi && (
                  <div className="border rounded-lg p-4 hover:shadow-md transition-shadow">
                    <div className="flex justify-between items-start mb-2">
                      <h3 className="font-semibold">Relative Vigor Index</h3>
                      <Badge className={results.rvi.trend === 'BULLISH' ? 'bg-green-500' : 'bg-red-500'}>
                        {results.rvi.trend}
                      </Badge>
                    </div>
                    <div className="space-y-1 text-sm">
                      <div className="flex justify-between">
                        <span className="text-gray-600">RVI:</span>
                        <span className="font-mono">{results.rvi.rvi.toFixed(4)}</span>
                      </div>
                      <div className="flex justify-between">
                        <span className="text-gray-600">Signal:</span>
                        <span className="font-mono">{results.rvi.signal.toFixed(4)}</span>
                      </div>
                    </div>
                    <p className="text-xs text-gray-500 mt-2">Close vs open momentum</p>
                  </div>
                )}

                {/* Schaff Trend Cycle */}
                {results.schaffTrendCycle !== null && (
                  <div className="border rounded-lg p-4 hover:shadow-md transition-shadow">
                    <div className="flex justify-between items-start mb-2">
                      <h3 className="font-semibold">Schaff Trend Cycle</h3>
                      <Badge className={results.schaffTrendCycle > 75 ? 'bg-red-500' : results.schaffTrendCycle < 25 ? 'bg-green-500' : 'bg-gray-500'}>
                        {results.schaffTrendCycle > 75 ? 'OVERBOUGHT' : results.schaffTrendCycle < 25 ? 'OVERSOLD' : 'NEUTRAL'}
                      </Badge>
                    </div>
                    <div className="space-y-1 text-sm">
                      <div className="flex justify-between">
                        <span className="text-gray-600">STC:</span>
                        <span className="font-mono">{results.schaffTrendCycle.toFixed(2)}</span>
                      </div>
                      <div className="w-full bg-gray-200 rounded-full h-2 mt-2">
                        <div
                          className="bg-indigo-500 h-2 rounded-full transition-all"
                          style={{ width: `${results.schaffTrendCycle}%` }}
                        />
                      </div>
                    </div>
                    <p className="text-xs text-gray-500 mt-2">Enhanced MACD</p>
                  </div>
                )}
              </div>
            </CardContent>
          </Card>

          {/* Volatility & Risk Indicators */}
          <Card>
            <CardHeader>
              <CardTitle className="flex items-center gap-2">
                <AlertTriangle className="w-5 h-5" />
                Volatility & Risk Metrics
              </CardTitle>
            </CardHeader>
            <CardContent>
              <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-4">

                {/* Historical Volatility */}
                {results.historicalVolatility !== null && (
                  <div className="border rounded-lg p-4 hover:shadow-md transition-shadow">
                    <h3 className="font-semibold mb-2">Historical Volatility</h3>
                    <div className="text-3xl font-bold text-blue-600 mb-1">
                      {results.historicalVolatility.toFixed(2)}%
                    </div>
                    <p className="text-xs text-gray-500">Realized volatility</p>
                  </div>
                )}

                {/* Parkinson Volatility */}
                {results.parkinsonVolatility !== null && (
                  <div className="border rounded-lg p-4 hover:shadow-md transition-shadow">
                    <h3 className="font-semibold mb-2">Parkinson Volatility</h3>
                    <div className="text-3xl font-bold text-green-600 mb-1">
                      {results.parkinsonVolatility.toFixed(2)}%
                    </div>
                    <p className="text-xs text-gray-500">High-Low estimator</p>
                  </div>
                )}

                {/* Garman-Klass Volatility */}
                {results.garmanKlassVolatility !== null && (
                  <div className="border rounded-lg p-4 hover:shadow-md transition-shadow">
                    <h3 className="font-semibold mb-2">Garman-Klass Volatility</h3>
                    <div className="text-3xl font-bold text-purple-600 mb-1">
                      {results.garmanKlassVolatility.toFixed(2)}%
                    </div>
                    <p className="text-xs text-gray-500">OHLC estimator</p>
                  </div>
                )}

                {/* Yang-Zhang Volatility */}
                {results.yangZhangVolatility !== null && (
                  <div className="border rounded-lg p-4 hover:shadow-md transition-shadow">
                    <h3 className="font-semibold mb-2">Yang-Zhang Volatility</h3>
                    <div className="text-3xl font-bold text-orange-600 mb-1">
                      {results.yangZhangVolatility.toFixed(2)}%
                    </div>
                    <p className="text-xs text-gray-500">Best OHLC estimator</p>
                  </div>
                )}

                {/* Choppiness Index */}
                {results.choppinessIndex && (
                  <div className="border rounded-lg p-4 hover:shadow-md transition-shadow col-span-1 md:col-span-2">
                    <div className="flex justify-between items-start mb-2">
                      <h3 className="font-semibold">Choppiness Index</h3>
                      <Badge className={
                        results.choppinessIndex.isTrending ? 'bg-blue-500' :
                        results.choppinessIndex.isRanging ? 'bg-yellow-500' :
                        'bg-gray-500'
                      }>
                        {results.choppinessIndex.state}
                      </Badge>
                    </div>
                    <div className="space-y-2">
                      <div className="flex justify-between text-sm">
                        <span className="text-gray-600">Index:</span>
                        <span className="font-mono font-semibold">{results.choppinessIndex.index.toFixed(2)}</span>
                      </div>
                      <div className="relative w-full bg-gray-200 rounded-full h-4">
                        <div className="absolute left-0 top-0 bottom-0 w-1/3 border-r-2 border-green-500"></div>
                        <div className="absolute right-0 top-0 bottom-0 w-1/3 border-l-2 border-yellow-500"></div>
                        <div
                          className="absolute top-1 h-2 w-2 bg-red-600 rounded-full transform -translate-x-1/2"
                          style={{ left: `${results.choppinessIndex.index}%` }}
                        />
                      </div>
                      <div className="flex justify-between text-xs text-gray-500">
                        <span>Trending (&lt;38.2)</span>
                        <span>Transitional</span>
                        <span>Choppy (&gt;61.8)</span>
                      </div>
                    </div>
                    <p className="text-xs text-gray-500 mt-2">Trend vs range detection</p>
                  </div>
                )}
              </div>
            </CardContent>
          </Card>
        </>
      )}

      {/* Info Panel */}
      {!results && !loading && (
        <Card className="bg-blue-50">
          <CardContent className="pt-6">
            <h3 className="font-semibold mb-2">About Advanced Indicators</h3>
            <p className="text-sm text-gray-700 mb-4">
              These professional-grade indicators provide deep market insights beyond traditional technical analysis.
              Select a symbol and timeframe above, then click "Calculate Indicators" to analyze market conditions.
            </p>
            <div className="grid grid-cols-1 md:grid-cols-3 gap-4 text-sm">
              <div>
                <h4 className="font-semibold text-blue-700">Advanced Momentum (7)</h4>
                <p className="text-gray-600">Fisher Transform, Laguerre RSI, Connors RSI, Squeeze, Wave Trend, RVI, STC</p>
              </div>
              <div>
                <h4 className="font-semibold text-blue-700">Volatility & Risk (5)</h4>
                <p className="text-gray-600">Historical, Parkinson, Garman-Klass, Yang-Zhang, Choppiness Index</p>
              </div>
              <div>
                <h4 className="font-semibold text-blue-700">Multi-Timeframe (4)</h4>
                <p className="text-gray-600">MTF RSI, MTF MA, MTF MACD, HTF Trend Filter (coming soon)</p>
              </div>
            </div>
          </CardContent>
        </Card>
      )}
    </div>
  );
};

export default AdvancedIndicatorsPage;
