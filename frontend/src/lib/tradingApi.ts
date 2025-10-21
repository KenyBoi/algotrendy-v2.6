import axios from 'axios';

// Backend API base URL - update this if backend runs on different host/port
const API_BASE_URL = import.meta.env.VITE_API_URL || 'http://localhost:5002/api';

// Trading API endpoints
export const tradingApi = {
  // Market Data
  async getMarketData(symbols: string[]) {
    const response = await axios.get(`${API_BASE_URL}/marketdata`, {
      params: { symbols: symbols.join(',') },
    });
    return response.data;
  },

  async getOrderBook(symbol: string) {
    const response = await axios.get(`${API_BASE_URL}/marketdata/${symbol}/orderbook`);
    return response.data;
  },

  async getPriceHistory(symbol: string, interval: string = '1h', limit: number = 100) {
    const response = await axios.get(`${API_BASE_URL}/marketdata/${symbol}/history`, {
      params: { interval, limit },
    });
    return response.data;
  },

  // Orders
  async placeOrder(orderData: {
    symbol: string;
    side: 'buy' | 'sell';
    type: 'market' | 'limit' | 'stop';
    quantity: number;
    price?: number;
    broker?: string;
  }) {
    const response = await axios.post(`${API_BASE_URL}/orders`, orderData);
    return response.data;
  },

  async getOrders(status?: string) {
    const response = await axios.get(`${API_BASE_URL}/orders`, {
      params: { status },
    });
    return response.data;
  },

  async cancelOrder(orderId: string) {
    const response = await axios.delete(`${API_BASE_URL}/orders/${orderId}`);
    return response.data;
  },

  // Positions
  async getPositions() {
    const response = await axios.get(`${API_BASE_URL}/positions`);
    return response.data;
  },

  async closePosition(positionId: string) {
    const response = await axios.post(`${API_BASE_URL}/positions/${positionId}/close`);
    return response.data;
  },

  // Portfolio
  async getPortfolio() {
    const response = await axios.get(`${API_BASE_URL}/portfolio`);
    return response.data;
  },

  async getPortfolioAnalytics() {
    const response = await axios.get(`${API_BASE_URL}/portfolio/analytics`);
    return response.data;
  },

  // Backtesting
  async runBacktest(params: {
    strategy: string;
    symbol: string;
    startDate: string;
    endDate: string;
    initialCapital: number;
  }) {
    const response = await axios.post(`${API_BASE_URL}/backtesting/run`, params);
    return response.data;
  },

  async getBacktestResults(backtestId?: string) {
    const url = backtestId
      ? `${API_BASE_URL}/backtesting/results/${backtestId}`
      : `${API_BASE_URL}/backtesting/results`;
    const response = await axios.get(url);
    return response.data;
  },

  async getBacktestStatus(backtestId: string) {
    const response = await axios.get(`${API_BASE_URL}/backtesting/status/${backtestId}`);
    return response.data;
  },
};

// Export convenience functions
export const {
  getMarketData,
  getOrderBook,
  getPriceHistory,
  placeOrder,
  getOrders,
  cancelOrder,
  getPositions,
  closePosition,
  getPortfolio,
  getPortfolioAnalytics,
  runBacktest,
  getBacktestResults,
  getBacktestStatus,
} = tradingApi;
