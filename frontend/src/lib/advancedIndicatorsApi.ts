import { api } from './api';

export interface MarketData {
  timestamp: string;
  open: number;
  high: number;
  low: number;
  close: number;
  volume: number;
}

export interface FisherTransformResult {
  fisher: number;
  trigger: number;
  signal: string;
}

export interface SqueezeMomentumResult {
  isSqueeze: boolean;
  momentum: number;
  signal: string;
  bbUpper: number;
  bbLower: number;
  kcUpper: number;
  kcLower: number;
}

export interface WaveTrendResult {
  wt1: number;
  wt2: number;
  signal: string;
}

export interface RVIResult {
  rvi: number;
  signal: number;
  trend: string;
}

export interface ChoppinessResult {
  index: number;
  state: string;
  isTrending: boolean;
  isRanging: boolean;
}

export interface MTFIndicatorResult {
  name: string;
  values: Record<string, number>;
  trends?: Record<string, string>;
  averageValue: number;
  signal: string;
  confluence: number;
}

export interface AdvancedIndicatorsResponse {
  symbol: string;
  timeframe: string;
  timestamp: string;

  // Advanced Momentum
  fisherTransform?: FisherTransformResult;
  laguerreRSI?: number;
  connorsRSI?: number;
  squeezeMomentum?: SqueezeMomentumResult;
  waveTrend?: WaveTrendResult;
  rvi?: RVIResult;
  schaffTrendCycle?: number;

  // Volatility & Risk
  historicalVolatility?: number;
  parkinsonVolatility?: number;
  garmanKlassVolatility?: number;
  yangZhangVolatility?: number;
  choppinessIndex?: ChoppinessResult;

  // Overall
  overallSignal: string;
  signalStrength: number;
}

export interface IndicatorInfo {
  name: string;
  description: string;
  priority: string;
}

export interface IndicatorListResponse {
  categories: Record<string, IndicatorInfo[]>;
  totalIndicators: number;
}

class AdvancedIndicatorsApi {
  private baseUrl: string;

  constructor() {
    this.baseUrl = '/api/advancedindicators';
  }

  /**
   * Fetch historical market data for a symbol and timeframe
   * This is a mock implementation - replace with actual API call
   */
  private async fetchMarketData(symbol: string, timeframe: string, limit: number = 200): Promise<MarketData[]> {
    // TODO: Replace with actual market data API call
    // For now, generate mock data
    const data: MarketData[] = [];
    const now = Date.now();
    const intervalMs = this.getIntervalMs(timeframe);

    let currentPrice = 50000 + Math.random() * 10000;

    for (let i = limit - 1; i >= 0; i--) {
      const timestamp = new Date(now - i * intervalMs).toISOString();
      const change = (Math.random() - 0.5) * 1000;
      const open = currentPrice;
      const high = currentPrice + Math.abs(change) * 0.5;
      const low = currentPrice - Math.abs(change) * 0.5;
      const close = currentPrice + change;
      const volume = 100 + Math.random() * 500;

      data.push({
        timestamp,
        open,
        high,
        low,
        close,
        volume
      });

      currentPrice = close;
    }

    return data;
  }

  private getIntervalMs(timeframe: string): number {
    const intervals: Record<string, number> = {
      '1m': 60 * 1000,
      '5m': 5 * 60 * 1000,
      '15m': 15 * 60 * 1000,
      '30m': 30 * 60 * 1000,
      '1h': 60 * 60 * 1000,
      '4h': 4 * 60 * 60 * 1000,
      '1d': 24 * 60 * 60 * 1000,
      '1w': 7 * 24 * 60 * 60 * 1000
    };

    return intervals[timeframe] || intervals['1h'];
  }

