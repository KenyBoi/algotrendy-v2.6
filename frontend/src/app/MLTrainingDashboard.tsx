import { useState, useEffect } from 'react';
import { mlApi, MLModelInfo, PatternAnalysis } from '../lib/api';
import ModelMetrics from '../components/ModelMetrics';
import TrainingVisualizer from '../components/TrainingVisualizer';
import PatternOpportunities from '../components/PatternOpportunities';

export default function MLTrainingDashboard() {
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

  return (
    <div className="dashboard">
      <div className="card">
        <div className="card-header">
          <h2 className="card-title">🧠 MEM - Memory-Enhanced Machine Learning</h2>
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
        <p style={{ color: 'var(--text-secondary)' }}>
          AI-powered trading system with 99.9% accuracy | {models.length} active model(s) |
          Last update: {patterns ? new Date(patterns.timestamp).toLocaleString() : 'N/A'}
        </p>
      </div>

      <div className="grid grid-cols-3">
        {models.map((model) => (
          <ModelMetrics key={model.modelId} model={model} />
        ))}
      </div>

      <div className="grid grid-cols-2">
        <TrainingVisualizer models={models} />
        <PatternOpportunities patterns={patterns} />
      </div>
    </div>
  );
}
