/**
 * SignalR WebSocket Client for Real-Time Market Data
 * Connects to AlgoTrendy v2.6 SignalR Hub
 */

import { HubConnection, HubConnectionBuilder, LogLevel } from '@microsoft/signalr';

const WS_URL = import.meta.env.VITE_WS_URL || 'http://localhost:5002/hubs/market';

export interface MarketUpdate {
  symbol: string;
  price: number;
  volume: number;
  timestamp: string;
}

export interface OrderUpdate {
  orderId: string;
  status: string;
  filledQuantity: number;
  averagePrice: number;
}

export interface PositionUpdate {
  symbol: string;
  unrealizedPnl: number;
  currentPrice: number;
}

type MarketUpdateCallback = (update: MarketUpdate) => void;
type OrderUpdateCallback = (update: OrderUpdate) => void;
type PositionUpdateCallback = (update: PositionUpdate) => void;

class SignalRClient {
  private connection: HubConnection | null = null;
  private isConnecting = false;
  private reconnectAttempts = 0;
  private maxReconnectAttempts = 5;

  async connect(): Promise<void> {
    if (this.connection?.state === 'Connected' || this.isConnecting) {
      return;
    }

    this.isConnecting = true;

    try {
      this.connection = new HubConnectionBuilder()
        .withUrl(WS_URL, {
          accessTokenFactory: () => {
            return localStorage.getItem('authToken') || '';
          },
        })
        .withAutomaticReconnect({
          nextRetryDelayInMilliseconds: (retryContext) => {
            // Exponential backoff: 2s, 4s, 8s, 16s, 32s
            return Math.min(1000 * Math.pow(2, retryContext.previousRetryCount), 32000);
          },
        })
        .configureLogging(LogLevel.Information)
        .build();

      // Connection event handlers
      this.connection.onreconnecting((error) => {
        console.warn('SignalR reconnecting...', error);
      });

      this.connection.onreconnected((connectionId) => {
        console.log('SignalR reconnected:', connectionId);
        this.reconnectAttempts = 0;
      });

      this.connection.onclose((error) => {
        console.error('SignalR connection closed:', error);
        if (this.reconnectAttempts < this.maxReconnectAttempts) {
          this.reconnectAttempts++;
          setTimeout(() => this.connect(), 5000);
        }
      });

      await this.connection.start();
      console.log('SignalR connected');
      this.reconnectAttempts = 0;
    } catch (error) {
      console.error('SignalR connection error:', error);
      if (this.reconnectAttempts < this.maxReconnectAttempts) {
        this.reconnectAttempts++;
        setTimeout(() => this.connect(), 5000);
      }
    } finally {
      this.isConnecting = false;
    }
  }

  async disconnect(): Promise<void> {
    if (this.connection) {
      await this.connection.stop();
      this.connection = null;
    }
  }

  // Subscribe to market data updates
  onMarketUpdate(callback: MarketUpdateCallback): void {
    if (!this.connection) {
      throw new Error('SignalR not connected');
    }
    this.connection.on('MarketUpdate', callback);
  }

  // Subscribe to order updates
  onOrderUpdate(callback: OrderUpdateCallback): void {
    if (!this.connection) {
      throw new Error('SignalR not connected');
    }
    this.connection.on('OrderUpdate', callback);
  }

  // Subscribe to position updates
  onPositionUpdate(callback: PositionUpdateCallback): void {
    if (!this.connection) {
      throw new Error('SignalR not connected');
    }
    this.connection.on('PositionUpdate', callback);
  }

  // Subscribe to specific symbol market data
  async subscribeToSymbol(symbol: string): Promise<void> {
    if (!this.connection) {
      throw new Error('SignalR not connected');
    }
    await this.connection.invoke('SubscribeToSymbol', symbol);
  }

  // Unsubscribe from specific symbol
  async unsubscribeFromSymbol(symbol: string): Promise<void> {
    if (!this.connection) {
      throw new Error('SignalR not connected');
    }
    await this.connection.invoke('UnsubscribeFromSymbol', symbol);
  }

  // Remove specific event handler
  off(eventName: string, callback?: (...args: any[]) => void): void {
    if (this.connection) {
      this.connection.off(eventName, callback);
    }
  }

  // Check connection status
  get isConnected(): boolean {
    return this.connection?.state === 'Connected';
  }

  get connectionState(): string {
    return this.connection?.state || 'Disconnected';
  }
}

// Export singleton instance
export const signalRClient = new SignalRClient();

export default signalRClient;
