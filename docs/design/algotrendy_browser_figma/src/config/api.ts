// API Configuration
// Update this base URL to match your .NET backend API endpoint
export const API_BASE_URL = 'http://localhost:5298/api';

// API Endpoints
export const API_ENDPOINTS = {
  // Dataset endpoints
  datasets: {
    getCompany: (ticker: string) => `/datasets/company/${ticker}`,
    getFinancialMetrics: (ticker: string) => `/datasets/financial-metrics/${ticker}`,
    getDailyData: (ticker: string) => `/datasets/daily-data/${ticker}`,
    getFilings: (ticker: string) => `/datasets/filings/${ticker}`,
    addToMyCompanies: '/datasets/my-companies',
    searchCompanies: '/datasets/search'
  },
  
  // Strategy endpoints
  strategies: {
    list: '/strategies',
    get: (id: string) => `/strategies/${id}`,
    create: '/strategies',
    update: (id: string) => `/strategies/${id}`,
    delete: (id: string) => `/strategies/${id}`,
    backtest: (id: string) => `/strategies/${id}/backtest`,
    optimize: (id: string) => `/strategies/${id}/optimize`
  },
  
  // Query endpoints
  queries: {
    execute: '/queries/execute',
    save: '/queries/save',
    history: '/queries/history'
  },
  
  // AI Assistant endpoints
  ai: {
    chat: '/ai/chat',
    analyze: '/ai/analyze',
    suggestions: '/ai/suggestions'
  },
  
  // Analytics endpoints
  analytics: {
    portfolio: '/analytics/portfolio',
    performance: '/analytics/performance',
    reports: '/analytics/reports'
  }
};
