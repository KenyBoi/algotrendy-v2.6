import { PatternAnalysis } from '../lib/api';

interface Props {
  patterns: PatternAnalysis | null;
}

export default function PatternOpportunities({ patterns }: Props) {
  if (!patterns) {
    return (
      <div className="card">
        <div className="card-header">
          <h3 className="card-title">🎯 Pattern Opportunities</h3>
        </div>
        <div style={{ textAlign: 'center', padding: '3rem', color: 'var(--text-secondary)' }}>
          <p>Loading pattern analysis...</p>
        </div>
      </div>
    );
  }

  const { opportunities } = patterns;

  return (
    <div className="card">
      <div className="card-header">
        <h3 className="card-title">🎯 Pattern Opportunities</h3>
        <span className="badge badge-success">
          {opportunities.length} Active
        </span>
      </div>

      <div style={{ maxHeight: '400px', overflowY: 'auto' }}>
        {opportunities.map((opp, index) => (
          <div
            key={opp.symbol}
            style={{
              padding: '1rem',
              borderBottom: index < opportunities.length - 1 ? '1px solid var(--border)' : 'none',
            }}
          >
            <div style={{ display: 'flex', justifyContent: 'space-between', marginBottom: '0.5rem' }}>
              <div>
                <span style={{ fontWeight: 600, fontSize: '1.125rem' }}>{opp.symbol}</span>
                <span style={{ marginLeft: '0.5rem', color: 'var(--text-secondary)' }}>
                  ${opp.price.toLocaleString()}
                </span>
              </div>
              <div style={{ textAlign: 'right' }}>
                <div style={{ fontSize: '0.875rem', color: 'var(--text-secondary)' }}>
                  RSI: {opp.rsi.toFixed(2)}
                </div>
                <div style={{
                  fontSize: '1rem',
                  fontWeight: 600,
                  color: opp.reversalConfidence > 0.7 ? 'var(--success)' : 'var(--warning)'
                }}>
                  {(opp.reversalConfidence * 100).toFixed(1)}% Confidence
                </div>
              </div>
            </div>

            <div style={{ display: 'flex', gap: '0.5rem', flexWrap: 'wrap' }}>
              {opp.patterns.map((pattern, i) => (
                <div
                  key={i}
                  style={{
                    padding: '0.25rem 0.75rem',
                    borderRadius: '4px',
                    fontSize: '0.75rem',
                    backgroundColor: pattern.signal === 'BUY'
                      ? 'rgba(16, 185, 129, 0.2)'
                      : 'rgba(239, 68, 68, 0.2)',
                    color: pattern.signal === 'BUY' ? 'var(--success)' : 'var(--danger)',
                  }}
                >
                  {pattern.type} ({(pattern.confidence * 100).toFixed(0)}%)
                </div>
              ))}
            </div>

            {opp.patterns.length > 0 && (
              <div style={{
                marginTop: '0.5rem',
                padding: '0.5rem',
                backgroundColor: 'var(--bg-tertiary)',
                borderRadius: '4px',
                fontSize: '0.875rem',
                color: 'var(--text-secondary)'
              }}>
                {opp.patterns[0].reason}
              </div>
            )}
          </div>
        ))}

        {opportunities.length === 0 && (
          <div style={{ textAlign: 'center', padding: '3rem', color: 'var(--text-secondary)' }}>
            <p>No trading opportunities detected at this time.</p>
            <p style={{ fontSize: '0.875rem', marginTop: '0.5rem' }}>
              The ML model continuously scans the market for reversal patterns.
            </p>
          </div>
        )}
      </div>
    </div>
  );
}
