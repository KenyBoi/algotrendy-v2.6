import { TrendingUp, TrendingDown, Activity, Zap, DollarSign, BarChart3, Clock, Brain, Rocket, CheckCircle2, AlertCircle, Play } from 'lucide-react';
import { Card } from './ui/card';
import { LineChart, Line, BarChart, Bar, AreaChart, Area, XAxis, YAxis, CartesianGrid, Tooltip, Legend, ResponsiveContainer } from 'recharts';
import { Button } from './ui/button';

export function Dashboard() {
  // ML Model Performance Over Time
  const mlPerformanceData = [
    { date: 'Jan', accuracy: 82.4, precision: 79.2, recall: 85.1, f1Score: 82.0 },
    { date: 'Feb', accuracy: 84.1, precision: 81.3, recall: 86.8, f1Score: 83.9 },
    { date: 'Mar', accuracy: 86.8, precision: 84.2, recall: 88.3, f1Score: 86.2 },
    { date: 'Apr', accuracy: 88.2, precision: 86.1, recall: 89.7, f1Score: 87.8 },
    { date: 'May', accuracy: 89.5, precision: 87.8, recall: 90.9, f1Score: 89.3 },
    { date: 'Jun', accuracy: 91.2, precision: 89.4, recall: 92.1, f1Score: 90.7 },
  ];

  // Backtesting Results
  const backtestingData = [
    { date: '1/1', strategy: 42.5, benchmark: 28.3 },
    { date: '2/1', strategy: 48.2, benchmark: 31.1 },
    { date: '3/1', strategy: 45.8, benchmark: 29.8 },
    { date: '4/1', strategy: 52.3, benchmark: 33.5 },
    { date: '5/1', strategy: 58.7, benchmark: 35.2 },
    { date: '6/1', strategy: 64.2, benchmark: 37.8 },
    { date: '7/1', strategy: 71.5, benchmark: 39.4 },
    { date: '8/1', strategy: 68.9, benchmark: 38.9 },
    { date: '9/1', strategy: 76.4, benchmark: 41.2 },
    { date: '10/1', strategy: 82.3, benchmark: 43.6 },
  ];

  // Model Training Metrics
  const trainingMetrics = [
    { epoch: 1, loss: 0.42, valLoss: 0.45 },
    { epoch: 5, loss: 0.28, valLoss: 0.32 },
    { epoch: 10, loss: 0.18, valLoss: 0.22 },
    { epoch: 15, loss: 0.12, valLoss: 0.16 },
    { epoch: 20, loss: 0.08, valLoss: 0.13 },
    { epoch: 25, loss: 0.05, valLoss: 0.11 },
  ];

  // Prediction Confidence Distribution
  const confidenceData = [
    { range: '90-100%', count: 2847 },
    { range: '80-90%', count: 1923 },
    { range: '70-80%', count: 1145 },
    { range: '60-70%', count: 642 },
    { range: '50-60%', count: 328 },
  ];

  // Models ready to deploy
  const readyModels = [
    { 
      name: 'Momentum Alpha v4.2', 
      accuracy: 91.2, 
      sharpeRatio: 2.84, 
      maxDrawdown: '-8.3%',
      status: 'Ready',
      lastTested: '2 hours ago'
    },
    { 
      name: 'Mean Reversion XGBoost', 
      accuracy: 88.7, 
      sharpeRatio: 2.41, 
      maxDrawdown: '-11.2%',
      status: 'Ready',
      lastTested: '5 hours ago'
    },
    { 
      name: 'Volatility LSTM', 
      accuracy: 89.3, 
      sharpeRatio: 2.67, 
      maxDrawdown: '-9.8%',
      status: 'Testing',
      lastTested: '12 hours ago'
    },
  ];

  const mlMetrics = [
    { label: 'Model Accuracy', value: '91.2%', change: '+2.7%', trend: 'up' },
    { label: 'Avg Confidence', value: '87.4%', change: '+1.2%', trend: 'up' },
    { label: 'Predictions Today', value: '3,847', change: '+412', trend: 'up' },
    { label: 'Win Rate', value: '68.3%', change: '+3.1%', trend: 'up' },
  ];

  return (
    <div className="space-y-6">
      {/* Header */}
      <div className="flex items-center justify-between">
        <div>
          <h1 className="text-2xl text-white mb-1">ML Testing & Deployment</h1>
          <p className="text-sm text-gray-400">
            Real-time machine learning performance metrics and model deployment center
          </p>
        </div>
        <div className="flex items-center gap-2 px-4 py-2 bg-slate-800/50 border border-slate-700 rounded">
          <Brain className="w-4 h-4 text-purple-400" />
          <span className="text-sm text-gray-300">MEM Learning</span>
        </div>
      </div>

      {/* ML Metrics */}
      <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-4 gap-4">
        {mlMetrics.map((metric, idx) => (
          <Card key={idx} className="bg-slate-900 border-slate-800 p-4">
            <div className="flex items-start justify-between mb-2">
              <p className="text-xs text-gray-400">{metric.label}</p>
              {metric.trend === 'up' && <TrendingUp className="w-4 h-4 text-green-400" />}
            </div>
            <p className="text-2xl text-white mb-1">{metric.value}</p>
            <p className="text-xs text-green-400">{metric.change}</p>
          </Card>
        ))}
      </div>

      {/* Charts Row 1 */}
      <div className="grid grid-cols-1 lg:grid-cols-2 gap-6">
        {/* ML Model Performance */}
        <Card className="bg-slate-900 border-slate-800 p-6">
          <div className="flex items-center gap-2 mb-4">
            <Brain className="w-5 h-5 text-purple-400" />
            <h2 className="text-white">Model Performance Evolution</h2>
          </div>
          <ResponsiveContainer width="100%" height={280}>
            <LineChart data={mlPerformanceData}>
              <CartesianGrid strokeDasharray="3 3" stroke="#334155" />
              <XAxis dataKey="date" stroke="#94a3b8" fontSize={12} />
              <YAxis stroke="#94a3b8" fontSize={12} domain={[75, 95]} />
              <Tooltip 
                contentStyle={{ 
                  backgroundColor: '#1e293b', 
                  border: '1px solid #475569',
                  borderRadius: '6px'
                }}
                labelStyle={{ color: '#e2e8f0' }}
              />
              <Legend wrapperStyle={{ fontSize: '12px' }} />
              <Line type="monotone" dataKey="accuracy" stroke="#8b5cf6" strokeWidth={2} name="Accuracy" />
              <Line type="monotone" dataKey="precision" stroke="#06b6d4" strokeWidth={2} name="Precision" />
              <Line type="monotone" dataKey="recall" stroke="#10b981" strokeWidth={2} name="Recall" />
              <Line type="monotone" dataKey="f1Score" stroke="#f59e0b" strokeWidth={2} name="F1 Score" />
            </LineChart>
          </ResponsiveContainer>
        </Card>

        {/* Backtesting Results */}
        <Card className="bg-slate-900 border-slate-800 p-6">
          <div className="flex items-center gap-2 mb-4">
            <BarChart3 className="w-5 h-5 text-blue-400" />
            <h2 className="text-white">Backtest Performance vs Benchmark</h2>
          </div>
          <ResponsiveContainer width="100%" height={280}>
            <AreaChart data={backtestingData}>
              <CartesianGrid strokeDasharray="3 3" stroke="#334155" />
              <XAxis dataKey="date" stroke="#94a3b8" fontSize={12} />
              <YAxis stroke="#94a3b8" fontSize={12} />
              <Tooltip 
                contentStyle={{ 
                  backgroundColor: '#1e293b', 
                  border: '1px solid #475569',
                  borderRadius: '6px'
                }}
                labelStyle={{ color: '#e2e8f0' }}
              />
              <Legend wrapperStyle={{ fontSize: '12px' }} />
              <Area type="monotone" dataKey="strategy" stackId="1" stroke="#10b981" fill="#10b981" fillOpacity={0.6} name="MEM Strategy" />
              <Area type="monotone" dataKey="benchmark" stackId="2" stroke="#64748b" fill="#64748b" fillOpacity={0.3} name="S&P 500" />
            </AreaChart>
          </ResponsiveContainer>
        </Card>
      </div>

      {/* Charts Row 2 */}
      <div className="grid grid-cols-1 lg:grid-cols-2 gap-6">
        {/* Training Loss */}
        <Card className="bg-slate-900 border-slate-800 p-6">
          <div className="flex items-center gap-2 mb-4">
            <Activity className="w-5 h-5 text-green-400" />
            <h2 className="text-white">Training & Validation Loss</h2>
          </div>
          <ResponsiveContainer width="100%" height={280}>
            <LineChart data={trainingMetrics}>
              <CartesianGrid strokeDasharray="3 3" stroke="#334155" />
              <XAxis dataKey="epoch" stroke="#94a3b8" fontSize={12} />
              <YAxis stroke="#94a3b8" fontSize={12} domain={[0, 0.5]} />
              <Tooltip 
                contentStyle={{ 
                  backgroundColor: '#1e293b', 
                  border: '1px solid #475569',
                  borderRadius: '6px'
                }}
                labelStyle={{ color: '#e2e8f0' }}
              />
              <Legend wrapperStyle={{ fontSize: '12px' }} />
              <Line type="monotone" dataKey="loss" stroke="#10b981" strokeWidth={2} name="Training Loss" />
              <Line type="monotone" dataKey="valLoss" stroke="#f59e0b" strokeWidth={2} name="Validation Loss" />
            </LineChart>
          </ResponsiveContainer>
        </Card>

        {/* Confidence Distribution */}
        <Card className="bg-slate-900 border-slate-800 p-6">
          <div className="flex items-center gap-2 mb-4">
            <Zap className="w-5 h-5 text-yellow-400" />
            <h2 className="text-white">Prediction Confidence Distribution</h2>
          </div>
          <ResponsiveContainer width="100%" height={280}>
            <BarChart data={confidenceData}>
              <CartesianGrid strokeDasharray="3 3" stroke="#334155" />
              <XAxis dataKey="range" stroke="#94a3b8" fontSize={12} />
              <YAxis stroke="#94a3b8" fontSize={12} />
              <Tooltip 
                contentStyle={{ 
                  backgroundColor: '#1e293b', 
                  border: '1px solid #475569',
                  borderRadius: '6px'
                }}
                labelStyle={{ color: '#e2e8f0' }}
              />
              <Bar dataKey="count" fill="#8b5cf6" radius={[4, 4, 0, 0]} />
            </BarChart>
          </ResponsiveContainer>
        </Card>
      </div>

      {/* Deploy Trading Section */}
      <Card className="bg-slate-900 border-slate-800 p-6">
        <div className="flex items-center justify-between mb-6">
          <div className="flex items-center gap-2">
            <Rocket className="w-5 h-5 text-blue-400" />
            <h2 className="text-white">Deploy Trading Models</h2>
          </div>
          <Button className="bg-blue-600 hover:bg-blue-700 text-white">
            <Play className="w-4 h-4 mr-2" />
            Deploy Selected
          </Button>
        </div>

        <div className="space-y-3">
          {readyModels.map((model, idx) => (
            <div key={idx} className="p-4 bg-slate-800/50 border border-slate-700 rounded-lg hover:border-slate-600 transition-colors">
              <div className="flex items-center justify-between">
                <div className="flex items-center gap-4 flex-1">
                  <input 
                    type="checkbox" 
                    className="w-4 h-4 rounded border-slate-600 bg-slate-800 text-blue-600"
                    defaultChecked={model.status === 'Ready'}
                  />
                  <div className="flex-1">
                    <div className="flex items-center gap-3 mb-2">
                      <h3 className="text-white">{model.name}</h3>
                      <span className={`px-2 py-0.5 text-xs rounded ${
                        model.status === 'Ready' 
                          ? 'bg-green-500/10 text-green-400 border border-green-500/20' 
                          : 'bg-yellow-500/10 text-yellow-400 border border-yellow-500/20'
                      }`}>
                        {model.status}
                      </span>
                    </div>
                    <div className="flex items-center gap-6 text-xs text-gray-400">
                      <div className="flex items-center gap-1">
                        <CheckCircle2 className="w-3 h-3 text-green-400" />
                        <span>Accuracy: <span className="text-green-400">{model.accuracy}%</span></span>
                      </div>
                      <div className="flex items-center gap-1">
                        <TrendingUp className="w-3 h-3 text-blue-400" />
                        <span>Sharpe: <span className="text-blue-400">{model.sharpeRatio}</span></span>
                      </div>
                      <div className="flex items-center gap-1">
                        <TrendingDown className="w-3 h-3 text-red-400" />
                        <span>Max DD: <span className="text-red-400">{model.maxDrawdown}</span></span>
                      </div>
                      <div className="flex items-center gap-1">
                        <Clock className="w-3 h-3" />
                        <span>Tested {model.lastTested}</span>
                      </div>
                    </div>
                  </div>
                </div>
                <Button 
                  variant="outline" 
                  className="bg-slate-800 border-slate-600 text-white hover:bg-slate-700"
                >
                  Configure
                </Button>
              </div>
            </div>
          ))}
        </div>

        {/* Deploy Options */}
        <div className="mt-6 p-4 bg-slate-800/30 border border-slate-700 rounded-lg">
          <div className="grid grid-cols-1 md:grid-cols-3 gap-4">
            <div>
              <label className="text-xs text-gray-400 mb-2 block">Capital Allocation</label>
              <input 
                type="text" 
                className="w-full bg-slate-800 border border-slate-700 rounded px-3 py-2 text-white text-sm"
                placeholder="$100,000"
              />
            </div>
            <div>
              <label className="text-xs text-gray-400 mb-2 block">Risk Level</label>
              <select className="w-full bg-slate-800 border border-slate-700 rounded px-3 py-2 text-white text-sm">
                <option>Conservative</option>
                <option>Moderate</option>
                <option>Aggressive</option>
              </select>
            </div>
            <div>
              <label className="text-xs text-gray-400 mb-2 block">Max Positions</label>
              <input 
                type="text" 
                className="w-full bg-slate-800 border border-slate-700 rounded px-3 py-2 text-white text-sm"
                placeholder="20"
              />
            </div>
          </div>
        </div>
      </Card>
    </div>
  );
}
