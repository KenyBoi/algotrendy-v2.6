import { StrategySignal, StrategyCondition } from '../../types/strategy';
import { Plus, Trash2 } from 'lucide-react';

interface Props {
  signals: { entry: StrategySignal[]; exit: StrategySignal[] };
  onChange: (signals: { entry: StrategySignal[]; exit: StrategySignal[] }) => void;
}

export default function SignalsBuilder({ signals, onChange }: Props) {
  const addSignal = (type: 'entry' | 'exit') => {
    const newSignal: StrategySignal = {
      type,
      side: type === 'entry' ? 'buy' : 'sell',
      conditions: [],
      logic: 'all',
    };

    onChange({
      ...signals,
      [type]: [...signals[type], newSignal],
    });
  };

  const removeSignal = (type: 'entry' | 'exit', index: number) => {
    onChange({
      ...signals,
      [type]: signals[type].filter((_, i) => i !== index),
    });
  };

  return (
    <div className="signals-builder">
      <h2 style={{ marginTop: 0 }}>Entry & Exit Signals</h2>

      {/* Entry Signals */}
      <section>
        <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', marginBottom: '1rem' }}>
          <h3>Entry Signals</h3>
          <button className="btn-primary" onClick={() => addSignal('entry')} style={{ display: 'flex', alignItems: 'center', gap: '0.5rem' }}>
            <Plus size={18} />
            Add Entry Signal
          </button>
        </div>

        {signals.entry.length === 0 ? (
          <div style={{ textAlign: 'center', padding: '2rem', background: 'var(--background)', borderRadius: '8px', color: 'var(--text-secondary)' }}>
            No entry signals defined. Click "Add Entry Signal" to create one.
          </div>
        ) : (
          <div className="signals-list">
            {signals.entry.map((signal, index) => (
              <div key={index} className="signal-card">
                <div className="signal-header">
                  <h4>Entry Signal #{index + 1}</h4>
                  <button onClick={() => removeSignal('entry', index)} className="btn-icon" style={{ color: 'var(--error)' }}>
                    <Trash2 size={18} />
                  </button>
                </div>
                <p style={{ color: 'var(--text-secondary)', fontSize: '0.9rem', marginBottom: '1rem' }}>
                  Configure conditions for entering a {signal.side} position
                </p>
                <div className="signal-config">
                  <label style={{ fontSize: '0.9rem', fontWeight: 500 }}>
                    Coming soon: Visual condition builder
                  </label>
                </div>
              </div>
            ))}
          </div>
        )}
      </section>

      {/* Exit Signals */}
      <section style={{ marginTop: '2rem' }}>
        <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', marginBottom: '1rem' }}>
          <h3>Exit Signals</h3>
          <button className="btn-primary" onClick={() => addSignal('exit')} style={{ display: 'flex', alignItems: 'center', gap: '0.5rem' }}>
            <Plus size={18} />
            Add Exit Signal
          </button>
        </div>

        {signals.exit.length === 0 ? (
          <div style={{ textAlign: 'center', padding: '2rem', background: 'var(--background)', borderRadius: '8px', color: 'var(--text-secondary)' }}>
            No exit signals defined. Click "Add Exit Signal" to create one.
          </div>
        ) : (
          <div className="signals-list">
            {signals.exit.map((signal, index) => (
              <div key={index} className="signal-card">
                <div className="signal-header">
                  <h4>Exit Signal #{index + 1}</h4>
                  <button onClick={() => removeSignal('exit', index)} className="btn-icon" style={{ color: 'var(--error)' }}>
                    <Trash2 size={18} />
                  </button>
                </div>
                <p style={{ color: 'var(--text-secondary)', fontSize: '0.9rem', marginBottom: '1rem' }}>
                  Configure conditions for exiting a position
                </p>
                <div className="signal-config">
                  <label style={{ fontSize: '0.9rem', fontWeight: 500 }}>
                    Coming soon: Visual condition builder
                  </label>
                </div>
              </div>
            ))}
          </div>
        )}
      </section>

      <style>{`
        .signals-builder {
          max-width: 1000px;
        }

        .signals-list {
          display: flex;
          flex-direction: column;
          gap: 1rem;
        }

        .signal-card {
          background: var(--background);
          border: 1px solid var(--border);
          border-radius: 8px;
          padding: 1.5rem;
        }

        .signal-header {
          display: flex;
          justify-content: space-between;
          align-items: center;
          margin-bottom: 0.5rem;
        }

        .signal-header h4 {
          margin: 0;
        }

        .signal-config {
          padding: 1rem;
          background: var(--card-bg);
          border-radius: 6px;
          border: 1px dashed var(--border);
        }
      `}</style>
    </div>
  );
}
