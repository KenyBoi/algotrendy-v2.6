import React, { useEffect, useState } from 'react';
import axios from 'axios';

// API Types
interface MetricInfo {
  current: number;
  warningThreshold: number;
  criticalThreshold: number;
  status: 'OK' | 'Warning' | 'Critical';
  unit: string;
}

interface LatencyMetricInfo extends MetricInfo {
  average: number;
  p95: number;
}

interface SystemMetrics {
  timestamp: string;
  cpu: MetricInfo;
  memory: MetricInfo;
  latency: LatencyMetricInfo;
  requestsPerMinute: number;
  errorRate: number;
  uptime: string;
}

interface HealthScore {
  overallScore: number;
  componentScores: Record<string, number>;
  status: string;
  recommendations: string[];
}

interface ScalingDecision {
  currentArchitecture: string;
  recommendation: string;
  confidence: number;
  reasons: string[];
  estimatedCostImpact: string;
  estimatedPerformanceGain: string;
  nextReviewIn: string;
  actionRequired: boolean;
  migrationEstimatedTime: string;
}

const SystemHealthDashboard: React.FC = () => {
  const [metrics, setMetrics] = useState<SystemMetrics | null>(null);
  const [healthScore, setHealthScore] = useState<HealthScore | null>(null);
  const [scalingDecision, setScalingDecision] = useState<ScalingDecision | null>(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [autoRefresh, setAutoRefresh] = useState(true);

  const API_BASE = 'http://localhost:5000/api';

  useEffect(() => {
    fetchAllMetrics();
    if (autoRefresh) {
      const interval = setInterval(fetchAllMetrics, 10000); // Refresh every 10 seconds
      return () => clearInterval(interval);
    }
  }, [autoRefresh]);

  const fetchAllMetrics = async () => {
    try {
      const [metricsRes, healthRes, decisionRes] = await Promise.all([
        axios.get<SystemMetrics>(`${API_BASE}/system-metrics`),
        axios.get<HealthScore>(`${API_BASE}/system-metrics/health-score`),
        axios.get<ScalingDecision>(`${API_BASE}/system-metrics/scaling-decision`)
      ]);

      setMetrics(metricsRes.data);
      setHealthScore(healthRes.data);
      setScalingDecision(decisionRes.data);
      setError(null);
    } catch (err) {
      setError('Failed to fetch metrics. Is the API running?');
      console.error(err);
    } finally {
      setLoading(false);
    }
  };

  const getStatusColor = (status: string): string => {
    switch (status) {
      case 'OK': return 'text-green-600 bg-green-100';
      case 'Warning': return 'text-yellow-600 bg-yellow-100';
      case 'Critical': return 'text-red-600 bg-red-100';
      default: return 'text-gray-600 bg-gray-100';
    }
  };

  const getRecommendationColor = (recommendation: string): string => {
    switch (recommendation) {
      case 'Monolith': return 'bg-green-100 border-green-500';
      case 'Hybrid': return 'bg-yellow-100 border-yellow-500';
      case 'Microservices': return 'bg-red-100 border-red-500';
      default: return 'bg-gray-100 border-gray-500';
    }
  };

  if (loading) {
    return (
      <div className="flex items-center justify-center min-h-screen">
        <div className="text-xl text-gray-600">Loading system health metrics...</div>
      </div>
    );
  }

  if (error) {
    return (
      <div className="flex items-center justify-center min-h-screen">
        <div className="bg-red-100 border border-red-400 text-red-700 px-4 py-3 rounded">
          <strong className="font-bold">Error:</strong>
          <span className="block sm:inline"> {error}</span>
        </div>
      </div>
    );
  }

  return (
    <div className="min-h-screen bg-gray-50 p-8">
      <div className="max-w-7xl mx-auto">
        {/* Header */}
        <div className="mb-8">
          <div className="flex justify-between items-center">
            <div>
              <h1 className="text-3xl font-bold text-gray-900">System Health Dashboard</h1>
              <p className="text-gray-600 mt-2">
                Monitor metrics to determine when to switch architectures
              </p>
            </div>
            <div className="flex items-center gap-4">
              <label className="flex items-center gap-2">
                <input
                  type="checkbox"
                  checked={autoRefresh}
                  onChange={(e) => setAutoRefresh(e.target.checked)}
                  className="rounded"
                />
                <span className="text-sm text-gray-600">Auto-refresh (10s)</span>
              </label>
              <button
                onClick={fetchAllMetrics}
                className="px-4 py-2 bg-blue-600 text-white rounded hover:bg-blue-700"
              >
                Refresh Now
              </button>
            </div>
          </div>
        </div>

        {/* Scaling Decision Card - Most Important */}
        {scalingDecision && (
          <div className={`mb-8 border-4 rounded-lg p-6 ${getRecommendationColor(scalingDecision.recommendation)}`}>
            <div className="flex justify-between items-start">
              <div>
                <h2 className="text-2xl font-bold mb-2">
                  {scalingDecision.actionRequired ? '🚨' : '✅'} Scaling Recommendation
                </h2>
                <div className="text-lg mb-4">
                  <span className="font-semibold">Current:</span> {scalingDecision.currentArchitecture}
                  <span className="mx-4">→</span>
                  <span className="font-semibold">Recommended:</span>
                  <span className={`ml-2 font-bold ${scalingDecision.actionRequired ? 'text-red-700' : 'text-green-700'}`}>
                    {scalingDecision.recommendation.toUpperCase()}
                  </span>
                </div>
              </div>
              <div className="text-right">
                <div className="text-sm text-gray-600">Confidence</div>
                <div className="text-3xl font-bold">{scalingDecision.confidence}%</div>
              </div>
            </div>

            <div className="grid grid-cols-1 md:grid-cols-2 gap-4 mt-4">
              <div>
                <h3 className="font-semibold mb-2">Why?</h3>
                <ul className="space-y-1">
                  {scalingDecision.reasons.map((reason, idx) => (
                    <li key={idx} className="text-sm">• {reason}</li>
                  ))}
                </ul>
              </div>
              <div>
                <h3 className="font-semibold mb-2">Impact</h3>
                <div className="space-y-1 text-sm">
                  <div><strong>Cost:</strong> {scalingDecision.estimatedCostImpact}</div>
                  <div><strong>Performance:</strong> {scalingDecision.estimatedPerformanceGain}</div>
                  <div><strong>Migration Time:</strong> {scalingDecision.migrationEstimatedTime}</div>
                  <div><strong>Next Review:</strong> {scalingDecision.nextReviewIn}</div>
                </div>
              </div>
            </div>

            {scalingDecision.actionRequired && (
              <div className="mt-4 p-4 bg-white bg-opacity-70 rounded">
                <strong>⚠️ Action Required:</strong> Resource constraints detected.
                Consider migration to {scalingDecision.recommendation} architecture.
                <div className="mt-2">
                  <a
                    href="/docs/DUAL_DEPLOYMENT_GUIDE.md"
                    className="text-blue-600 hover:underline mr-4"
                  >
                    View Migration Guide
                  </a>
                  <a
                    href="/docs/MODULAR_VS_MONOLITH.md"
                    className="text-blue-600 hover:underline"
                  >
                    Architecture Comparison
                  </a>
                </div>
              </div>
            )}
          </div>
        )}

        {/* Health Score */}
        {healthScore && (
          <div className="bg-white rounded-lg shadow-lg p-6 mb-8">
            <h2 className="text-2xl font-bold mb-4">Overall Health Score</h2>
            <div className="flex items-center gap-8">
              <div className="flex flex-col items-center">
                <div className={`text-6xl font-bold ${
                  healthScore.overallScore >= 80 ? 'text-green-600' :
                  healthScore.overallScore >= 60 ? 'text-yellow-600' :
                  'text-red-600'
                }`}>
                  {healthScore.overallScore}
                </div>
                <div className="text-gray-600 mt-2">{healthScore.status}</div>
              </div>
              <div className="flex-1">
                <div className="grid grid-cols-2 gap-4">
                  {Object.entries(healthScore.componentScores).map(([component, score]) => (
                    <div key={component} className="flex items-center justify-between">
                      <span className="text-sm font-medium">{component}</span>
                      <span className={`text-sm font-bold ${
                        score >= 80 ? 'text-green-600' :
                        score >= 60 ? 'text-yellow-600' :
                        'text-red-600'
                      }`}>
                        {score}
                      </span>
                    </div>
                  ))}
                </div>
              </div>
            </div>
            {healthScore.recommendations.length > 0 && (
              <div className="mt-4 p-4 bg-gray-50 rounded">
                <h3 className="font-semibold mb-2">Recommendations</h3>
                <ul className="space-y-1">
                  {healthScore.recommendations.map((rec, idx) => (
                    <li key={idx} className="text-sm">{rec}</li>
                  ))}
                </ul>
              </div>
            )}
          </div>
        )}

        {/* Metric Cards */}
        {metrics && (
          <div className="grid grid-cols-1 md:grid-cols-3 gap-6 mb-8">
            {/* CPU Card */}
            <div className="bg-white rounded-lg shadow-lg p-6">
              <div className="flex justify-between items-start mb-4">
                <h3 className="text-lg font-semibold">CPU Usage</h3>
                <span className={`px-3 py-1 rounded-full text-sm font-semibold ${getStatusColor(metrics.cpu.status)}`}>
                  {metrics.cpu.status}
                </span>
              </div>
              <div className="mb-4">
                <div className="text-4xl font-bold">{metrics.cpu.current}%</div>
                <div className="text-sm text-gray-600 mt-1">
                  Warning: {metrics.cpu.warningThreshold}% |
                  Critical: {metrics.cpu.criticalThreshold}%
                </div>
              </div>
              <div className="w-full bg-gray-200 rounded-full h-4">
                <div
                  className={`h-4 rounded-full ${
                    metrics.cpu.status === 'Critical' ? 'bg-red-600' :
                    metrics.cpu.status === 'Warning' ? 'bg-yellow-500' :
                    'bg-green-500'
                  }`}
                  style={{ width: `${Math.min(metrics.cpu.current, 100)}%` }}
                />
              </div>
            </div>

            {/* Memory Card */}
            <div className="bg-white rounded-lg shadow-lg p-6">
              <div className="flex justify-between items-start mb-4">
                <h3 className="text-lg font-semibold">Memory Usage</h3>
                <span className={`px-3 py-1 rounded-full text-sm font-semibold ${getStatusColor(metrics.memory.status)}`}>
                  {metrics.memory.status}
                </span>
              </div>
              <div className="mb-4">
                <div className="text-4xl font-bold">{metrics.memory.current}%</div>
                <div className="text-sm text-gray-600 mt-1">
                  Warning: {metrics.memory.warningThreshold}% |
                  Critical: {metrics.memory.criticalThreshold}%
                </div>
              </div>
              <div className="w-full bg-gray-200 rounded-full h-4">
                <div
                  className={`h-4 rounded-full ${
                    metrics.memory.status === 'Critical' ? 'bg-red-600' :
                    metrics.memory.status === 'Warning' ? 'bg-yellow-500' :
                    'bg-green-500'
                  }`}
                  style={{ width: `${Math.min(metrics.memory.current, 100)}%` }}
                />
              </div>
            </div>

            {/* Latency Card */}
            <div className="bg-white rounded-lg shadow-lg p-6">
              <div className="flex justify-between items-start mb-4">
                <h3 className="text-lg font-semibold">API Latency (P95)</h3>
                <span className={`px-3 py-1 rounded-full text-sm font-semibold ${getStatusColor(metrics.latency.status)}`}>
                  {metrics.latency.status}
                </span>
              </div>
              <div className="mb-4">
                <div className="text-4xl font-bold">{metrics.latency.p95}ms</div>
                <div className="text-sm text-gray-600 mt-1">
                  Avg: {metrics.latency.average}ms |
                  Threshold: {metrics.latency.warningThreshold}ms
                </div>
              </div>
              <div className="w-full bg-gray-200 rounded-full h-4">
                <div
                  className={`h-4 rounded-full ${
                    metrics.latency.status === 'Critical' ? 'bg-red-600' :
                    metrics.latency.status === 'Warning' ? 'bg-yellow-500' :
                    'bg-green-500'
                  }`}
                  style={{ width: `${Math.min((metrics.latency.p95 / metrics.latency.criticalThreshold) * 100, 100)}%` }}
                />
              </div>
            </div>
          </div>
        )}

        {/* Additional Stats */}
        {metrics && (
          <div className="grid grid-cols-1 md:grid-cols-3 gap-6">
            <div className="bg-white rounded-lg shadow-lg p-6">
              <h3 className="text-lg font-semibold mb-2">Traffic</h3>
              <div className="text-3xl font-bold">{Math.round(metrics.requestsPerMinute)}</div>
              <div className="text-sm text-gray-600">requests/minute</div>
            </div>

            <div className="bg-white rounded-lg shadow-lg p-6">
              <h3 className="text-lg font-semibold mb-2">Error Rate</h3>
              <div className={`text-3xl font-bold ${
                metrics.errorRate >= 5 ? 'text-red-600' :
                metrics.errorRate >= 1 ? 'text-yellow-600' :
                'text-green-600'
              }`}>
                {metrics.errorRate.toFixed(2)}%
              </div>
              <div className="text-sm text-gray-600">of all requests</div>
            </div>

            <div className="bg-white rounded-lg shadow-lg p-6">
              <h3 className="text-lg font-semibold mb-2">Uptime</h3>
              <div className="text-3xl font-bold">{metrics.uptime}</div>
              <div className="text-sm text-gray-600">current session</div>
            </div>
          </div>
        )}

        {/* Footer Note */}
        <div className="mt-8 p-4 bg-blue-50 border border-blue-200 rounded-lg">
          <p className="text-sm text-blue-800">
            <strong>📖 Learn More:</strong> See{' '}
            <a href="https://github.com/KenyBoi/algotrendy-v2.6/blob/main/DUAL_DEPLOYMENT_GUIDE.md" className="underline">
              DUAL_DEPLOYMENT_GUIDE.md
            </a>{' '}
            for detailed information on when and how to switch architectures.
          </p>
        </div>
      </div>
    </div>
  );
};

export default SystemHealthDashboard;
