// Strategy Types for Strategy Builder

export interface StrategyMetadata {
  id?: string;
  name: string;
  displayName: string;
  description: string;
  category: StrategyCategory;
  tags: string[];
  language: 'python' | 'csharp';
  status: StrategyStatus;
  createdAt?: string;
  updatedAt?: string;
}

export type StrategyCategory =
  | 'momentum'
  | 'mean_reversion'
  | 'carry'
  | 'arbitrage'
  | 'trend_following'
  | 'breakout'
  | 'custom';

export type StrategyStatus = 'experimental' | 'backtested' | 'active' | 'deprecated';

export interface StrategyParameter {
  name: string;
  type: 'number' | 'string' | 'boolean' | 'select';
  value: any;
  default?: any;
  min?: number;
  max?: number;
  step?: number;
  options?: string[];
  description?: string;
  required?: boolean;
}

export interface StrategyCondition {
  id: string;
  type: 'indicator' | 'price' | 'volume' | 'time' | 'custom';
  indicator?: string;
  operator: '>' | '<' | '=' | '>=' | '<=' | '!=' | 'crosses_above' | 'crosses_below';
  value: number | string;
  timeframe?: string;
}

export interface StrategySignal {
  type: 'entry' | 'exit';
  side: 'buy' | 'sell';
  conditions: StrategyCondition[];
  logic: 'all' | 'any'; // AND or OR logic
}

export interface StrategyRiskManagement {
  stopLoss?: {
    enabled: boolean;
    type: 'fixed' | 'trailing' | 'atr';
    value: number;
  };
  takeProfit?: {
    enabled: boolean;
    type: 'fixed' | 'trailing';
    value: number;
  };
  positionSize?: {
    type: 'fixed' | 'percentage' | 'kelly' | 'risk_based';
    value: number;
  };
  maxDrawdown?: number;
  maxPositions?: number;
}

export interface BacktestConfig {
  symbol: string;
  timeframe: string;
  startDate: string;
  endDate: string;
  initialCapital: number;
  commission?: number;
  slippage?: number;
}

export interface BacktestResults {
  totalReturn: number;
  cagr: number;
  sharpeRatio: number;
  sortinoRatio: number;
  maxDrawdown: number;
  winRate: number;
  profitFactor: number;
  totalTrades: number;
  averageWin: number;
  averageLoss: number;
  largestWin: number;
  largestLoss: number;
  equity: { timestamp: string; value: number }[];
  trades: Trade[];
}

export interface Trade {
  id: string;
  timestamp: string;
  symbol: string;
  side: 'buy' | 'sell';
  type: 'entry' | 'exit';
  price: number;
  quantity: number;
  pnl?: number;
  pnlPercent?: number;
}

export interface Strategy {
  metadata: StrategyMetadata;
  parameters: StrategyParameter[];
  signals: {
    entry: StrategySignal[];
    exit: StrategySignal[];
  };
  riskManagement: StrategyRiskManagement;
  code?: string; // For custom code strategies
  backtestResults?: BacktestResults;
}

export interface StrategyTemplate {
  id: string;
  name: string;
  description: string;
  category: StrategyCategory;
  strategy: Partial<Strategy>;
}

// API Response Types
export interface StrategyListItem {
  id: string;
  displayName: string;
  category: StrategyCategory;
  status: StrategyStatus;
  sharpeRatio?: number;
  winRate?: number;
  totalTrades: number;
  createdAt: string;
}

export interface CreateStrategyRequest {
  metadata: StrategyMetadata;
  parameters?: Record<string, any>;
  academicFoundation?: {
    paperTitle?: string;
    authors?: string;
    year?: number;
    journal?: string;
  };
  backtestResults?: Record<string, any>;
}

export interface UpdateStrategyRequest extends Partial<CreateStrategyRequest> {
  id: string;
}
