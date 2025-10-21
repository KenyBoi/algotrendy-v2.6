import axios from 'axios';
import type {
  Strategy,
  StrategyListItem,
  CreateStrategyRequest,
  UpdateStrategyRequest,
  BacktestConfig,
  BacktestResults,
  StrategyTemplate,
} from '../types/strategy';

const API_BASE_URL = import.meta.env.VITE_API_URL || 'http://localhost:5002/api';

export const strategyApi = {
  // Strategy CRUD
  async getAllStrategies(filters?: {
    category?: string;
    status?: string;
    tags?: string[];
    minSharpe?: number;
  }): Promise<StrategyListItem[]> {
    const response = await axios.get(`${API_BASE_URL}/strategies`, { params: filters });
    return response.data;
  },

  async getStrategy(id: string): Promise<Strategy> {
    const response = await axios.get(`${API_BASE_URL}/strategies/${id}`);
    return response.data;
  },

  async createStrategy(data: CreateStrategyRequest): Promise<{ id: string; strategy: Strategy }> {
    const response = await axios.post(`${API_BASE_URL}/strategies`, data);
    return response.data;
  },

  async updateStrategy(id: string, data: UpdateStrategyRequest): Promise<Strategy> {
    const response = await axios.put(`${API_BASE_URL}/strategies/${id}`, data);
    return response.data;
  },

  async deleteStrategy(id: string): Promise<void> {
    await axios.delete(`${API_BASE_URL}/strategies/${id}`);
  },

  async duplicateStrategy(id: string, newName: string): Promise<{ id: string; strategy: Strategy }> {
    const response = await axios.post(`${API_BASE_URL}/strategies/${id}/duplicate`, { newName });
    return response.data;
  },

  // Strategy Templates
  async getTemplates(): Promise<StrategyTemplate[]> {
    const response = await axios.get(`${API_BASE_URL}/strategies/templates`);
    return response.data;
  },

  async createFromTemplate(templateId: string, customData?: Partial<CreateStrategyRequest>): Promise<{ id: string; strategy: Strategy }> {
    const response = await axios.post(`${API_BASE_URL}/strategies/from-template/${templateId}`, customData);
    return response.data;
  },

  // Backtesting Integration
  async runBacktest(strategyId: string, config: BacktestConfig): Promise<{ backtestId: string }> {
    const response = await axios.post(`${API_BASE_URL}/strategies/${strategyId}/backtest`, config);
    return response.data;
  },

  async getBacktestResults(strategyId: string, backtestId?: string): Promise<BacktestResults> {
    const url = backtestId
      ? `${API_BASE_URL}/strategies/${strategyId}/backtest/${backtestId}`
      : `${API_BASE_URL}/strategies/${strategyId}/backtest/latest`;
    const response = await axios.get(url);
    return response.data;
  },

  // Strategy Validation
  async validateStrategy(strategy: Partial<Strategy>): Promise<{
    valid: boolean;
    errors: string[];
    warnings: string[];
  }> {
    const response = await axios.post(`${API_BASE_URL}/strategies/validate`, strategy);
    return response.data;
  },

  // Strategy Code Generation
  async generateCode(strategy: Strategy, language: 'python' | 'csharp' = 'python'): Promise<{ code: string }> {
    const response = await axios.post(`${API_BASE_URL}/strategies/generate-code`, {
      strategy,
      language,
    });
    return response.data;
  },

  // Search
  async searchStrategies(query: string): Promise<StrategyListItem[]> {
    const response = await axios.get(`${API_BASE_URL}/strategies/search`, {
      params: { q: query },
    });
    return response.data;
  },

  // Analytics
  async getStrategyPerformance(strategyId: string, days?: number): Promise<{
    totalTrades: number;
    winRate: number;
    sharpeRatio: number;
    totalPnL: number;
    avgWin: number;
    avgLoss: number;
  }> {
    const response = await axios.get(`${API_BASE_URL}/strategies/${strategyId}/performance`, {
      params: { days },
    });
    return response.data;
  },

  async compareStrategies(strategyIds: string[]): Promise<{
    strategies: Array<{
      id: string;
      name: string;
      sharpeRatio: number;
      winRate: number;
      totalReturn: number;
      maxDrawdown: number;
    }>;
  }> {
    const response = await axios.post(`${API_BASE_URL}/strategies/compare`, { strategyIds });
    return response.data;
  },
};
