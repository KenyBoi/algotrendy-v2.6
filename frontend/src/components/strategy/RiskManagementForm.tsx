import { StrategyRiskManagement } from '../../types/strategy';

interface Props {
  riskManagement: StrategyRiskManagement;
  onChange: (riskManagement: StrategyRiskManagement) => void;
}

export default function RiskManagementForm({ riskManagement, onChange }: Props) {
  return (
    <div className="risk-management-form">
      <h2 style={{ marginTop: 0 }}>Risk Management</h2>

      {/* Stop Loss */}
      <section className="risk-section">
        <div className="risk-header">
          <h3>Stop Loss</h3>
          <label className="toggle">
            <input
              type="checkbox"
              checked={riskManagement.stopLoss?.enabled || false}
              onChange={e => onChange({
                ...riskManagement,
                stopLoss: { ...riskManagement.stopLoss!, enabled: e.target.checked }
              })}
            />
            <span className="toggle-slider"></span>
          </label>
        </div>

        {riskManagement.stopLoss?.enabled && (
          <div className="risk-config">
            <div className="form-group">
              <label>Stop Loss Type</label>
              <select
                value={riskManagement.stopLoss.type}
                onChange={e => onChange({
                  ...riskManagement,
                  stopLoss: { ...riskManagement.stopLoss!, type: e.target.value as any }
                })}
                className="form-input"
              >
                <option value="fixed">Fixed %</option>
                <option value="trailing">Trailing %</option>
                <option value="atr">ATR Multiple</option>
              </select>
            </div>

            <div className="form-group">
              <label>Value (%)</label>
              <input
                type="number"
                value={riskManagement.stopLoss.value}
                onChange={e => onChange({
                  ...riskManagement,
                  stopLoss: { ...riskManagement.stopLoss!, value: parseFloat(e.target.value) }
                })}
                className="form-input"
                min="0"
                max="100"
                step="0.1"
              />
            </div>
          </div>
        )}
      </section>

      {/* Take Profit */}
      <section className="risk-section">
        <div className="risk-header">
          <h3>Take Profit</h3>
          <label className="toggle">
            <input
              type="checkbox"
              checked={riskManagement.takeProfit?.enabled || false}
              onChange={e => onChange({
                ...riskManagement,
                takeProfit: { ...riskManagement.takeProfit!, enabled: e.target.checked }
              })}
            />
            <span className="toggle-slider"></span>
          </label>
        </div>

        {riskManagement.takeProfit?.enabled && (
          <div className="risk-config">
            <div className="form-group">
              <label>Take Profit Type</label>
              <select
                value={riskManagement.takeProfit.type}
                onChange={e => onChange({
                  ...riskManagement,
                  takeProfit: { ...riskManagement.takeProfit!, type: e.target.value as any }
                })}
                className="form-input"
              >
                <option value="fixed">Fixed %</option>
                <option value="trailing">Trailing %</option>
              </select>
            </div>

            <div className="form-group">
              <label>Value (%)</label>
              <input
                type="number"
                value={riskManagement.takeProfit.value}
                onChange={e => onChange({
                  ...riskManagement,
                  takeProfit: { ...riskManagement.takeProfit!, value: parseFloat(e.target.value) }
                })}
                className="form-input"
                min="0"
                max="1000"
                step="0.1"
              />
            </div>
          </div>
        )}
      </section>

      {/* Position Sizing */}
      <section className="risk-section">
        <h3>Position Sizing</h3>
        <div className="risk-config">
          <div className="form-group">
            <label>Sizing Method</label>
            <select
              value={riskManagement.positionSize?.type}
              onChange={e => onChange({
                ...riskManagement,
                positionSize: { ...riskManagement.positionSize!, type: e.target.value as any }
              })}
              className="form-input"
            >
              <option value="fixed">Fixed Amount</option>
              <option value="percentage">Percentage of Portfolio</option>
              <option value="kelly">Kelly Criterion</option>
              <option value="risk_based">Risk-Based Sizing</option>
            </select>
          </div>

          <div className="form-group">
            <label>Value (%)</label>
            <input
              type="number"
              value={riskManagement.positionSize?.value}
              onChange={e => onChange({
                ...riskManagement,
                positionSize: { ...riskManagement.positionSize!, value: parseFloat(e.target.value) }
              })}
              className="form-input"
              min="0"
              max="100"
              step="0.1"
            />
          </div>

          <div className="form-group">
            <label>Max Drawdown (%)</label>
            <input
              type="number"
              value={riskManagement.maxDrawdown || ''}
              onChange={e => onChange({
                ...riskManagement,
                maxDrawdown: parseFloat(e.target.value) || undefined
              })}
              className="form-input"
              placeholder="Optional"
              min="0"
              max="100"
              step="1"
            />
          </div>

          <div className="form-group">
            <label>Max Positions</label>
            <input
              type="number"
              value={riskManagement.maxPositions || ''}
              onChange={e => onChange({
                ...riskManagement,
                maxPositions: parseInt(e.target.value) || undefined
              })}
              className="form-input"
              placeholder="Optional"
              min="1"
              max="100"
            />
          </div>
        </div>
      </section>

      <style>{`
        .risk-management-form {
          max-width: 1000px;
        }

        .risk-section {
          background: var(--background);
          border: 1px solid var(--border);
          border-radius: 8px;
          padding: 1.5rem;
          margin-bottom: 1.5rem;
        }

        .risk-header {
          display: flex;
          justify-content: space-between;
          align-items: center;
          margin-bottom: 1rem;
        }

        .risk-header h3 {
          margin: 0;
        }

        .risk-config {
          display: grid;
          grid-template-columns: repeat(auto-fit, minmax(200px, 1fr));
          gap: 1rem;
          padding-top: 1rem;
          border-top: 1px solid var(--border);
        }

        .toggle {
          position: relative;
          display: inline-block;
          width: 50px;
          height: 24px;
        }

        .toggle input {
          opacity: 0;
          width: 0;
          height: 0;
        }

        .toggle-slider {
          position: absolute;
          cursor: pointer;
          top: 0;
          left: 0;
          right: 0;
          bottom: 0;
          background-color: var(--border);
          transition: 0.4s;
          border-radius: 24px;
        }

        .toggle-slider:before {
          position: absolute;
          content: "";
          height: 18px;
          width: 18px;
          left: 3px;
          bottom: 3px;
          background-color: white;
          transition: 0.4s;
          border-radius: 50%;
        }

        .toggle input:checked + .toggle-slider {
          background-color: var(--primary);
        }

        .toggle input:checked + .toggle-slider:before {
          transform: translateX(26px);
        }
      `}</style>
    </div>
  );
}
