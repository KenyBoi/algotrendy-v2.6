import { useState, useEffect } from 'react';
import { mlApi, MLModelInfo, PatternAnalysis } from '../lib/api';
import ModelMetrics from '../components/ModelMetrics';
import TrainingVisualizer from '../components/TrainingVisualizer';
import PatternOpportunities from '../components/PatternOpportunities';
import MLMonitoringDashboard from '../components/ml/MLMonitoringDashboard';
import { Activity, Brain, TrendingUp } from 'lucide-react';

type DashboardView = 'training' | 'monitoring' | 'patterns';

export default function MLTrainingDashboard() {
  const [view, setView] = useState<DashboardView>('monitoring');
  const [models, setModels] = useState<MLModelInfo[]>([]);
  const [patterns, setPatterns] = useState<PatternAnalysis | null>(null);
  const [loading, setLoading] = useState(true);
  const [health, setHealth] = useState<any>(null);

  useEffect(() => {
    loadData();
    const interval = setInterval(loadData, 60000); // Refresh every minute
    return () => clearInterval(interval);
  }, []);

  const loadData = async () => {
    try {
      const [modelsData, patternsData, healthData] = await Promise.all([
        mlApi.getModels(),
        mlApi.getLatestPatterns(),
        mlApi.checkHealth(),
      ]);
      setModels(modelsData);
      setPatterns(patternsData);
      setHealth(healthData);
      setLoading(false);
    } catch (error) {
      console.error('Error loading data:', error);
      setLoading(false);
    }
  };

  const handleStartTraining = async () => {
    try {
      const result = await mlApi.startTraining(
        ['BTC-USD', 'ETH-USD', 'BNB-USD', 'XRP-USD', 'SOL-USD', 'ADA-USD'],
        30
      );
      console.log('Training started:', result);
      // TODO: Show training progress modal
    } catch (error) {
      console.error('Error starting training:', error);
    }
  };

  if (loading) {
    return (
      <div style={{ textAlign: 'center', padding: '4rem' }}>
        <h2>Loading ML Dashboard...</h2>
      </div>
    );
  }

  const views = [
    { id: 'monitoring' as DashboardView, label: 'Model Monitoring', icon: Activity },
    { id: 'training' as DashboardView, label: 'Training & Models', icon: Brain },
    { id: 'patterns' as DashboardView, label: 'Pattern Analysis', icon: TrendingUp },
  ];

  return (
    <div className="dashboard">
      {/* Header with tabs */}
      <div className="card">
        <div className="card-header">
          <div>
            <h2 className="card-title">🧠 AI/ML System - XGBoost + LSTM Hybrid</h2>
            <p style={{ color: 'var(--text-secondary)', marginTop: '0.5rem', fontSize: '0.875rem' }}>
              Advanced machine learning with overfitting detection | {models.length} active model(s) |
              Last update: {patterns ? new Date(patterns.timestamp).toLocaleString() : 'N/A'}
            </p>
          </div>
          <div style={{ display: 'flex', gap: '1rem', alignItems: 'center' }}>
            {health && (
              <span className={`badge ${health.mlApiConnected ? 'badge-success' : 'badge-danger'}`}>
                {health.mlApiConnected ? '✓ ML API Connected' : '✗ ML API Offline'}
              </span>
            )}
            <button className="btn btn-primary" onClick={handleStartTraining}>
              🚀 Start Training
            </button>
          </div>
        </div>

        {/* View Tabs */}
        <div style={{
          display: 'flex',
          gap: '0.5rem',
          marginTop: '1rem',
          borderTop: '1px solid var(--border)',
          paddingTop: '1rem'
        }}>
          {views.map(({ id, label, icon: Icon }) => (
            <button
              key={id}
              onClick={() => setView(id)}
              className={`btn ${view === id ? 'btn-primary' : 'btn-secondary'}`}
              style={{ display: 'flex', alignItems: 'center', gap: '0.5rem' }}
            >
              <Icon size={16} />
              {label}
            </button>
          ))}
        </div>
      </div>

      {/* View Content */}
      {view === 'monitoring' && <MLMonitoringDashboard />}

      {view === 'training' && (
        <div>
          <div className="grid grid-cols-3">
            {models.map((model) => (
              <ModelMetrics key={model.modelId} model={model} />
            ))}
          </div>

          <div className="grid grid-cols-2">
            <TrainingVisualizer models={models} />
            <div className="card">
              <div className="card-header">
                <h3 className="card-title">Quick Actions</h3>
              </div>
              <div style={{ padding: '1.5rem' }}>
                <div style={{ display: 'flex', flexDirection: 'column', gap: '1rem' }}>
                  <button className="btn btn-primary" onClick={handleStartTraining}>
                    🚀 Train New Model
                  </button>
                  <button className="btn btn-secondary" onClick={() => setView('monitoring')}>
                    📊 View Model Metrics
                  </button>
                  <button className="btn btn-secondary" onClick={() => setView('patterns')}>
                    🔍 Analyze Patterns
                  </button>
                </div>

                <div style={{ marginTop: '2rem', padding: '1rem', background: 'var(--info-bg)', borderRadius: '8px' }}>
                  <h4 style={{ fontSize: '0.875rem', marginBottom: '0.5rem' }}>Training Tips</h4>
                  <ul style={{ fontSize: '0.75rem', lineHeight: '1.6', color: 'var(--text-secondary)', paddingLeft: '1.5rem' }}>
                    <li>Use at least 30 days of data for reliable models</li>
                    <li>Monitor overfitting gap (should be &lt;5%)</li>
                    <li>Retrain monthly or when drift score &gt; 0.25</li>
                    <li>Use ensemble predictions for best results</li>
                  </ul>
                </div>
              </div>
            </div>
          </div>
        </div>
      )}

      {view === 'patterns' && (
        <div>
          <PatternOpportunities patterns={patterns} />
        </div>
      )}
    </div>
  );
}
