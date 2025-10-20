import { api } from './api';
import { API_ENDPOINTS } from '../config/api';
import type { QueryRequest, QueryResult } from '../types';

export const queryService = {
  // Execute a query
  async executeQuery(request: QueryRequest) {
    return api.post<QueryResult>(API_ENDPOINTS.queries.execute, request);
  },

  // Save a query
  async saveQuery(query: QueryRequest) {
    return api.post(API_ENDPOINTS.queries.save, query);
  },

  // Get query history
  async getQueryHistory() {
    return api.get<QueryRequest[]>(API_ENDPOINTS.queries.history);
  },
};
