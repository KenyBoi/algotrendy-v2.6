import { api } from './api';
import type { ApiResponse } from '../types';

// ============================================
// MEM (AI Trading Intelligence) Service
// ============================================
// MEM is AlgoTrendy's proprietary AI system that acts as
// both CEO and Executive Assistant, making millisecond
// trading decisions using ML and advanced algorithms.
// ============================================

// MEM Status Types
export type MEMStatus = 'Active' | 'Idle' | 'Analyzing' | 'Trading' | 'Paused' | 'Error' | 'Offline';
export type MEMConfidence = 'Low' | 'Medium' | 'High' | 'Very High';
export type MEMAutonomyLevel = 'Manual' | 'SemiAutonomous' | 'FullyAutonomous';

export interface MEMStatusResponse {
  status: MEMStatus;
  uptime: number;
  lastActivity: string;
  activeStrategies: number;
  openPositions: number;
  todayPnL: number;
  isTrading: boolean;
  healthScore: number; // 0-100
}

export interface MEMActivity {
  id: string;
  timestamp: string;
  type: 'Trade' | 'Decision' | 'Alert' | 'Analysis' | 'Optimization';
  title: string;
  description: string;
  confidence: MEMConfidence;
  impact: 'Low' | 'Medium' | 'High';
  symbol?: string;
  strategy?: string;
  metadata?: Record<string, any>;
}

export interface MEMOpportunity {
  id: string;
  symbol: string;
  type: 'Buy' | 'Sell' | 'Hedge';
  confidence: number; // 0-100
  reasoning: string;
  expectedReturn: number;
  risk: number;
  timeframe: string;
  detectedAt: string;
  expiresAt: string;
  signals: MEMSignal[];
}

export interface MEMSignal {
  name: string;
  value: number;
  threshold: number;
  strength: number; // 0-100
}

export interface MEMDecision {
  id: string;
  timestamp: string;
  type: 'Entry' | 'Exit' | 'Adjust' | 'Hold';
  symbol: string;
  action: string;
  reasoning: string[];
  confidence: number;
  dataPoints: MEMDataPoint[];
  executionTime: number; // milliseconds
  outcome?: 'Pending' | 'Success' | 'Failed';
}

export interface MEMDataPoint {
  label: string;
  value: string | number;
  weight: number; // How important this was to the decision
}

export interface MEMInsight {
  id: string;
  timestamp: string;
  category: 'Market' | 'Strategy' | 'Risk' | 'Performance' | 'Opportunity';
  title: string;
  message: string;
  priority: 'Low' | 'Medium' | 'High' | 'Urgent';
  actionable: boolean;
  suggestedAction?: string;
  relatedSymbols?: string[];
}

export interface MEMChatRequest {
  message: string;
  context?: {
    currentView?: string;
    symbol?: string;
    strategyId?: string;
    timeframe?: string;
  };
  requestAnalysis?: boolean;
}

export interface MEMChatResponse {
  message: string;
  confidence?: number;
  dataSupport?: MEMDataPoint[];
  visualizations?: any[];
  suggestedActions?: string[];
  followUpQuestions?: string[];
}

export interface MEMStrategyValidation {
  isValid: boolean;
  confidence: number;
  issues: string[];
  warnings: string[];
  suggestions: string[];
  estimatedPerformance?: {
    expectedReturn: number;
    risk: number;
    sharpeRatio: number;
    maxDrawdown: number;
  };
}

export interface MEMStrategyOptimization {
  originalStrategy: any;
  optimizedStrategy: any;
  improvements: MEMImprovement[];
  backtestComparison: {
    original: any;
    optimized: any;
    improvement: number; // percentage
  };
  reasoning: string;
}

export interface MEMImprovement {
  parameter: string;
  originalValue: any;
  optimizedValue: any;
  expectedImpact: string;
  reasoning: string;
}

export interface MEMConfig {
  autonomyLevel: MEMAutonomyLevel;
  riskLimits: {
    maxPositionSize: number;
    maxDailyLoss: number;
    maxDrawdown: number;
    maxLeverage: number;
  };
  tradingHours: {
    enabled: boolean;
    start: string; // HH:mm
    end: string; // HH:mm
    timezone: string;
  };
  permissions: {
    canTrade: boolean;
    requiresApproval: boolean;
    maxTradeSize: number;
    allowedAssetClasses: string[];
  };
  alertPreferences: {
    emailAlerts: boolean;
    smsAlerts: boolean;
    pushNotifications: boolean;
    alertThreshold: 'Low' | 'Medium' | 'High';
  };
}

export interface MEMPerformance {
  period: string;
  totalPnL: number;
  winRate: number;
  totalTrades: number;
  avgWin: number;
  avgLoss: number;
  sharpeRatio: number;
  maxDrawdown: number;
  profitFactor: number;
  decisionAccuracy: number; // MEM's prediction accuracy
  executionQuality: number; // How well MEM executed vs. intended
  learningProgress: number; // ML model improvement over time
}

export interface MEMLearningCurve {
  date: string;
  accuracy: number;
  sharpeRatio: number;
  winRate: number;
  confidence: number;
}

export interface MEMDataset {
  id: string;
  name: string;
  description: string;
  category: string;
  sources: string[];
  updateFrequency: string;
  recordCount: number;
  lastUpdated: string;
  isProprietary: boolean;
  accessLevel: 'Public' | 'Premium' | 'Exclusive';
}

