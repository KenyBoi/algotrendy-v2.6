import { api } from './api';
import { API_ENDPOINTS } from '../config/api';
import type { Strategy, BacktestRequest, BacktestResult } from '../types';

export const strategyService = {
  // Get all strategies
  async listStrategies() {
    return api.get<Strategy[]>(API_ENDPOINTS.strategies.list);
  },

  // Get a single strategy
  async getStrategy(id: string) {
    return api.get<Strategy>(API_ENDPOINTS.strategies.get(id));
  },

  // Create a new strategy
  async createStrategy(strategy: Omit<Strategy, 'id'>) {
    return api.post<Strategy>(API_ENDPOINTS.strategies.create, strategy);
  },

  // Update an existing strategy
  async updateStrategy(id: string, strategy: Partial<Strategy>) {
    return api.put<Strategy>(API_ENDPOINTS.strategies.update(id), strategy);
  },

  // Delete a strategy
  async deleteStrategy(id: string) {
    return api.delete(API_ENDPOINTS.strategies.delete(id));
  },

  // Run backtest on a strategy
  async runBacktest(request: BacktestRequest) {
    return api.post<BacktestResult>(
      API_ENDPOINTS.strategies.backtest(request.strategyId),
      request
    );
  },

  // Optimize strategy parameters
  async optimizeStrategy(id: string, optimizationParams: any) {
    return api.post<Strategy>(
      API_ENDPOINTS.strategies.optimize(id),
      optimizationParams
    );
  },
};
