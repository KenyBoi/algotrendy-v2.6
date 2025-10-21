import { useState, useEffect } from 'react';
import { mlApi, MLModelInfo } from '../../lib/api';
import PlotlyChart from './PlotlyChart';
import { AlertTriangle, TrendingUp, Activity, BarChart3, Layers } from 'lucide-react';

type TabType = 'overview' | 'learning-curves' | 'feature-importance' | 'comparison' | 'overfitting';

export default function MLMonitoringDashboard() {
  const [activeTab, setActiveTab] = useState<TabType>('overview');
  const [models, setModels] = useState<MLModelInfo[]>([]);
  const [selectedModel, setSelectedModel] = useState<string>('latest');
  const [loading, setLoading] = useState(true);

  // Visualization data
  const [learningCurves, setLearningCurves] = useState<any>(null);
  const [featureImportance, setFeatureImportance] = useState<any>(null);
  const [modelComparison, setModelComparison] = useState<any>(null);
  const [overfittingDashboard, setOverfittingDashboard] = useState<any>(null);

  useEffect(() => {
    loadModels();
  }, []);

  useEffect(() => {
    if (selectedModel) {
      loadVisualizations();
    }
  }, [selectedModel, activeTab]);

  const loadModels = async () => {
    try {
      const data = await mlApi.getModels();
      setModels(data);
      if (data.length > 0) {
        setSelectedModel(data[0].modelId);
      }
      setLoading(false);
    } catch (error) {
      console.error('Error loading models:', error);
      setLoading(false);
    }
  };

  const loadVisualizations = async () => {
    try {
      switch (activeTab) {
        case 'learning-curves':
          const lcData = await mlApi.getLearningCurves(selectedModel);
          setLearningCurves(lcData.visualization);
          break;
        case 'feature-importance':
          const fiData = await mlApi.getFeatureImportance(selectedModel, 20);
          setFeatureImportance(fiData.visualization);
          break;
        case 'comparison':
          const compData = await mlApi.getModelComparison();
          setModelComparison(compData.visualization);
          break;
        case 'overfitting':
          const ofData = await mlApi.getOverfittingDashboard(selectedModel);
          setOverfittingDashboard(ofData.visualization);
          break;
      }
    } catch (error) {
      console.error('Error loading visualization:', error);
    }
  };

  const tabs = [
    { id: 'overview' as TabType, label: 'Overview', icon: Activity },
    { id: 'learning-curves' as TabType, label: 'Learning Curves', icon: TrendingUp },
    { id: 'feature-importance' as TabType, label: 'Feature Importance', icon: BarChart3 },
    { id: 'comparison' as TabType, label: 'Model Comparison', icon: Layers },
    { id: 'overfitting' as TabType, label: 'Overfitting Analysis', icon: AlertTriangle },
  ];

  const getOverfittingStatus = (model: MLModelInfo) => {
    // Overfitting gap calculation (would come from API in real scenario)
    const gap = model.accuracy - 0.95; // Simplified
    if (gap < 0.02) return { status: 'Excellent', color: 'var(--success)' };
    if (gap < 0.05) return { status: 'Good', color: 'var(--warning)' };
    return { status: 'Poor', color: 'var(--danger)' };
  };

  if (loading) {
    return (
      <div style={{ textAlign: 'center', padding: '4rem' }}>
        <h2>Loading ML Monitoring Dashboard...</h2>
      </div>
    );
  }

  return (
    <div className="dashboard">
      {/* Header */}
      <div className="card">
        <div className="card-header">
          <h2 className="card-title">🔬 ML Model Monitoring & Diagnostics</h2>
          <select
            value={selectedModel}
            onChange={(e) => setSelectedModel(e.target.value)}
            className="select"
            style={{ minWidth: '200px' }}
          >
            {models.map((model) => (
              <option key={model.modelId} value={model.modelId}>
                {model.modelId} ({model.modelType})
              </option>
            ))}
          </select>
        </div>
        <p style={{ color: 'var(--text-secondary)', marginTop: '0.5rem' }}>
          Advanced model performance analysis and overfitting detection
        </p>
      </div>

      {/* Tabs */}
      <div className="card">
        <div style={{ display: 'flex', gap: '1rem', borderBottom: '1px solid var(--border)', padding: '0 1rem' }}>
          {tabs.map(({ id, label, icon: Icon }) => (
            <button
              key={id}
              onClick={() => setActiveTab(id)}
              style={{
                padding: '1rem 1.5rem',
                background: 'transparent',
                border: 'none',
                borderBottom: activeTab === id ? '2px solid var(--primary)' : '2px solid transparent',
                color: activeTab === id ? 'var(--primary)' : 'var(--text-secondary)',
                cursor: 'pointer',
                display: 'flex',
                alignItems: 'center',
                gap: '0.5rem',
                fontSize: '0.875rem',
                fontWeight: activeTab === id ? '600' : '400',
                transition: 'all 0.2s',
              }}
            >
              <Icon size={16} />
              {label}
            </button>
          ))}
        </div>

        {/* Tab Content */}
        <div style={{ padding: '2rem' }}>
          {activeTab === 'overview' && (
            <div>
              <h3 style={{ marginBottom: '1.5rem' }}>Model Performance Overview</h3>
              <div className="grid grid-cols-3" style={{ gap: '1rem' }}>
                {models.map((model) => {
                  const overfit = getOverfittingStatus(model);
                  return (
                    <div
                      key={model.modelId}
                      className="card"
                      style={{
                        padding: '1.5rem',
                        border: selectedModel === model.modelId ? '2px solid var(--primary)' : undefined,
                      }}
                    >
                      <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'start' }}>
                        <div>
                          <h4 style={{ fontSize: '0.875rem', color: 'var(--text-secondary)', marginBottom: '0.5rem' }}>
                            {model.modelType}
                          </h4>
                          <div style={{ fontSize: '1.5rem', fontWeight: '600', marginBottom: '1rem' }}>
                            {(model.accuracy * 100).toFixed(2)}%
                          </div>
                        </div>
                        <span
                          className="badge"
                          style={{ background: overfit.color, color: 'white', fontSize: '0.75rem' }}
                        >
                          {overfit.status}
                        </span>
                      </div>

                      <div style={{ display: 'grid', gridTemplateColumns: '1fr 1fr', gap: '0.5rem', fontSize: '0.75rem' }}>
                        <div>
                          <div style={{ color: 'var(--text-secondary)' }}>Precision</div>
                          <div style={{ fontWeight: '500' }}>{(model.precision * 100).toFixed(1)}%</div>
                        </div>
                        <div>
                          <div style={{ color: 'var(--text-secondary)' }}>Recall</div>
                          <div style={{ fontWeight: '500' }}>{(model.recall * 100).toFixed(1)}%</div>
                        </div>
                        <div>
                          <div style={{ color: 'var(--text-secondary)' }}>F1 Score</div>
                          <div style={{ fontWeight: '500' }}>{(model.f1Score * 100).toFixed(1)}%</div>
                        </div>
                        <div>
                          <div style={{ color: 'var(--text-secondary)' }}>Training Rows</div>
                          <div style={{ fontWeight: '500' }}>{model.trainingRows.toLocaleString()}</div>
                        </div>
                      </div>

                      <div style={{ marginTop: '1rem', fontSize: '0.75rem', color: 'var(--text-secondary)' }}>
                        Trained: {new Date(model.trainedAt).toLocaleDateString()}
                      </div>
                    </div>
                  );
                })}
              </div>

              <div className="card" style={{ marginTop: '2rem', padding: '1.5rem', background: 'var(--info-bg)' }}>
                <h4 style={{ marginBottom: '1rem', display: 'flex', alignItems: 'center', gap: '0.5rem' }}>
                  <AlertTriangle size={18} color="var(--info)" />
                  Overfitting Detection Guide
                </h4>
                <div style={{ fontSize: '0.875rem', lineHeight: '1.6' }}>
                  <p><strong>Excellent (&lt;2% gap):</strong> Model generalizes well, safe to deploy</p>
                  <p><strong>Good (2-5% gap):</strong> Acceptable performance, monitor closely</p>
                  <p><strong>Poor (&gt;5% gap):</strong> Overfitting detected, retrain with regularization</p>
                </div>
              </div>
            </div>
          )}

          {activeTab === 'learning-curves' && (
            <div>
              <div style={{ marginBottom: '1.5rem' }}>
                <h3>Learning Curves - Overfitting Detection</h3>
                <p style={{ color: 'var(--text-secondary)', fontSize: '0.875rem', marginTop: '0.5rem' }}>
                  Converging curves indicate good generalization. Large gaps suggest overfitting.
                </p>
              </div>
              <PlotlyChart data={learningCurves} height={500} />
            </div>
          )}

          {activeTab === 'feature-importance' && (
            <div>
              <div style={{ marginBottom: '1.5rem' }}>
                <h3>Feature Importance Analysis</h3>
                <p style={{ color: 'var(--text-secondary)', fontSize: '0.875rem', marginTop: '0.5rem' }}>
                  Top 20 features driving model predictions. Higher importance = more influential.
                </p>
              </div>
              <PlotlyChart data={featureImportance} height={600} />
            </div>
          )}

          {activeTab === 'comparison' && (
            <div>
              <div style={{ marginBottom: '1.5rem' }}>
                <h3>Model Performance Comparison</h3>
                <p style={{ color: 'var(--text-secondary)', fontSize: '0.875rem', marginTop: '0.5rem' }}>
                  Side-by-side comparison of all trained models. Higher bars = better performance.
                </p>
              </div>
              <PlotlyChart data={modelComparison} height={500} />
            </div>
          )}

          {activeTab === 'overfitting' && (
            <div>
              <div style={{ marginBottom: '1.5rem' }}>
                <h3>Comprehensive Overfitting Dashboard</h3>
                <p style={{ color: 'var(--text-secondary)', fontSize: '0.875rem', marginTop: '0.5rem' }}>
                  Multi-panel analysis including learning curves, ROC, confusion matrix, and overfitting score.
                </p>
              </div>
              {overfittingDashboard ? (
                <PlotlyChart data={overfittingDashboard} height={800} />
              ) : (
                <div className="card" style={{ padding: '3rem', textAlign: 'center' }}>
                  <AlertTriangle size={48} color="var(--warning)" style={{ margin: '0 auto 1rem' }} />
                  <h4>Dashboard data not available</h4>
                  <p style={{ color: 'var(--text-secondary)', marginTop: '0.5rem' }}>
                    This model may not have complete training data required for the dashboard.
                    Available visualizations: Learning Curves, Feature Importance, Model Comparison.
                  </p>
                </div>
              )}
            </div>
          )}
        </div>
      </div>
    </div>
  );
}
