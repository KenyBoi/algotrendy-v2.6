import { api } from './api';
import { API_ENDPOINTS } from '../config/api';
import type { CompanyData, DailyData, FinancialMetric, Filing } from '../types';

export const datasetService = {
  // Get company information
  async getCompany(ticker: string) {
    return api.get<CompanyData>(API_ENDPOINTS.datasets.getCompany(ticker));
  },

  // Get financial metrics for a company
  async getFinancialMetrics(ticker: string, period: 'quarter' | 'annual' = 'quarter') {
    return api.get<FinancialMetric[]>(
      `${API_ENDPOINTS.datasets.getFinancialMetrics(ticker)}?period=${period}`
    );
  },

  // Get daily market data
  async getDailyData(ticker: string) {
    return api.get<DailyData>(API_ENDPOINTS.datasets.getDailyData(ticker));
  },

  // Get company filings
  async getFilings(ticker: string) {
    return api.get<Filing[]>(API_ENDPOINTS.datasets.getFilings(ticker));
  },

  // Add company to watchlist
  async addToMyCompanies(ticker: string) {
    return api.post(API_ENDPOINTS.datasets.addToMyCompanies, { ticker });
  },

  // Search companies
  async searchCompanies(query: string) {
    return api.get<CompanyData[]>(
      `${API_ENDPOINTS.datasets.searchCompanies}?q=${encodeURIComponent(query)}`
    );
  },
};
