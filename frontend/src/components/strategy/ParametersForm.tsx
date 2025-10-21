import { StrategyParameter } from '../../types/strategy';
import { Plus, Trash2 } from 'lucide-react';
import { useState } from 'react';

interface Props {
  parameters: StrategyParameter[];
  onChange: (parameters: StrategyParameter[]) => void;
}

export default function ParametersForm({ parameters, onChange }: Props) {
  const [showAddForm, setShowAddForm] = useState(false);
  const [newParam, setNewParam] = useState<StrategyParameter>({
    name: '',
    type: 'number',
    value: 0,
    required: true,
  });

  const handleAddParameter = () => {
    if (newParam.name && !parameters.find(p => p.name === newParam.name)) {
      onChange([...parameters, newParam]);
      setNewParam({ name: '', type: 'number', value: 0, required: true });
      setShowAddForm(false);
    }
  };

  const handleUpdateParameter = (index: number, updated: StrategyParameter) => {
    const newParams = [...parameters];
    newParams[index] = updated;
    onChange(newParams);
  };

  const handleRemoveParameter = (index: number) => {
    onChange(parameters.filter((_, i) => i !== index));
  };

  return (
    <div className="parameters-form">
      <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', marginBottom: '2rem' }}>
        <h2 style={{ margin: 0 }}>Strategy Parameters</h2>
        <button
          className="btn-primary"
          onClick={() => setShowAddForm(!showAddForm)}
          style={{ display: 'flex', alignItems: 'center', gap: '0.5rem' }}
        >
          <Plus size={18} />
          Add Parameter
        </button>
      </div>

      {showAddForm && (
        <div className="add-parameter-form">
          <h3>Add New Parameter</h3>
          <div className="form-grid">
            <div className="form-group">
              <label>Parameter Name *</label>
              <input
                type="text"
                value={newParam.name}
                onChange={e => setNewParam({ ...newParam, name: e.target.value })}
                placeholder="e.g., lookback_period"
                className="form-input"
              />
            </div>

            <div className="form-group">
              <label>Type *</label>
              <select
                value={newParam.type}
                onChange={e => setNewParam({ ...newParam, type: e.target.value as any })}
                className="form-input"
              >
                <option value="number">Number</option>
                <option value="string">String</option>
                <option value="boolean">Boolean</option>
                <option value="select">Select (Dropdown)</option>
              </select>
            </div>

            <div className="form-group">
              <label>Default Value *</label>
              {newParam.type === 'boolean' ? (
                <select
                  value={newParam.value.toString()}
                  onChange={e => setNewParam({ ...newParam, value: e.target.value === 'true' })}
                  className="form-input"
                >
                  <option value="true">True</option>
                  <option value="false">False</option>
                </select>
              ) : newParam.type === 'number' ? (
                <input
                  type="number"
                  value={newParam.value}
                  onChange={e => setNewParam({ ...newParam, value: parseFloat(e.target.value) })}
                  className="form-input"
                />
              ) : (
                <input
                  type="text"
                  value={newParam.value}
                  onChange={e => setNewParam({ ...newParam, value: e.target.value })}
                  className="form-input"
                />
              )}
            </div>

            <div className="form-group full-width">
              <label>Description</label>
              <input
                type="text"
                value={newParam.description || ''}
                onChange={e => setNewParam({ ...newParam, description: e.target.value })}
                placeholder="Optional description..."
                className="form-input"
              />
            </div>
          </div>

          <div style={{ display: 'flex', gap: '1rem', marginTop: '1rem' }}>
            <button onClick={handleAddParameter} className="btn-primary">Add</button>
            <button onClick={() => setShowAddForm(false)} className="btn-secondary">Cancel</button>
          </div>
        </div>
      )}

      {parameters.length === 0 ? (
        <div style={{ textAlign: 'center', padding: '3rem', color: 'var(--text-secondary)' }}>
          <p>No parameters yet. Click "Add Parameter" to get started.</p>
        </div>
      ) : (
        <div className="parameters-list">
          {parameters.map((param, index) => (
            <div key={index} className="parameter-item">
              <div className="parameter-header">
                <div>
                  <h4 style={{ margin: 0 }}>{param.name}</h4>
                  {param.description && (
                    <p style={{ margin: '0.25rem 0 0 0', fontSize: '0.85rem', color: 'var(--text-secondary)' }}>
                      {param.description}
                    </p>
                  )}
                </div>
                <button
                  onClick={() => handleRemoveParameter(index)}
                  className="btn-icon"
                  style={{ color: 'var(--error)' }}
                >
                  <Trash2 size={18} />
                </button>
              </div>

              <div className="parameter-fields">
                <div className="form-group">
                  <label>Type</label>
                  <select
                    value={param.type}
                    onChange={e => handleUpdateParameter(index, { ...param, type: e.target.value as any })}
                    className="form-input"
                  >
                    <option value="number">Number</option>
                    <option value="string">String</option>
                    <option value="boolean">Boolean</option>
                    <option value="select">Select</option>
                  </select>
                </div>

                <div className="form-group">
                  <label>Value</label>
                  {param.type === 'boolean' ? (
                    <select
                      value={param.value.toString()}
                      onChange={e => handleUpdateParameter(index, { ...param, value: e.target.value === 'true' })}
                      className="form-input"
                    >
                      <option value="true">True</option>
                      <option value="false">False</option>
                    </select>
                  ) : param.type === 'number' ? (
                    <input
                      type="number"
                      value={param.value}
                      onChange={e => handleUpdateParameter(index, { ...param, value: parseFloat(e.target.value) })}
                      className="form-input"
                      step={param.step}
                      min={param.min}
                      max={param.max}
                    />
                  ) : (
                    <input
                      type="text"
                      value={param.value}
                      onChange={e => handleUpdateParameter(index, { ...param, value: e.target.value })}
                      className="form-input"
                    />
                  )}
                </div>

                {param.type === 'number' && (
                  <>
                    <div className="form-group">
                      <label>Min</label>
                      <input
                        type="number"
                        value={param.min || ''}
                        onChange={e => handleUpdateParameter(index, { ...param, min: parseFloat(e.target.value) || undefined })}
                        className="form-input"
                        placeholder="Optional"
                      />
                    </div>
                    <div className="form-group">
                      <label>Max</label>
                      <input
                        type="number"
                        value={param.max || ''}
                        onChange={e => handleUpdateParameter(index, { ...param, max: parseFloat(e.target.value) || undefined })}
                        className="form-input"
                        placeholder="Optional"
                      />
                    </div>
                  </>
                )}
              </div>
            </div>
          ))}
        </div>
      )}

      <style>{`
        .parameters-form {
          max-width: 1000px;
        }

        .add-parameter-form {
          background: var(--background);
          border: 1px solid var(--border);
          border-radius: 8px;
          padding: 1.5rem;
          margin-bottom: 2rem;
        }

        .add-parameter-form h3 {
          margin-top: 0;
        }

        .parameters-list {
          display: flex;
          flex-direction: column;
          gap: 1rem;
        }

        .parameter-item {
          background: var(--background);
          border: 1px solid var(--border);
          border-radius: 8px;
          padding: 1.5rem;
        }

        .parameter-header {
          display: flex;
          justify-content: space-between;
          align-items: flex-start;
          margin-bottom: 1rem;
        }

        .parameter-fields {
          display: grid;
          grid-template-columns: repeat(auto-fit, minmax(200px, 1fr));
          gap: 1rem;
        }

        .btn-icon {
          background: none;
          border: none;
          cursor: pointer;
          padding: 0.5rem;
          display: flex;
          align-items: center;
          justify-content: center;
          border-radius: 4px;
          transition: background 0.2s;
        }

        .btn-icon:hover {
          background: var(--background);
        }
      `}</style>
    </div>
  );
}
