/**
 * Backtesting API Service
 *
 * Handles all communication with the backtesting API endpoints
 */

import axios from 'axios';

const API_BASE_URL = process.env.NEXT_PUBLIC_API_URL || 'http://localhost:8000';

export interface BacktestConfig {
  ai_name: string;
  backtester: 'quantconnect' | 'backtester' | 'custom';
  asset_class: 'crypto' | 'futures' | 'equities';
  symbol: string;
  timeframe: 'tick' | 'min' | 'hr' | 'day' | 'wk' | 'mo' | 'renko' | 'line' | 'range';
  timeframe_value?: number;
  start_date: string;
  end_date: string;
  initial_capital: number;
  indicators: {
    [key: string]: boolean;
  };
  indicator_params: {
    [key: string]: number;
  };
  commission?: number;
  slippage?: number;
}

export interface BacktestMetrics {
  total_return: number;
  annual_return: number;
  sharpe_ratio: number;
  sortino_ratio: number;
  max_drawdown: number;
  win_rate: number;
  profit_factor: number;
  total_trades: number;
  winning_trades: number;
  losing_trades: number;
  avg_win: number;
  avg_loss: number;
  largest_win: number;
  largest_loss: number;
  avg_trade_duration: number;
}

export interface TradeResult {
  entry_time: string;
  exit_time?: string;
  entry_price: number;
  exit_price?: number;
  quantity: number;
  side: 'long' | 'short';
  pnl?: number;
  pnl_percent?: number;
  duration_minutes?: number;
  exit_reason?: string;
}

export interface EquityPoint {
  timestamp: string;
  equity: number;
  cash: number;
  positions_value: number;
  drawdown: number;
}

export interface BacktestResults {
  backtest_id: string;
  status: 'pending' | 'running' | 'completed' | 'failed';
  config: BacktestConfig;
  started_at?: string;
  completed_at?: string;
  execution_time_seconds?: number;
  metrics?: BacktestMetrics;
  equity_curve: EquityPoint[];
  trades: TradeResult[];
  indicators_used: string[];
  error_message?: string;
  metadata?: any;
}

export interface BacktestConfigOptions {
  ai_models: Array<{ value: string; label: string; status: string }>;
  backtesting_engines: Array<{ value: string; label: string; status: string }>;
  asset_classes: Array<{ value: string; label: string; symbols: string[] }>;
  timeframe_types: Array<{ value: string; label: string; needs_value: boolean; value_label?: string }>;
}

export interface BacktestHistoryItem {
  backtest_id: string;
  symbol: string;
  asset_class: string;
  timeframe: string;
  start_date: string;
  end_date: string;
  status: string;
  total_return?: number;
  sharpe_ratio?: number;
  total_trades?: number;
  created_at: string;
}

class BacktestService {
  /**
   * Get available backtesting configuration options
   */
  async getConfig(): Promise<BacktestConfigOptions> {
    const response = await axios.get(`${API_BASE_URL}/api/backtest/config`);
    return response.data.config;
  }

  /**
   * Run a backtest with the given configuration
   */
  async runBacktest(config: BacktestConfig): Promise<BacktestResults> {
    const response = await axios.post(`${API_BASE_URL}/api/backtest/run`, config);
    return response.data.results;
  }

  /**
   * Get results of a specific backtest
   */
  async getResults(backtestId: string): Promise<BacktestResults> {
    const response = await axios.get(`${API_BASE_URL}/api/backtest/results/${backtestId}`);
    return response.data.results;
  }

  /**
   * Get list of past backtests
   */
  async getHistory(limit: number = 50): Promise<BacktestHistoryItem[]> {
    const response = await axios.get(`${API_BASE_URL}/api/backtest/history?limit=${limit}`);
    return response.data.history;
  }

  /**
   * Get available indicators
   */
  async getIndicators(): Promise<any> {
    const response = await axios.get(`${API_BASE_URL}/api/backtest/indicators`);
    return response.data.indicators;
  }

  /**
   * Delete a backtest
   */
  async deleteBacktest(backtestId: string): Promise<void> {
    await axios.delete(`${API_BASE_URL}/api/backtest/${backtestId}`);
  }
}

export const backtestService = new BacktestService();
