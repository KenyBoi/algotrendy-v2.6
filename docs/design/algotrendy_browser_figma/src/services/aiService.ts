import { api } from './api';
import { API_ENDPOINTS } from '../config/api';
import type { ChatRequest, ChatResponse } from '../types';

export const aiService = {
  // Send a chat message to the AI
  async chat(request: ChatRequest) {
    return api.post<ChatResponse>(API_ENDPOINTS.ai.chat, request);
  },

  // Get AI analysis for specific data
  async analyze(data: any) {
    return api.post<{ analysis: string; insights: string[] }>(
      API_ENDPOINTS.ai.analyze,
      data
    );
  },

  // Get AI suggestions
  async getSuggestions(context: any) {
    return api.post<{ suggestions: string[] }>(
      API_ENDPOINTS.ai.suggestions,
      context
    );
  },
};