// ============================================
// MEM Service Functions
// ============================================

export const memService = {
  // Status & Activity
  async getStatus(): Promise<ApiResponse<MEMStatusResponse>> {
    return api.get<MEMStatusResponse>('/mem/status');
  },

  async getActivityFeed(limit: number = 50): Promise<ApiResponse<MEMActivity[]>> {
    return api.get<MEMActivity[]>(`/mem/activity-feed?limit=${limit}`);
  },

  async getHeartbeat(): Promise<ApiResponse<{ timestamp: string; healthy: boolean }>> {
    return api.get('/mem/heartbeat');
  },

  // Intelligence & Insights
  async chat(request: MEMChatRequest): Promise<ApiResponse<MEMChatResponse>> {
    return api.post<MEMChatResponse>('/mem/chat', request);
  },

  async analyze(data: { symbol?: string; strategy?: string; timeframe?: string }): Promise<ApiResponse<any>> {
    return api.post('/mem/analyze', data);
  },

  async getInsights(): Promise<ApiResponse<MEMInsight[]>> {
    return api.get<MEMInsight[]>('/mem/insights');
  },

  async getOpportunities(): Promise<ApiResponse<MEMOpportunity[]>> {
    return api.get<MEMOpportunity[]>('/mem/opportunities');
  },

  async getDecisionHistory(limit: number = 100): Promise<ApiResponse<MEMDecision[]>> {
    return api.get<MEMDecision[]>(`/mem/recent-decisions?limit=${limit}`);
  },

  // Strategy Management
  async validateStrategy(strategy: any): Promise<ApiResponse<MEMStrategyValidation>> {
    return api.post<MEMStrategyValidation>('/mem/validate-strategy', strategy);
  },

  async optimizeStrategy(strategy: any): Promise<ApiResponse<MEMStrategyOptimization>> {
    return api.post<MEMStrategyOptimization>('/mem/optimize-strategy', strategy);
  },

  async deployStrategy(strategyId: string): Promise<ApiResponse<{ success: boolean; message: string }>> {
    return api.post('/mem/deploy-strategy', { strategyId });
  },

  async getActiveStrategies(): Promise<ApiResponse<any[]>> {
    return api.get('/mem/active-strategies');
  },

  // Trading Control
  async pauseTrading(): Promise<ApiResponse<{ success: boolean; message: string }>> {
    return api.post('/mem/pause-trading');
  },

  async resumeTrading(): Promise<ApiResponse<{ success: boolean; message: string }>> {
    return api.post('/mem/resume-trading');
  },

  async emergencyStop(): Promise<ApiResponse<{ success: boolean; actionsToken: string[] }>> {
    return api.post('/mem/emergency-stop');
  },

  async getTradingState(): Promise<ApiResponse<any>> {
    return api.get('/mem/trading-state');
  },

  // Performance & Analytics
  async getPerformanceSummary(period?: string): Promise<ApiResponse<MEMPerformance>> {
    const query = period ? `?period=${period}` : '';
    return api.get<MEMPerformance>(`/mem/performance/summary${query}`);
  },

  async getLearningCurve(days: number = 30): Promise<ApiResponse<MEMLearningCurve[]>> {
    return api.get<MEMLearningCurve[]>(`/mem/performance/learning-curve?days=${days}`);
  },

  async getPerformancePredictions(): Promise<ApiResponse<any>> {
    return api.get('/mem/performance/predictions');
  },

  async getPerformanceByStrategy(): Promise<ApiResponse<any[]>> {
    return api.get('/mem/performance/by-strategy');
  },

  async getPerformanceByAsset(): Promise<ApiResponse<any[]>> {
    return api.get('/mem/performance/by-asset');
  },

  // Configuration
  async getConfig(): Promise<ApiResponse<MEMConfig>> {
    return api.get<MEMConfig>('/mem/config');
  },

  async updateRiskLimits(limits: MEMConfig['riskLimits']): Promise<ApiResponse<{ success: boolean }>> {
    return api.put('/mem/config/risk-limits', limits);
  },

  async updateTradingHours(hours: MEMConfig['tradingHours']): Promise<ApiResponse<{ success: boolean }>> {
    return api.put('/mem/config/trading-hours', hours);
  },

  async updatePermissions(permissions: MEMConfig['permissions']): Promise<ApiResponse<{ success: boolean }>> {
    return api.put('/mem/config/permissions', permissions);
  },

  async updateAutonomyLevel(level: MEMAutonomyLevel): Promise<ApiResponse<{ success: boolean }>> {
    return api.put('/mem/config/autonomy-level', { level });
  },

  // Datasets
  async getAvailableDatasets(): Promise<ApiResponse<MEMDataset[]>> {
    return api.get<MEMDataset[]>('/mem/datasets/available');
  },

  async queryDataset(datasetId: string, query: any): Promise<ApiResponse<any>> {
    return api.post(`/mem/datasets/${datasetId}/query`, query);
  },

  async analyzeDataset(datasetId: string, analysisType: string): Promise<ApiResponse<any>> {
    return api.post('/mem/datasets/analyze', { datasetId, analysisType });
  },

  async getDatasetPreview(datasetId: string, limit: number = 100): Promise<ApiResponse<any>> {
    return api.get(`/mem/datasets/${datasetId}/preview?limit=${limit}`);
  },
};

export default memService;
