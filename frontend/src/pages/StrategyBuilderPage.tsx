import { useState, useEffect } from 'react';
import {
  Play,
  Save,
  Copy,
  Trash2,
  Settings,
  TrendingUp,
  AlertCircle,
  CheckCircle,
  Code,
  BarChart3,
  Plus,
  X,
} from 'lucide-react';
import type {
  Strategy,
  StrategyMetadata,
  StrategyParameter,
  StrategySignal,
  StrategyRiskManagement,
  StrategyCondition,
  BacktestConfig,
  BacktestResults,
} from '../types/strategy';
import { strategyApi } from '../lib/strategyApi';
import MetadataForm from '../components/strategy/MetadataForm';
import ParametersForm from '../components/strategy/ParametersForm';
import SignalsBuilder from '../components/strategy/SignalsBuilder';
import RiskManagementForm from '../components/strategy/RiskManagementForm';
import BacktestPanel from '../components/strategy/BacktestPanel';
import StrategyCodeViewer from '../components/strategy/StrategyCodeViewer';

type BuilderTab = 'metadata' | 'parameters' | 'signals' | 'risk' | 'backtest' | 'code';

export default function StrategyBuilderPage() {
  const [activeTab, setActiveTab] = useState<BuilderTab>('metadata');
  const [strategy, setStrategy] = useState<Partial<Strategy>>({
    metadata: {
      name: '',
      displayName: '',
      description: '',
      category: 'momentum',
      tags: [],
      language: 'python',
      status: 'experimental',
    },
    parameters: [],
    signals: {
      entry: [],
      exit: [],
    },
    riskManagement: {
      stopLoss: { enabled: false, type: 'fixed', value: 0 },
      takeProfit: { enabled: false, type: 'fixed', value: 0 },
      positionSize: { type: 'percentage', value: 10 },
    },
  });

  const [isSaving, setIsSaving] = useState(false);
  const [validation, setValidation] = useState<{
    valid: boolean;
    errors: string[];
    warnings: string[];
  } | null>(null);
  const [backtestResults, setBacktestResults] = useState<BacktestResults | null>(null);
  const [generatedCode, setGeneratedCode] = useState<string>('');

  const tabs: Array<{ id: BuilderTab; label: string; icon: any }> = [
    { id: 'metadata', label: 'Strategy Info', icon: Settings },
    { id: 'parameters', label: 'Parameters', icon: TrendingUp },
    { id: 'signals', label: 'Entry/Exit Signals', icon: Play },
    { id: 'risk', label: 'Risk Management', icon: AlertCircle },
    { id: 'backtest', label: 'Backtest', icon: BarChart3 },
    { id: 'code', label: 'Generated Code', icon: Code },
  ];

  useEffect(() => {
    // Validate strategy whenever it changes
    if (strategy.metadata?.name) {
      validateStrategy();
    }
  }, [strategy]);

  const validateStrategy = async () => {
    try {
      const result = await strategyApi.validateStrategy(strategy);
      setValidation(result);
    } catch (error) {
      console.error('Error validating strategy:', error);
    }
  };

  const handleSave = async () => {
    setIsSaving(true);
    try {
      if (!strategy.metadata) return;

      const createRequest = {
        metadata: strategy.metadata as StrategyMetadata,
        parameters: strategy.parameters?.reduce((acc, param) => {
          acc[param.name] = param.value;
          return acc;
        }, {} as Record<string, any>),
      };

      const response = await strategyApi.createStrategy(createRequest);
      alert(`Strategy "${response.strategy.metadata.displayName}" saved successfully!`);
    } catch (error: any) {
      alert(`Error saving strategy: ${error.message}`);
    } finally {
      setIsSaving(false);
    }
  };

  const handleGenerateCode = async () => {
    try {
      const response = await strategyApi.generateCode(
        strategy as Strategy,
        strategy.metadata?.language || 'python'
      );
      setGeneratedCode(response.code);
      setActiveTab('code');
    } catch (error: any) {
      alert(`Error generating code: ${error.message}`);
    }
  };

  const handleRunBacktest = async (config: BacktestConfig) => {
    try {
      // First save the strategy if it has an ID, or use the current strategy
      // For now, we'll use a placeholder ID
      const backtestId = 'temp-' + Date.now();

      // In a real implementation, this would call the backend
      // For now, show a loading state
      alert('Backtest started. This feature requires backend integration.');

    } catch (error: any) {
      alert(`Error running backtest: ${error.message}`);
    }
  };

  const updateMetadata = (metadata: StrategyMetadata) => {
    setStrategy({ ...strategy, metadata });
  };

  const updateParameters = (parameters: StrategyParameter[]) => {
    setStrategy({ ...strategy, parameters });
  };

  const updateSignals = (signals: { entry: StrategySignal[]; exit: StrategySignal[] }) => {
    setStrategy({ ...strategy, signals });
  };

  const updateRiskManagement = (riskManagement: StrategyRiskManagement) => {
    setStrategy({ ...strategy, riskManagement });
  };

  return (
    <div className="strategy-builder">
      {/* Header */}
      <div className="strategy-builder-header">
        <div>
          <h1 style={{ margin: 0, marginBottom: '0.5rem' }}>Strategy Builder</h1>
          <p style={{ color: 'var(--text-secondary)', margin: 0 }}>
            Create and customize your trading strategies
          </p>
        </div>
        <div style={{ display: 'flex', gap: '1rem', alignItems: 'center' }}>
          {/* Validation Status */}
          {validation && (
            <div style={{ display: 'flex', alignItems: 'center', gap: '0.5rem' }}>
              {validation.valid ? (
                <>
                  <CheckCircle size={20} color="var(--success)" />
                  <span style={{ color: 'var(--success)' }}>Valid</span>
                </>
              ) : (
                <>
                  <AlertCircle size={20} color="var(--warning)" />
                  <span style={{ color: 'var(--warning)' }}>
                    {validation.errors.length} errors
                  </span>
                </>
              )}
            </div>
          )}

          {/* Action Buttons */}
          <button
            className="btn-secondary"
            onClick={handleGenerateCode}
            style={{ display: 'flex', alignItems: 'center', gap: '0.5rem' }}
          >
            <Code size={18} />
            Generate Code
          </button>
          <button
            className="btn-primary"
            onClick={handleSave}
            disabled={isSaving || !validation?.valid}
            style={{ display: 'flex', alignItems: 'center', gap: '0.5rem' }}
          >
            <Save size={18} />
            {isSaving ? 'Saving...' : 'Save Strategy'}
          </button>
        </div>
      </div>

      {/* Tabs */}
      <div className="strategy-builder-tabs">
        {tabs.map(({ id, label, icon: Icon }) => (
          <button
            key={id}
            className={`tab ${activeTab === id ? 'active' : ''}`}
            onClick={() => setActiveTab(id)}
            style={{
              display: 'flex',
              alignItems: 'center',
              gap: '0.5rem',
              padding: '1rem 1.5rem',
              background: activeTab === id ? 'var(--card-bg)' : 'transparent',
              border: 'none',
              borderBottom: activeTab === id ? '2px solid var(--primary)' : '2px solid transparent',
              color: activeTab === id ? 'var(--primary)' : 'var(--text-secondary)',
              cursor: 'pointer',
              fontSize: '0.95rem',
              fontWeight: 500,
            }}
          >
            <Icon size={18} />
            {label}
          </button>
        ))}
      </div>

      {/* Content */}
      <div className="strategy-builder-content">
        {activeTab === 'metadata' && (
          <MetadataForm
            metadata={strategy.metadata as StrategyMetadata}
            onChange={updateMetadata}
          />
        )}

        {activeTab === 'parameters' && (
          <ParametersForm
            parameters={strategy.parameters || []}
            onChange={updateParameters}
          />
        )}

        {activeTab === 'signals' && (
          <SignalsBuilder
            signals={strategy.signals || { entry: [], exit: [] }}
            onChange={updateSignals}
          />
        )}

        {activeTab === 'risk' && (
          <RiskManagementForm
            riskManagement={strategy.riskManagement as StrategyRiskManagement}
            onChange={updateRiskManagement}
          />
        )}

        {activeTab === 'backtest' && (
          <BacktestPanel
            onRunBacktest={handleRunBacktest}
            results={backtestResults}
          />
        )}

        {activeTab === 'code' && (
          <StrategyCodeViewer
            code={generatedCode}
            language={strategy.metadata?.language || 'python'}
          />
        )}
      </div>

      {/* Validation Messages */}
      {validation && (validation.errors.length > 0 || validation.warnings.length > 0) && (
        <div className="validation-panel">
          {validation.errors.length > 0 && (
            <div className="errors">
              <h3 style={{ margin: 0, marginBottom: '0.5rem', display: 'flex', alignItems: 'center', gap: '0.5rem' }}>
                <AlertCircle size={20} color="var(--error)" />
                Errors
              </h3>
              <ul style={{ margin: 0, paddingLeft: '1.5rem' }}>
                {validation.errors.map((error, i) => (
                  <li key={i} style={{ color: 'var(--error)' }}>{error}</li>
                ))}
              </ul>
            </div>
          )}
          {validation.warnings.length > 0 && (
            <div className="warnings" style={{ marginTop: validation.errors.length > 0 ? '1rem' : 0 }}>
              <h3 style={{ margin: 0, marginBottom: '0.5rem', display: 'flex', alignItems: 'center', gap: '0.5rem' }}>
                <AlertCircle size={20} color="var(--warning)" />
                Warnings
              </h3>
              <ul style={{ margin: 0, paddingLeft: '1.5rem' }}>
                {validation.warnings.map((warning, i) => (
                  <li key={i} style={{ color: 'var(--warning)' }}>{warning}</li>
                ))}
              </ul>
            </div>
          )}
        </div>
      )}

      <style>{`
        .strategy-builder {
          padding: 2rem;
          max-width: 1400px;
          margin: 0 auto;
        }

        .strategy-builder-header {
          display: flex;
          justify-content: space-between;
          align-items: flex-start;
          margin-bottom: 2rem;
        }

        .strategy-builder-tabs {
          display: flex;
          border-bottom: 1px solid var(--border);
          margin-bottom: 2rem;
          overflow-x: auto;
        }

        .strategy-builder-content {
          background: var(--card-bg);
          border-radius: 8px;
          padding: 2rem;
          min-height: 500px;
        }

        .validation-panel {
          margin-top: 2rem;
          padding: 1.5rem;
          background: var(--card-bg);
          border-radius: 8px;
          border-left: 4px solid var(--warning);
        }

        .btn-primary, .btn-secondary {
          padding: 0.75rem 1.5rem;
          border-radius: 6px;
          border: none;
          cursor: pointer;
          font-weight: 500;
          transition: all 0.2s;
        }

        .btn-primary {
          background: var(--primary);
          color: white;
        }

        .btn-primary:hover:not(:disabled) {
          opacity: 0.9;
        }

        .btn-primary:disabled {
          opacity: 0.5;
          cursor: not-allowed;
        }

        .btn-secondary {
          background: var(--card-bg);
          color: var(--text);
          border: 1px solid var(--border);
        }

        .btn-secondary:hover {
          background: var(--background);
        }
      `}</style>
    </div>
  );
}
