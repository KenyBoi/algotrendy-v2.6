import { MLModelInfo } from '../lib/api';

interface Props {
  model: MLModelInfo;
}

export default function ModelMetrics({ model }: Props) {
  const formatPercent = (value: number) => `${(value * 100).toFixed(2)}%`;
  const formatDate = (date: string) => new Date(date).toLocaleDateString();

  const getMetricColor = (value: number, metric: string) => {
    if (metric === 'accuracy' && value > 0.95) return 'var(--danger)'; // Overfitting
    if (value > 0.75) return 'var(--success)';
    if (value > 0.60) return 'var(--warning)';
    return 'var(--danger)';
  };

  return (
    <div className="card">
      <div className="card-header">
        <div>
          <h3 className="card-title">{model.modelType}</h3>
          <p style={{ fontSize: '0.875rem', color: 'var(--text-secondary)' }}>
            ID: {model.modelId}
          </p>
        </div>
        <span className={`badge ${model.status === 'active' ? 'badge-success' : 'badge-warning'}`}>
          {model.status}
        </span>
      </div>

      <div style={{ display: 'grid', gap: '1rem' }}>
        <MetricBar
          label="Accuracy"
          value={model.accuracy}
          color={getMetricColor(model.accuracy, 'accuracy')}
        />
        <MetricBar
          label="Precision"
          value={model.precision}
          color={getMetricColor(model.precision, 'precision')}
        />
        <MetricBar
          label="Recall"
          value={model.recall}
          color={getMetricColor(model.recall, 'recall')}
        />
        <MetricBar
          label="F1-Score"
          value={model.f1Score}
          color={getMetricColor(model.f1Score, 'f1')}
        />
      </div>

      <div style={{
        marginTop: '1rem',
        paddingTop: '1rem',
        borderTop: '1px solid var(--border)',
        display: 'flex',
        justifyContent: 'space-between',
        fontSize: '0.875rem',
        color: 'var(--text-secondary)'
      }}>
        <span>Training Rows: {model.trainingRows.toLocaleString()}</span>
        <span>Trained: {formatDate(model.trainedAt)}</span>
      </div>
    </div>
  );
}

function MetricBar({ label, value, color }: { label: string; value: number; color: string }) {
  const percent = value * 100;

  return (
    <div>
      <div style={{ display: 'flex', justifyContent: 'space-between', marginBottom: '0.25rem' }}>
        <span style={{ fontSize: '0.875rem', fontWeight: 500 }}>{label}</span>
        <span style={{ fontSize: '0.875rem', color }}>{percent.toFixed(2)}%</span>
      </div>
      <div style={{
        backgroundColor: 'var(--bg-tertiary)',
        borderRadius: '4px',
        height: '8px',
        overflow: 'hidden'
      }}>
        <div style={{
          backgroundColor: color,
          height: '100%',
          width: `${percent}%`,
          transition: 'width 0.3s ease'
        }} />
      </div>
    </div>
  );
}
