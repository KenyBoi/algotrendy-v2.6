import { StrategyMetadata, StrategyCategory, StrategyStatus } from '../../types/strategy';
import { Plus, X } from 'lucide-react';
import { useState } from 'react';

interface Props {
  metadata: StrategyMetadata;
  onChange: (metadata: StrategyMetadata) => void;
}

export default function MetadataForm({ metadata, onChange }: Props) {
  const [newTag, setNewTag] = useState('');

  const categories: { value: StrategyCategory; label: string }[] = [
    { value: 'momentum', label: 'Momentum' },
    { value: 'mean_reversion', label: 'Mean Reversion' },
    { value: 'carry', label: 'Carry Trade' },
    { value: 'arbitrage', label: 'Arbitrage' },
    { value: 'trend_following', label: 'Trend Following' },
    { value: 'breakout', label: 'Breakout' },
    { value: 'custom', label: 'Custom' },
  ];

  const statuses: { value: StrategyStatus; label: string }[] = [
    { value: 'experimental', label: 'Experimental' },
    { value: 'backtested', label: 'Backtested' },
    { value: 'active', label: 'Active' },
    { value: 'deprecated', label: 'Deprecated' },
  ];

  const handleAddTag = () => {
    if (newTag.trim() && !metadata.tags.includes(newTag.trim())) {
      onChange({
        ...metadata,
        tags: [...metadata.tags, newTag.trim()],
      });
      setNewTag('');
    }
  };

  const handleRemoveTag = (tag: string) => {
    onChange({
      ...metadata,
      tags: metadata.tags.filter(t => t !== tag),
    });
  };

  return (
    <div className="metadata-form">
      <h2 className="mt-0">Strategy Information</h2>

      <div className="form-grid">
        <div className="form-group">
          <label>Strategy Name *</label>
          <input
            type="text"
            value={metadata.name}
            onChange={e => onChange({ ...metadata, name: e.target.value })}
            placeholder="e.g., volatility_managed_momentum"
            className="form-input"
          />
          <small>Technical name (lowercase, underscores)</small>
        </div>

        <div className="form-group">
          <label>Display Name *</label>
          <input
            type="text"
            value={metadata.displayName}
            onChange={e => onChange({ ...metadata, displayName: e.target.value })}
            placeholder="e.g., Volatility-Managed Momentum"
            className="form-input"
          />
          <small>Human-readable name</small>
        </div>

        <div className="form-group full-width">
          <label>Description *</label>
          <textarea
            value={metadata.description}
            onChange={e => onChange({ ...metadata, description: e.target.value })}
            placeholder="Describe your strategy's approach, logic, and expected performance..."
            className="form-input"
            rows={4}
          />
        </div>

        <div className="form-group">
          <label>Category *</label>
          <select
            value={metadata.category}
            onChange={e => onChange({ ...metadata, category: e.target.value as StrategyCategory })}
            className="form-input"
          >
            {categories.map(cat => (
              <option key={cat.value} value={cat.value}>
                {cat.label}
              </option>
            ))}
          </select>
        </div>

        <div className="form-group">
          <label>Implementation Language *</label>
          <select
            value={metadata.language}
            onChange={e => onChange({ ...metadata, language: e.target.value as 'python' | 'csharp' })}
            className="form-input"
          >
            <option value="python">Python</option>
            <option value="csharp">C#</option>
          </select>
        </div>

        <div className="form-group">
          <label>Status</label>
          <select
            value={metadata.status}
            onChange={e => onChange({ ...metadata, status: e.target.value as StrategyStatus })}
            className="form-input"
          >
            {statuses.map(status => (
              <option key={status.value} value={status.value}>
                {status.label}
              </option>
            ))}
          </select>
        </div>

        <div className="form-group full-width">
          <label>Tags</label>
          <div className="tags-input-wrapper">
            <div className="tags-display">
              {metadata.tags.map(tag => (
                <span key={tag} className="tag">
                  {tag}
                  <button onClick={() => handleRemoveTag(tag)} className="tag-remove">
                    <X size={14} />
                  </button>
                </span>
              ))}
            </div>
            <div className="tags-add">
              <input
                type="text"
                value={newTag}
                onChange={e => setNewTag(e.target.value)}
                onKeyPress={e => e.key === 'Enter' && handleAddTag()}
                placeholder="Add a tag..."
                className="form-input mb-0"
              />
              <button onClick={handleAddTag} className="btn-icon">
                <Plus size={20} />
              </button>
            </div>
          </div>
          <small>Press Enter or click + to add tags</small>
        </div>
      </div>
    </div>
  );
}
