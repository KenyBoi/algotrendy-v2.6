import React, { useState } from 'react';
import { Play, Settings, AlertCircle, CheckCircle, AlertTriangle } from 'lucide-react';

interface TestingConfig {
  symbol: string;
  enableBacktest: boolean;
  enableWalkForward: boolean;
  enableGapAnalysis: boolean;
  backtestConfig: {
    nCVSplits: number;
    embargoPct: number;
    testSize: number;
  };
  walkForwardConfig: {
    trainWindowDays: number;
    testWindowDays: number;
    stepDays: number;
  };
}

interface TestingResults {
  timestamp: string;
  backtestResults?: {
    accuracy: number;
    precision: number;
    recall: number;
    sharpeRatio: number;
    maxDrawdown: number;
    winRate: number;
  };
  walkForwardResults?: {
    numPeriods: number;
    avgAccuracy: number;
    avgSharpe: number;
    efficiency: number;
  };
  gapAnalysis?: {
    accuracyGap: number;
    sharpeGap: number;
    trend: string;
    overfittingScore: number;
    degradationPrediction: number;
    recommendation: string;
    isSafeToDeploy: boolean;
  };
}

interface TestingPreset {
  name: string;
  description: string;
  config: TestingConfig;
}

export default function ModelTestingPanel() {
  const [config, setConfig] = useState<TestingConfig>({
    symbol: 'BTCUSDT',
    enableBacktest: true,
    enableWalkForward: true,
    enableGapAnalysis: true,
    backtestConfig: {
      nCVSplits: 5,
      embargoPct: 0.01,
      testSize: 0.2
    },
    walkForwardConfig: {
      trainWindowDays: 365,
      testWindowDays: 90,
      stepDays: 30
    }
  });

  const [results, setResults] = useState<TestingResults | null>(null);
  const [loading, setLoading] = useState(false);
  const [showAdvanced, setShowAdvanced] = useState(false);
  const [presets, setPresets] = useState<TestingPreset[]>([]);

  // Load presets on mount
  React.useEffect(() => {
    loadPresets();
  }, []);

  const loadPresets = async () => {
    try {
      const response = await fetch('/api/mltesting/presets');
      if (response.ok) {
        const data = await response.json();
        setPresets(data);
      }
    } catch (error) {
      console.error('Error loading presets:', error);
    }
  };

  const runTests = async () => {
    setLoading(true);
    try {
      const response = await fetch('/api/mltesting/run', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify(config)
      });

      if (response.ok) {
        const data = await response.json();
        setResults(data);
      } else {
        alert('Testing failed. Check console for details.');
      }
    } catch (error) {
      console.error('Error running tests:', error);
      alert('Error running tests: ' + error);
    } finally {
      setLoading(false);
    }
  };

  const applyPreset = (preset: TestingPreset) => {
    setConfig({ ...config, ...preset.config });
  };

  const getRecommendationIcon = (recommendation?: string) => {
    if (!recommendation) return null;
    if (recommendation.includes('✅')) return <CheckCircle className="w-6 h-6 text-green-500" />;
    if (recommendation.includes('⚠️')) return <AlertTriangle className="w-6 h-6 text-yellow-500" />;
    if (recommendation.includes('⛔')) return <AlertCircle className="w-6 h-6 text-red-500" />;
    return null;
  };

  const getRecommendationColor = (recommendation?: string) => {
    if (!recommendation) return 'gray';
    if (recommendation.includes('✅')) return 'green';
    if (recommendation.includes('⚠️')) return 'yellow';
    if (recommendation.includes('⛔')) return 'red';
    return 'gray';
  };

  return (
    <div className="max-w-7xl mx-auto p-6 space-y-6">
      {/* Header */}
      <div className="bg-white rounded-lg shadow p-6">
        <h1 className="text-2xl font-bold text-gray-900 mb-2">
          Model Testing Framework
        </h1>
        <p className="text-gray-600">
          Comprehensive validation with Backtest (CPCV), Walk-Forward, and Gap Analysis
        </p>
      </div>

      {/* Configuration */}
      <div className="bg-white rounded-lg shadow p-6">
        <div className="flex items-center justify-between mb-4">
          <h2 className="text-lg font-semibold text-gray-900">Test Configuration</h2>
          <button
            onClick={() => setShowAdvanced(!showAdvanced)}
            className="flex items-center gap-2 text-blue-600 hover:text-blue-700"
          >
            <Settings className="w-4 h-4" />
            {showAdvanced ? 'Hide' : 'Show'} Advanced Settings
          </button>
        </div>

        {/* Presets */}
        <div className="mb-6">
          <label className="block text-sm font-medium text-gray-700 mb-2">
            Quick Presets
          </label>
          <div className="grid grid-cols-1 md:grid-cols-3 lg:grid-cols-5 gap-3">
            {presets.map((preset) => (
              <button
                key={preset.name}
                onClick={() => applyPreset(preset)}
                className="p-3 border border-gray-300 rounded-lg hover:border-blue-500 hover:bg-blue-50 transition-colors text-left"
              >
                <div className="font-medium text-sm text-gray-900">{preset.name}</div>
                <div className="text-xs text-gray-500 mt-1">{preset.description}</div>
              </button>
            ))}
          </div>
        </div>

        {/* Symbol */}
        <div className="mb-4">
          <label className="block text-sm font-medium text-gray-700 mb-2">
            Trading Symbol
          </label>
          <input
            type="text"
            value={config.symbol}
            onChange={(e) => setConfig({ ...config, symbol: e.target.value })}
            className="w-full px-3 py-2 border border-gray-300 rounded-lg focus:ring-2 focus:ring-blue-500 focus:border-transparent"
            placeholder="BTCUSDT"
          />
        </div>

        {/* Test Types */}
        <div className="grid grid-cols-1 md:grid-cols-3 gap-4 mb-4">
          <label className="flex items-center gap-3 p-4 border border-gray-300 rounded-lg cursor-pointer hover:bg-gray-50">
            <input
              type="checkbox"
              checked={config.enableBacktest}
              onChange={(e) => setConfig({ ...config, enableBacktest: e.target.checked })}
              className="w-5 h-5 text-blue-600 rounded focus:ring-2 focus:ring-blue-500"
            />
            <div>
              <div className="font-medium text-gray-900">Backtest (CPCV)</div>
              <div className="text-xs text-gray-500">Combinatorial cross-validation</div>
            </div>
          </label>

          <label className="flex items-center gap-3 p-4 border border-gray-300 rounded-lg cursor-pointer hover:bg-gray-50">
            <input
              type="checkbox"
              checked={config.enableWalkForward}
              onChange={(e) => setConfig({ ...config, enableWalkForward: e.target.checked })}
              className="w-5 h-5 text-blue-600 rounded focus:ring-2 focus:ring-blue-500"
            />
            <div>
              <div className="font-medium text-gray-900">Walk-Forward</div>
              <div className="text-xs text-gray-500">Rolling optimization</div>
            </div>
          </label>

          <label className="flex items-center gap-3 p-4 border border-gray-300 rounded-lg cursor-pointer hover:bg-gray-50">
            <input
              type="checkbox"
              checked={config.enableGapAnalysis}
              onChange={(e) => setConfig({ ...config, enableGapAnalysis: e.target.checked })}
              className="w-5 h-5 text-blue-600 rounded focus:ring-2 focus:ring-blue-500"
              disabled={!config.enableBacktest || !config.enableWalkForward}
            />
            <div>
              <div className="font-medium text-gray-900">Gap Analysis</div>
              <div className="text-xs text-gray-500">Overfitting detection</div>
              {(!config.enableBacktest || !config.enableWalkForward) && (
                <div className="text-xs text-orange-500 mt-1">
                  Requires both Backtest and Walk-Forward
                </div>
              )}
            </div>
          </label>
        </div>

        {/* Advanced Settings */}
        {showAdvanced && (
          <div className="border-t border-gray-200 pt-4 mt-4 space-y-4">
            {/* Backtest Settings */}
            {config.enableBacktest && (
              <div className="bg-gray-50 p-4 rounded-lg">
                <h3 className="font-medium text-gray-900 mb-3">Backtest Settings</h3>
                <div className="grid grid-cols-1 md:grid-cols-3 gap-4">
                  <div>
                    <label className="block text-sm font-medium text-gray-700 mb-1">
                      CV Splits
                    </label>
                    <input
                      type="number"
                      value={config.backtestConfig.nCVSplits}
                      onChange={(e) => setConfig({
                        ...config,
                        backtestConfig: { ...config.backtestConfig, nCVSplits: parseInt(e.target.value) }
                      })}
                      className="w-full px-3 py-2 border border-gray-300 rounded-lg"
                      min="2"
                      max="10"
                    />
                    <p className="text-xs text-gray-500 mt-1">Number of cross-validation folds</p>
                  </div>
                  <div>
                    <label className="block text-sm font-medium text-gray-700 mb-1">
                      Embargo %
                    </label>
                    <input
                      type="number"
                      value={config.backtestConfig.embargoPct * 100}
                      onChange={(e) => setConfig({
                        ...config,
                        backtestConfig: { ...config.backtestConfig, embargoPct: parseFloat(e.target.value) / 100 }
                      })}
                      className="w-full px-3 py-2 border border-gray-300 rounded-lg"
                      min="0"
                      max="10"
                      step="0.1"
                    />
                    <p className="text-xs text-gray-500 mt-1">Data purge period (prevents leakage)</p>
                  </div>
                  <div>
                    <label className="block text-sm font-medium text-gray-700 mb-1">
                      Test Size %
                    </label>
                    <input
                      type="number"
                      value={config.backtestConfig.testSize * 100}
                      onChange={(e) => setConfig({
                        ...config,
                        backtestConfig: { ...config.backtestConfig, testSize: parseFloat(e.target.value) / 100 }
                      })}
                      className="w-full px-3 py-2 border border-gray-300 rounded-lg"
                      min="10"
                      max="40"
                    />
                    <p className="text-xs text-gray-500 mt-1">Portion of data for testing</p>
                  </div>
                </div>
              </div>
            )}

            {/* Walk-Forward Settings */}
            {config.enableWalkForward && (
              <div className="bg-gray-50 p-4 rounded-lg">
                <h3 className="font-medium text-gray-900 mb-3">Walk-Forward Settings</h3>
                <div className="grid grid-cols-1 md:grid-cols-3 gap-4">
                  <div>
                    <label className="block text-sm font-medium text-gray-700 mb-1">
                      Train Window (days)
                    </label>
                    <input
                      type="number"
                      value={config.walkForwardConfig.trainWindowDays}
                      onChange={(e) => setConfig({
                        ...config,
                        walkForwardConfig: { ...config.walkForwardConfig, trainWindowDays: parseInt(e.target.value) }
                      })}
                      className="w-full px-3 py-2 border border-gray-300 rounded-lg"
                      min="30"
                      max="1825"
                    />
                    <p className="text-xs text-gray-500 mt-1">Training period length</p>
                  </div>
                  <div>
                    <label className="block text-sm font-medium text-gray-700 mb-1">
                      Test Window (days)
                    </label>
                    <input
                      type="number"
                      value={config.walkForwardConfig.testWindowDays}
                      onChange={(e) => setConfig({
                        ...config,
                        walkForwardConfig: { ...config.walkForwardConfig, testWindowDays: parseInt(e.target.value) }
                      })}
                      className="w-full px-3 py-2 border border-gray-300 rounded-lg"
                      min="7"
                      max="365"
                    />
                    <p className="text-xs text-gray-500 mt-1">Testing period length</p>
                  </div>
                  <div>
                    <label className="block text-sm font-medium text-gray-700 mb-1">
                      Step Size (days)
                    </label>
                    <input
                      type="number"
                      value={config.walkForwardConfig.stepDays}
                      onChange={(e) => setConfig({
                        ...config,
                        walkForwardConfig: { ...config.walkForwardConfig, stepDays: parseInt(e.target.value) }
                      })}
                      className="w-full px-3 py-2 border border-gray-300 rounded-lg"
                      min="1"
                      max="90"
                    />
                    <p className="text-xs text-gray-500 mt-1">Forward step between periods</p>
                  </div>
                </div>
              </div>
            )}
          </div>
        )}

        {/* Run Button */}
        <div className="mt-6">
          <button
            onClick={runTests}
            disabled={loading || (!config.enableBacktest && !config.enableWalkForward)}
            className="w-full flex items-center justify-center gap-2 bg-blue-600 text-white px-6 py-3 rounded-lg font-medium hover:bg-blue-700 disabled:opacity-50 disabled:cursor-not-allowed transition-colors"
          >
            {loading ? (
              <>
                <div className="animate-spin rounded-full h-5 w-5 border-2 border-white border-t-transparent" />
                Running Tests...
              </>
            ) : (
              <>
                <Play className="w-5 h-5" />
                Run Comprehensive Tests
              </>
            )}
          </button>
        </div>
      </div>

      {/* Results */}
      {results && (
        <div className="space-y-6">
          {/* Gap Analysis - Most Important */}
          {results.gapAnalysis && (
            <div className={`bg-white rounded-lg shadow-lg border-l-4 ${
              getRecommendationColor(results.gapAnalysis.recommendation) === 'green' ? 'border-green-500' :
              getRecommendationColor(results.gapAnalysis.recommendation) === 'yellow' ? 'border-yellow-500' :
              getRecommendationColor(results.gapAnalysis.recommendation) === 'red' ? 'border-red-500' :
              'border-gray-500'
            } p-6`}>
              <div className="flex items-start justify-between mb-4">
                <div>
                  <h2 className="text-xl font-bold text-gray-900 mb-1">Gap Analysis Results</h2>
                  <p className="text-sm text-gray-600">Mathematical overfitting detection</p>
                </div>
                {getRecommendationIcon(results.gapAnalysis.recommendation)}
              </div>

              {/* Overfitting Score */}
              <div className="mb-6">
                <div className="flex items-center justify-between mb-2">
                  <span className="text-sm font-medium text-gray-700">Overfitting Score</span>
                  <span className="text-2xl font-bold text-gray-900">
                    {results.gapAnalysis.overfittingScore.toFixed(1)}/100
                  </span>
                </div>
                <div className="w-full bg-gray-200 rounded-full h-4">
                  <div
                    className={`h-4 rounded-full transition-all ${
                      results.gapAnalysis.overfittingScore < 30 ? 'bg-green-500' :
                      results.gapAnalysis.overfittingScore < 70 ? 'bg-yellow-500' :
                      'bg-red-500'
                    }`}
                    style={{ width: `${results.gapAnalysis.overfittingScore}%` }}
                  />
                </div>
                <div className="flex justify-between text-xs text-gray-500 mt-1">
                  <span>Low (&lt;30)</span>
                  <span>Moderate (30-70)</span>
                  <span>High (&gt;70)</span>
                </div>
              </div>

              {/* Key Metrics */}
              <div className="grid grid-cols-2 md:grid-cols-4 gap-4 mb-6">
                <div className="bg-gray-50 p-3 rounded-lg">
                  <div className="text-xs text-gray-600 mb-1">Accuracy Gap</div>
                  <div className="text-lg font-semibold text-gray-900">
                    {(results.gapAnalysis.accuracyGap * 100).toFixed(2)}%
                  </div>
                </div>
                <div className="bg-gray-50 p-3 rounded-lg">
                  <div className="text-xs text-gray-600 mb-1">Sharpe Gap</div>
                  <div className="text-lg font-semibold text-gray-900">
                    {results.gapAnalysis.sharpeGap.toFixed(2)}
                  </div>
                </div>
                <div className="bg-gray-50 p-3 rounded-lg">
                  <div className="text-xs text-gray-600 mb-1">Trend</div>
                  <div className="text-lg font-semibold text-gray-900 uppercase">
                    {results.gapAnalysis.trend}
                  </div>
                </div>
                <div className="bg-gray-50 p-3 rounded-lg">
                  <div className="text-xs text-gray-600 mb-1">Predicted Degradation</div>
                  <div className="text-lg font-semibold text-gray-900">
                    {(results.gapAnalysis.degradationPrediction * 100).toFixed(1)}%
                  </div>
                </div>
              </div>

              {/* Recommendation */}
              <div className={`p-4 rounded-lg ${
                getRecommendationColor(results.gapAnalysis.recommendation) === 'green' ? 'bg-green-50 border border-green-200' :
                getRecommendationColor(results.gapAnalysis.recommendation) === 'yellow' ? 'bg-yellow-50 border border-yellow-200' :
                getRecommendationColor(results.gapAnalysis.recommendation) === 'red' ? 'bg-red-50 border border-red-200' :
                'bg-gray-50 border border-gray-200'
              }`}>
                <div className="font-medium text-gray-900 mb-1">Recommendation</div>
                <div className="text-gray-700">{results.gapAnalysis.recommendation}</div>
              </div>

              {/* What This Means */}
              <div className="mt-4 p-4 bg-blue-50 border border-blue-200 rounded-lg">
                <div className="font-medium text-blue-900 mb-2">What This Means:</div>
                <ul className="text-sm text-blue-800 space-y-1">
                  {results.gapAnalysis.overfittingScore < 30 && (
                    <>
                      <li>✓ Model shows stable performance across validation methods</li>
                      <li>✓ Production performance likely to match test results</li>
                      <li>✓ Low risk of overfitting</li>
                    </>
                  )}
                  {results.gapAnalysis.overfittingScore >= 30 && results.gapAnalysis.overfittingScore < 70 && (
                    <>
                      <li>⚠ Moderate gap detected between backtest and walk-forward</li>
                      <li>⚠ Consider ensemble approach or regularization</li>
                      <li>⚠ Deploy with caution and close monitoring</li>
                    </>
                  )}
                  {results.gapAnalysis.overfittingScore >= 70 && (
                    <>
                      <li>✗ Significant overfitting detected</li>
                      <li>✗ Model likely memorizing training data</li>
                      <li>✗ Production performance will likely degrade</li>
                    </>
                  )}
                  {results.gapAnalysis.trend === 'increasing' && (
                    <li>⚠ Performance degrading over time - implement drift detection</li>
                  )}
                  {results.gapAnalysis.trend === 'decreasing' && (
                    <li>✓ Performance improving or stable over time</li>
                  )}
                </ul>
              </div>
            </div>
          )}

          {/* Backtest Results */}
          {results.backtestResults && (
            <div className="bg-white rounded-lg shadow p-6">
              <h2 className="text-lg font-semibold text-gray-900 mb-4">Backtest Results (CPCV)</h2>
              <div className="grid grid-cols-2 md:grid-cols-3 lg:grid-cols-6 gap-4">
                <div className="bg-gray-50 p-3 rounded-lg">
                  <div className="text-xs text-gray-600 mb-1">Accuracy</div>
                  <div className="text-xl font-bold text-gray-900">
                    {(results.backtestResults.accuracy * 100).toFixed(1)}%
                  </div>
                </div>
                <div className="bg-gray-50 p-3 rounded-lg">
                  <div className="text-xs text-gray-600 mb-1">Precision</div>
                  <div className="text-xl font-bold text-gray-900">
                    {(results.backtestResults.precision * 100).toFixed(1)}%
                  </div>
                </div>
                <div className="bg-gray-50 p-3 rounded-lg">
                  <div className="text-xs text-gray-600 mb-1">Recall</div>
                  <div className="text-xl font-bold text-gray-900">
                    {(results.backtestResults.recall * 100).toFixed(1)}%
                  </div>
                </div>
                <div className="bg-gray-50 p-3 rounded-lg">
                  <div className="text-xs text-gray-600 mb-1">Sharpe Ratio</div>
                  <div className="text-xl font-bold text-gray-900">
                    {results.backtestResults.sharpeRatio.toFixed(2)}
                  </div>
                </div>
                <div className="bg-gray-50 p-3 rounded-lg">
                  <div className="text-xs text-gray-600 mb-1">Max Drawdown</div>
                  <div className="text-xl font-bold text-gray-900">
                    {(results.backtestResults.maxDrawdown * 100).toFixed(2)}%
                  </div>
                </div>
                <div className="bg-gray-50 p-3 rounded-lg">
                  <div className="text-xs text-gray-600 mb-1">Win Rate</div>
                  <div className="text-xl font-bold text-gray-900">
                    {(results.backtestResults.winRate * 100).toFixed(1)}%
                  </div>
                </div>
              </div>
            </div>
          )}

          {/* Walk-Forward Results */}
          {results.walkForwardResults && (
            <div className="bg-white rounded-lg shadow p-6">
              <h2 className="text-lg font-semibold text-gray-900 mb-4">Walk-Forward Results</h2>
              <div className="grid grid-cols-2 md:grid-cols-4 gap-4">
                <div className="bg-gray-50 p-3 rounded-lg">
                  <div className="text-xs text-gray-600 mb-1">Periods Tested</div>
                  <div className="text-xl font-bold text-gray-900">
                    {results.walkForwardResults.numPeriods}
                  </div>
                </div>
                <div className="bg-gray-50 p-3 rounded-lg">
                  <div className="text-xs text-gray-600 mb-1">Avg Accuracy</div>
                  <div className="text-xl font-bold text-gray-900">
                    {(results.walkForwardResults.avgAccuracy * 100).toFixed(1)}%
                  </div>
                </div>
                <div className="bg-gray-50 p-3 rounded-lg">
                  <div className="text-xs text-gray-600 mb-1">Avg Sharpe</div>
                  <div className="text-xl font-bold text-gray-900">
                    {results.walkForwardResults.avgSharpe.toFixed(2)}
                  </div>
                </div>
                <div className="bg-gray-50 p-3 rounded-lg">
                  <div className="text-xs text-gray-600 mb-1">WF Efficiency</div>
                  <div className="text-xl font-bold text-gray-900">
                    {(results.walkForwardResults.efficiency * 100).toFixed(1)}%
                  </div>
                </div>
              </div>
            </div>
          )}
        </div>
      )}
    </div>
  );
}
