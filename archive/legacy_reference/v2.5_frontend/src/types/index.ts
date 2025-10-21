// Authentication
export interface User {
  id: string;
  email: string;
  name: string;
  role: 'admin' | 'trader' | 'viewer';
  avatar?: string;
  createdAt: string;
}

export interface AuthToken {
  access_token: string;
  token_type: string;
  refresh_token?: string;
  expires_in?: number;
}

// Trading
export interface Strategy {
  id: string;
  name: string;
  description: string;
  asset_class: 'crypto' | 'stocks' | 'futures';
  risk_level: 'low' | 'medium' | 'high';
  enabled: boolean;
  parameters?: Record<string, unknown>;
  createdAt: string;
  updatedAt: string;
}

export interface Position {
  id: string;
  symbol: string;
  entry_price: number;
  current_price: number;
  quantity: number;
  side: 'long' | 'short';
  pnl: number;
  pnl_percent: number;
  strategy_id: string;
  opened_at: string;
  closed_at?: string;
}

export interface Trade {
  id: string;
  symbol: string;
  side: 'buy' | 'sell';
  entry_price: number;
  exit_price?: number;
  quantity: number;
  pnl?: number;
  status: 'open' | 'closed';
  strategy_id: string;
  executed_at: string;
  closed_at?: string;
}

export interface Portfolio {
  total_value: number;
  cash: number;
  equity: number;
  buying_power: number;
  unrealized_pnl: number;
  realized_pnl: number;
  positions: Position[];
  strategies: Strategy[];
}

export interface Alert {
  id: string;
  type: 'info' | 'warning' | 'error' | 'success';
  title: string;
  message: string;
  timestamp: string;
  read: boolean;
}

// API Response
export interface ApiResponse<T> {
  success: boolean;
  data?: T;
  error?: string;
  message?: string;
}

// Freqtrade Integration Types
export interface FreqtradeBot {
  name: string;
  port: number;
  strategy: string;
  status: 'online' | 'offline';
  balance?: number;
  profit?: number;
  profit_percent?: number;
  open_trades?: number;
  win_rate?: number;
  total_trades?: number;
  error?: string;
}

export interface FreqtradeBotPortfolio {
  name: string;
  balance: number;
  profit: number;
  profit_percent: number;
  open_trades: number;
  total_trades: number;
  winning_trades: number;
  win_rate: number;
  status: 'online' | 'offline';
  error?: string;
}

export interface FreqtradePortfolio {
  total_balance: number;
  total_profit: number;
  total_profit_percent: number;
  active_bots: number;
  total_open_trades: number;
  combined_win_rate: number;
  bot_portfolios: FreqtradeBotPortfolio[];
}

export interface FreqtradePosition extends Position {
  bot_name: string;
  freqtrade_trade_id: number;
  entry_reason?: string;
  exit_reason?: string;
  stop_loss?: number;
  take_profit?: number;
  duration_minutes: number;
}

export interface IndexingResult {
  success: boolean;
  records_indexed?: number;
  bot_status?: Record<string, any>;
  algolia_task_id?: string;
  error?: string;
  timestamp: string;
}