  /**
   * Calculate all advanced indicators for a symbol and timeframe
   */
  async calculateAll(symbol: string, timeframe: string): Promise<AdvancedIndicatorsResponse> {
    try {
      // Fetch market data
      const marketData = await this.fetchMarketData(symbol, timeframe);

      // Make API request
      const response = await fetch(`${this.baseUrl}/calculate`, {
        method: 'POST',
        headers: {
          'Content-Type': 'application/json',
        },
        body: JSON.stringify({
          symbol,
          timeframe,
          data: marketData
        })
      });

      if (!response.ok) {
        const error = await response.json();
        throw new Error(error.error || error.details || 'Failed to calculate indicators');
      }

      return await response.json();
    } catch (error) {
      console.error('Error in calculateAll:', error);
      throw error;
    }
  }

  /**
   * Calculate Fisher Transform only
   */
  async calculateFisher(symbol: string, timeframe: string, period: number = 10): Promise<FisherTransformResult> {
    const marketData = await this.fetchMarketData(symbol, timeframe);

    const response = await fetch(`${this.baseUrl}/fisher`, {
      method: 'POST',
      headers: {
        'Content-Type': 'application/json',
      },
      body: JSON.stringify({
        symbol,
        data: marketData,
        period
      })
    });

    if (!response.ok) {
      throw new Error('Failed to calculate Fisher Transform');
    }

    return await response.json();
  }

  /**
   * Calculate Squeeze Momentum only
   */
  async calculateSqueeze(symbol: string, timeframe: string, period: number = 20): Promise<SqueezeMomentumResult> {
    const marketData = await this.fetchMarketData(symbol, timeframe);

    const response = await fetch(`${this.baseUrl}/squeeze`, {
      method: 'POST',
      headers: {
        'Content-Type': 'application/json',
      },
      body: JSON.stringify({
        symbol,
        data: marketData,
        period
      })
    });

    if (!response.ok) {
      throw new Error('Failed to calculate Squeeze Momentum');
    }

    return await response.json();
  }

  /**
   * Calculate Choppiness Index only
   */
  async calculateChoppiness(symbol: string, timeframe: string, period: number = 14): Promise<ChoppinessResult> {
    const marketData = await this.fetchMarketData(symbol, timeframe);

    const response = await fetch(`${this.baseUrl}/choppiness`, {
      method: 'POST',
      headers: {
        'Content-Type': 'application/json',
      },
      body: JSON.stringify({
        symbol,
        data: marketData,
        period
      })
    });

    if (!response.ok) {
      throw new Error('Failed to calculate Choppiness Index');
    }

    return await response.json();
  }

  /**
   * Calculate Multi-Timeframe RSI
   */
  async calculateMTFRSI(symbol: string, timeframes: string[], period: number = 14): Promise<MTFIndicatorResult> {
    const timeframeData: Record<string, MarketData[]> = {};

    for (const tf of timeframes) {
      timeframeData[tf] = await this.fetchMarketData(symbol, tf);
    }

    const response = await fetch(`${this.baseUrl}/mtf-rsi`, {
      method: 'POST',
      headers: {
        'Content-Type': 'application/json',
      },
      body: JSON.stringify({
        symbol,
        timeframeData,
        period
      })
    });

    if (!response.ok) {
      throw new Error('Failed to calculate MTF RSI');
    }

    return await response.json();
  }

  /**
   * Calculate Multi-Timeframe Moving Average
   */
  async calculateMTFMA(symbol: string, timeframes: string[]): Promise<MTFIndicatorResult> {
    const timeframeData: Record<string, MarketData[]> = {};

    for (const tf of timeframes) {
      timeframeData[tf] = await this.fetchMarketData(symbol, tf);
    }

    const response = await fetch(`${this.baseUrl}/mtf-ma`, {
      method: 'POST',
      headers: {
        'Content-Type': 'application/json',
      },
      body: JSON.stringify({
        symbol,
        timeframeData
      })
    });

    if (!response.ok) {
      throw new Error('Failed to calculate MTF MA');
    }

    return await response.json();
  }

  /**
   * Get list of all available indicators
   */
  async getIndicatorList(): Promise<IndicatorListResponse> {
    const response = await fetch(`${this.baseUrl}/list`);

    if (!response.ok) {
      throw new Error('Failed to fetch indicator list');
    }

    return await response.json();
  }
}

export const advancedIndicatorsApi = new AdvancedIndicatorsApi();
