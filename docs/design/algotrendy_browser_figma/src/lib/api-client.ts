/**
 * AlgoTrendy API Client
 * Integrates with AlgoTrendy v2.6 C# .NET 8 Backend
 */

import axios, { AxiosInstance, AxiosError } from 'axios';

// Get API URL from environment or default
const API_BASE_URL = import.meta.env.VITE_API_BASE_URL || 'http://localhost:5002/api';

// Types
export interface ApiResponse<T> {
  data: T;
  success: boolean;
  message?: string;
}

export interface Position {
  id: string;
  symbol: string;
  side: 'Long' | 'Short';
  quantity: number;
  entryPrice: number;
  currentPrice: number;
  unrealizedPnl: number;
  realizedPnl: number;
  leverage: number;
  openedAt: string;
}

export interface Order {
  id: string;
  symbol: string;
  side: 'Buy' | 'Sell';
  type: 'Market' | 'Limit' | 'StopMarket' | 'StopLimit';
  quantity: number;
  price?: number;
  stopPrice?: number;
  status: 'Pending' | 'Open' | 'PartiallyFilled' | 'Filled' | 'Cancelled' | 'Rejected';
  filledQuantity: number;
  averagePrice: number;
  createdAt: string;
  updatedAt: string;
}

export interface OrderRequest {
  symbol: string;
  side: 'Buy' | 'Sell';
  type: 'Market' | 'Limit' | 'StopMarket' | 'StopLimit';
  quantity: number;
  price?: number;
  stopPrice?: number;
  timeInForce?: 'GTC' | 'IOC' | 'FOK';
}

export interface Trade {
  id: string;
  orderId: string;
  symbol: string;
  side: 'Buy' | 'Sell';
  quantity: number;
  price: number;
  commission: number;
  timestamp: string;
}

export interface MarketData {
  symbol: string;
  price: number;
  volume: number;
  high24h: number;
  low24h: number;
  change24h: number;
  changePercent24h: number;
  timestamp: string;
}

export interface BacktestRequest {
  strategyName: string;
  symbol: string;
  startDate: string;
  endDate: string;
  initialCapital: number;
  parameters: Record<string, any>;
}

export interface BacktestResult {
  id: string;
  strategyName: string;
  symbol: string;
  startDate: string;
  endDate: string;
  initialCapital: number;
  finalCapital: number;
  totalReturn: number;
  totalReturnPercent: number;
  sharpeRatio: number;
  maxDrawdown: number;
  winRate: number;
  totalTrades: number;
  profitableTrades: number;
  losingTrades: number;
  averageWin: number;
  averageLoss: number;
  largestWin: number;
  largestLoss: number;
  trades: Trade[];
}

export interface Portfolio {
  totalValue: number;
  cash: number;
  equity: number;
  unrealizedPnl: number;
  realizedPnl: number;
  todayPnl: number;
  todayPnlPercent: number;
  positions: Position[];
}

// Create axios instance
const apiClient: AxiosInstance = axios.create({
  baseURL: API_BASE_URL,
  timeout: 30000,
  headers: {
    'Content-Type': 'application/json',
  },
});

// Request interceptor (add auth token if exists)
apiClient.interceptors.request.use(
  (config) => {
    const token = localStorage.getItem('authToken');
    if (token) {
      config.headers.Authorization = `Bearer ${token}`;
    }
    return config;
  },
  (error) => {
    return Promise.reject(error);
  }
);

// Response interceptor (handle errors)
apiClient.interceptors.response.use(
  (response) => response,
  (error: AxiosError) => {
    if (error.response?.status === 401) {
      // Unauthorized - clear token and redirect to login
      localStorage.removeItem('authToken');
      window.location.href = '/login';
    }
    return Promise.reject(error);
  }
);

// API Methods
export const api = {
  // Health Check
  health: () => apiClient.get('/health'),

  // Market Data
  market: {
    getLatest: (symbol: string) =>
      apiClient.get<MarketData>(`/market/latest/${symbol}`),

    getHistorical: (symbol: string, from: string, to: string) =>
      apiClient.get(`/market/historical/${symbol}`, {
        params: { from, to },
      }),
  },

  // Orders
  orders: {
    getAll: () => apiClient.get<Order[]>('/orders'),

    getById: (id: string) => apiClient.get<Order>(`/orders/${id}`),

    place: (orderRequest: OrderRequest) =>
      apiClient.post<Order>('/orders', orderRequest),

    cancel: (id: string, symbol: string) =>
      apiClient.delete<Order>(`/orders/${id}`, {
        params: { symbol },
      }),

    getHistory: (limit = 100) =>
      apiClient.get<Order[]>('/orders/history', {
        params: { limit },
      }),
  },

  // Positions
  positions: {
    getAll: () => apiClient.get<Position[]>('/positions'),

    getBySymbol: (symbol: string) =>
      apiClient.get<Position>(`/positions/${symbol}`),

    close: (symbol: string) =>
      apiClient.delete(`/positions/${symbol}`),
  },

  // Portfolio
  portfolio: {
    get: () => apiClient.get<Portfolio>('/portfolio'),
  },

  // Trades
  trades: {
    getAll: (limit = 100) =>
      apiClient.get<Trade[]>('/trades', {
        params: { limit },
      }),

    getBySymbol: (symbol: string, limit = 100) =>
      apiClient.get<Trade[]>(`/trades/${symbol}`, {
        params: { limit },
      }),
  },

  // Backtesting
  backtest: {
    run: (request: BacktestRequest) =>
      apiClient.post<{ id: string }>('/backtest/run', request),

    getResults: (id: string) =>
      apiClient.get<BacktestResult>(`/backtest/results/${id}`),

    getHistory: () =>
      apiClient.get<BacktestResult[]>('/backtest/history'),
  },

  // Strategies
  strategies: {
    getAll: () => apiClient.get<string[]>('/strategies'),

    getConfig: (name: string) =>
      apiClient.get(`/strategies/${name}/config`),
  },
};

export default api;
