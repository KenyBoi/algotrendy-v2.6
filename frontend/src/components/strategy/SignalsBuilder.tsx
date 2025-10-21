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
      <h2 className="mt-0">Entry & Exit Signals</h2>

      {/* Entry Signals */}
      <section>
        <div className="flex justify-between items-center mb-md">
          <h3>Entry Signals</h3>
          <button className="btn-primary" onClick={() => addSignal('entry')}>
            <Plus size={18} />
            Add Entry Signal
          </button>
        </div>

        {signals.entry.length === 0 ? (
          <div className="empty-state">
            <div className="empty-state-title">No entry signals defined</div>
            <div className="empty-state-description">Click "Add Entry Signal" to create one.</div>
          </div>
        ) : (
          <div className="signals-list">
            {signals.entry.map((signal, index) => (
              <div key={index} className="signal-card">
                <div className="signal-header">
                  <h4>Entry Signal #{index + 1}</h4>
                  <button onClick={() => removeSignal('entry', index)} className="btn-icon text-error">
                    <Trash2 size={18} />
                  </button>
                </div>
                <p className="text-secondary mb-md" style={{ fontSize: '0.9rem' }}>
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
      <section className="mt-xl">
        <div className="flex justify-between items-center mb-md">
          <h3>Exit Signals</h3>
          <button className="btn-primary" onClick={() => addSignal('exit')}>
            <Plus size={18} />
            Add Exit Signal
          </button>
        </div>

        {signals.exit.length === 0 ? (
          <div className="empty-state">
            <div className="empty-state-title">No exit signals defined</div>
            <div className="empty-state-description">Click "Add Exit Signal" to create one.</div>
          </div>
        ) : (
          <div className="signals-list">
            {signals.exit.map((signal, index) => (
              <div key={index} className="signal-card">
                <div className="signal-header">
                  <h4>Exit Signal #{index + 1}</h4>
                  <button onClick={() => removeSignal('exit', index)} className="btn-icon text-error">
                    <Trash2 size={18} />
                  </button>
                </div>
                <p className="text-secondary mb-md" style={{ fontSize: '0.9rem' }}>
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
    </div>
  );
}
