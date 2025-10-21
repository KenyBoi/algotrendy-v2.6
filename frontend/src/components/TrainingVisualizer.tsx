import { BarChart, Bar, XAxis, YAxis, CartesianGrid, Tooltip, Legend, ResponsiveContainer } from 'recharts';
import { MLModelInfo } from '../lib/api';

interface Props {
  models: MLModelInfo[];
}

export default function TrainingVisualizer({ models }: Props) {
  // Transform model data for chart
  const chartData = models.map(model => ({
    name: model.modelType,
    Accuracy: (model.accuracy * 100).toFixed(2),
    Precision: (model.precision * 100).toFixed(2),
    Recall: (model.recall * 100).toFixed(2),
    'F1-Score': (model.f1Score * 100).toFixed(2),
  }));

  return (
    <div className="card">
      <div className="card-header">
        <h3 className="card-title">📊 Model Performance Comparison</h3>
      </div>

      <ResponsiveContainer width="100%" height={300}>
        <BarChart data={chartData}>
          <CartesianGrid strokeDasharray="3 3" stroke="#475569" />
          <XAxis dataKey="name" stroke="#cbd5e1" />
          <YAxis stroke="#cbd5e1" domain={[0, 100]} />
          <Tooltip
            contentStyle={{
              backgroundColor: '#1e293b',
              border: '1px solid #475569',
              borderRadius: '6px',
            }}
          />
          <Legend />
          <Bar dataKey="Accuracy" fill="#3b82f6" />
          <Bar dataKey="Precision" fill="#10b981" />
          <Bar dataKey="Recall" fill="#f59e0b" />
          <Bar dataKey="F1-Score" fill="#8b5cf6" />
        </BarChart>
      </ResponsiveContainer>

      {models.length === 0 && (
        <div style={{ textAlign: 'center', padding: '3rem', color: 'var(--text-secondary)' }}>
          <p>No models available. Start training to see performance metrics.</p>
        </div>
      )}
    </div>
  );
}
