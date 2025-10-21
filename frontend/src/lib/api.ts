import axios from 'axios';

// Backend API base URL - update this if backend runs on different host/port
const API_BASE_URL = (import.meta.env.VITE_API_URL || 'http://localhost:5002/api') + '/mltraining';

export interface MLModelInfo {
  modelId: string;
  modelType: string;
  trainedAt: string;
  accuracy: number;
  precision: number;
  recall: number;
  f1Score: number;
  trainingRows: number;
  status: string;
}

export interface ModelMetrics {
  accuracy: number;
  precision: number;
  recall: number;
  f1Score: number;
}

export interface PatternInfo {
  type: string;
  confidence: number;
  signal: string;
  reason: string;
}

export interface OpportunityInfo {
  symbol: string;
  price: number;
  rsi: number;
  volumeRatio: number;
  reversalConfidence: number;
  patterns: PatternInfo[];
}

export interface PatternAnalysis {
  timestamp: string;
  opportunities: OpportunityInfo[];
  allResults: OpportunityInfo[];
}

export const mlApi = {
  // Get all ML models
  async getModels(): Promise<MLModelInfo[]> {
    const response = await axios.get(`${API_BASE_URL}/models`);
    return response.data;
  },

  // Get model details
  async getModelDetails(modelId: string) {
    const response = await axios.get(`${API_BASE_URL}/models/${modelId}`);
    return response.data;
  },

  // Start training
  async startTraining(symbols: string[], days: number = 30) {
    const response = await axios.post(`${API_BASE_URL}/train`, {
      symbols,
      days,
      interval: '5m',
    });
    return response.data;
  },

  // Get training status
  async getTrainingStatus(jobId: string) {
    const response = await axios.get(`${API_BASE_URL}/training/${jobId}`);
    return response.data;
  },

  // Get latest patterns
  async getLatestPatterns(): Promise<PatternAnalysis> {
    const response = await axios.get(`${API_BASE_URL}/patterns`);
    return response.data;
  },

  // Check health
  async checkHealth() {
    const response = await axios.get(`${API_BASE_URL}/health`);
    return response.data;
  },

  // Visualization endpoints
  async getLearningCurves(modelId: string) {
    const response = await axios.get(`${API_BASE_URL}/visualizations/learning-curves/${modelId}`);
    return response.data;
  },

  async getFeatureImportance(modelId: string, topN: number = 20) {
    const response = await axios.get(`${API_BASE_URL}/visualizations/feature-importance/${modelId}?top_n=${topN}`);
    return response.data;
  },

  async getModelComparison() {
    const response = await axios.get(`${API_BASE_URL}/visualizations/model-comparison`);
    return response.data;
  },

  async getTrainingHistory(modelId: string) {
    const response = await axios.get(`${API_BASE_URL}/visualizations/training-history/${modelId}`);
    return response.data;
  },

  async getOverfittingDashboard(modelId: string) {
    const response = await axios.get(`${API_BASE_URL}/visualizations/overfitting-dashboard/${modelId}`);
    return response.data;
  },

  // Ensemble prediction
  async ensemblePredict(symbol: string, candles: any[], strategy: 'voting' | 'weighted' | 'confidence' = 'weighted') {
    const response = await axios.post(`${API_BASE_URL}/predict/ensemble?strategy=${strategy}`, {
      symbol,
      recent_candles: candles,
    });
    return response.data;
  },
};
