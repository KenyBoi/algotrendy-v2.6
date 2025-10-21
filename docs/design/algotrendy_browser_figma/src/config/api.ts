/**
 * API Configuration
 * Runtime environment variables injected at container startup
 */

// Get runtime config (injected at container startup via env-config.js)
const getConfig = () => {
  if (typeof window !== 'undefined' && window.ENV) {
    return {
      API_BASE_URL: window.ENV.API_BASE_URL,
      WS_BASE_URL: window.ENV.WS_BASE_URL,
      ENVIRONMENT: window.ENV.ENVIRONMENT,
      VERSION: window.ENV.VERSION,
      ENABLE_DEBUG: window.ENV.ENABLE_DEBUG,
    };
  }

  // Development fallback (when env-config.js not loaded)
  const isLocalhost = window.location.hostname === 'localhost' || window.location.hostname === '127.0.0.1';
  return {
    API_BASE_URL: import.meta.env.VITE_API_BASE_URL || 'http://localhost:5002/api',
    WS_BASE_URL: window.location.protocol === 'https:' || !isLocalhost
      ? `wss://${window.location.host}`
      : `ws://${window.location.host}`,  // Only use ws:// for localhost development
    ENVIRONMENT: 'development' as const,
    VERSION: 'dev',
    ENABLE_DEBUG: true,
  };
};

const config = getConfig();

export const API_BASE_URL = config.API_BASE_URL;
export const WS_BASE_URL = config.WS_BASE_URL;
export const ENVIRONMENT = config.ENVIRONMENT;
export const VERSION = config.VERSION;
export const ENABLE_DEBUG = config.ENABLE_DEBUG;

// Log config on startup (only in debug mode)
if (ENABLE_DEBUG) {
  console.log('🔧 Runtime Configuration:', {
    API_BASE_URL,
    WS_BASE_URL,
    ENVIRONMENT,
    VERSION,
  });
}

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
