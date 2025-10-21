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
  Loader2,
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
import '../styles/StrategyBuilder.css';

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
  const [isGeneratingCode, setIsGeneratingCode] = useState(false);
  const [isValidating, setIsValidating] = useState(false);
  const [validation, setValidation] = useState<{
    valid: boolean;
    errors: string[];
    warnings: string[];
  } | null>(null);
  const [backtestResults, setBacktestResults] = useState<BacktestResults | null>(null);
  const [generatedCode, setGeneratedCode] = useState<string>('');
  const [saveSuccess, setSaveSuccess] = useState(false);

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
    setIsValidating(true);
    try {
      const result = await strategyApi.validateStrategy(strategy);
      setValidation(result);
    } catch (error) {
      console.error('Error validating strategy:', error);
      setValidation({ valid: false, errors: ['Validation failed'], warnings: [] });
    } finally {
      setIsValidating(false);
    }
  };

  const handleSave = async () => {
    setIsSaving(true);
    setSaveSuccess(false);
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
      setSaveSuccess(true);

      // Success toast will be shown via saveSuccess state
      setTimeout(() => setSaveSuccess(false), 3000);
    } catch (error: any) {
      // Error will be shown via alert for now (will be replaced with toast in next phase)
      alert(`Error saving strategy: ${error.message}`);
    } finally {
      setIsSaving(false);
    }
  };

  const handleGenerateCode = async () => {
    setIsGeneratingCode(true);
    try {
      const response = await strategyApi.generateCode(
        strategy as Strategy,
        strategy.metadata?.language || 'python'
      );
      setGeneratedCode(response.code);
      setActiveTab('code');
    } catch (error: any) {
      alert(`Error generating code: ${error.message}`);
    } finally {
      setIsGeneratingCode(false);
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
          <h1>Strategy Builder</h1>
          <p>Create and customize your trading strategies</p>
        </div>
        <div className="sb-header-actions">
          {/* Validation Status */}
          {validation && (
            <div className={`sb-validation-status ${validation.valid ? 'valid' : 'invalid'} ${isValidating ? 'validating' : ''}`}>
              {isValidating ? (
                <>
                  <Loader2 size={20} className="spinner" />
                  <span>Validating...</span>
                </>
              ) : validation.valid ? (
                <>
                  <CheckCircle size={20} />
                  <span>Valid</span>
                </>
              ) : (
                <>
                  <AlertCircle size={20} />
                  <span>{validation.errors.length} error{validation.errors.length !== 1 ? 's' : ''}</span>
                </>
              )}
            </div>
          )}

          {/* Action Buttons */}
          <button
            className={`btn-secondary ${isGeneratingCode ? 'btn-loading' : ''}`}
            onClick={handleGenerateCode}
            disabled={isGeneratingCode}
          >
            {!isGeneratingCode && <Code size={18} />}
            {isGeneratingCode ? 'Generating...' : 'Generate Code'}
          </button>
          <button
            className={`btn-primary ${isSaving ? 'btn-loading' : ''} ${saveSuccess ? 'success' : ''}`}
            onClick={handleSave}
            disabled={isSaving || !validation?.valid}
          >
            {!isSaving && !saveSuccess && <Save size={18} />}
            {!isSaving && saveSuccess && <CheckCircle size={18} />}
            {isSaving ? 'Saving...' : saveSuccess ? 'Saved!' : 'Save Strategy'}
          </button>
        </div>
      </div>

      {/* Tabs */}
      <div className="strategy-builder-tabs">
        {tabs.map(({ id, label, icon: Icon }) => (
          <button
            key={id}
            className={`sb-tab ${activeTab === id ? 'active' : ''}`}
            onClick={() => setActiveTab(id)}
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
              <h3 className="flex items-center gap-sm mt-0 mb-sm">
                <AlertCircle size={20} className="text-error" />
                Errors
              </h3>
              <ul>
                {validation.errors.map((error, i) => (
                  <li key={i}>{error}</li>
                ))}
              </ul>
            </div>
          )}
          {validation.warnings.length > 0 && (
            <div className={`warnings ${validation.errors.length > 0 ? 'mt-md' : ''}`}>
              <h3 className="flex items-center gap-sm mt-0 mb-sm">
                <AlertCircle size={20} className="text-warning" />
                Warnings
              </h3>
              <ul>
                {validation.warnings.map((warning, i) => (
                  <li key={i}>{warning}</li>
                ))}
              </ul>
            </div>
          )}
        </div>
      )}
    </div>
  );
}
