import * as signalR from '@microsoft/signalr';

export class SignalRService {
  private connection: signalR.HubConnection | null = null;
  private readonly hubUrl = import.meta.env.VITE_SIGNALR_HUB_URL || 'http://localhost:5002/hubs/trading';

  async connect(): Promise<void> {
    if (this.connection?.state === signalR.HubConnectionState.Connected) {
      console.log('SignalR already connected');
      return;
    }

    this.connection = new signalR.HubConnectionBuilder()
      .withUrl(this.hubUrl)
      .withAutomaticReconnect()
      .configureLogging(signalR.LogLevel.Information)
      .build();

    this.connection.onreconnecting((error) => {
      console.log('SignalR reconnecting:', error);
    });

    this.connection.onreconnected((connectionId) => {
      console.log('SignalR reconnected:', connectionId);
    });

    this.connection.onclose((error) => {
      console.log('SignalR connection closed:', error);
    });

    try {
      await this.connection.start();
      console.log('SignalR connected successfully');
    } catch (error) {
      console.error('Error connecting to SignalR:', error);
      throw error;
    }
  }

  async disconnect(): Promise<void> {
    if (this.connection) {
      await this.connection.stop();
      this.connection = null;
      console.log('SignalR disconnected');
    }
  }

  // Subscribe to market data updates
  onMarketDataUpdate(callback: (data: any) => void): void {
    if (!this.connection) {
      console.error('SignalR connection not established');
      return;
    }

    this.connection.on('MarketDataUpdate', callback);
  }

  // Subscribe to order updates
  onOrderUpdate(callback: (order: any) => void): void {
    if (!this.connection) {
      console.error('SignalR connection not established');
      return;
    }

    this.connection.on('OrderUpdate', callback);
  }

  // Subscribe to position updates
  onPositionUpdate(callback: (position: any) => void): void {
    if (!this.connection) {
      console.error('SignalR connection not established');
      return;
    }

    this.connection.on('PositionUpdate', callback);
  }

  // Subscribe to portfolio updates
  onPortfolioUpdate(callback: (portfolio: any) => void): void {
    if (!this.connection) {
      console.error('SignalR connection not established');
      return;
    }

    this.connection.on('PortfolioUpdate', callback);
  }

  // Subscribe to trade execution updates
  onTradeExecuted(callback: (trade: any) => void): void {
    if (!this.connection) {
      console.error('SignalR connection not established');
      return;
    }

    this.connection.on('TradeExecuted', callback);
  }

  // Subscribe to backtest progress updates
  onBacktestProgress(callback: (progress: any) => void): void {
    if (!this.connection) {
      console.error('SignalR connection not established');
      return;
    }

    this.connection.on('BacktestProgress', callback);
  }

  // Subscribe to ML training updates
  onMLTrainingUpdate(callback: (update: any) => void): void {
    if (!this.connection) {
      console.error('SignalR connection not established');
      return;
    }

    this.connection.on('MLTrainingUpdate', callback);
  }

  // Subscribe to alerts/notifications
  onAlert(callback: (alert: any) => void): void {
    if (!this.connection) {
      console.error('SignalR connection not established');
      return;
    }

    this.connection.on('Alert', callback);
  }

  // Subscribe to specific symbol
  async subscribeToSymbol(symbol: string): Promise<void> {
    if (!this.connection) {
      console.error('SignalR connection not established');
      return;
    }

    try {
      await this.connection.invoke('SubscribeToSymbol', symbol);
      console.log(`Subscribed to ${symbol}`);
    } catch (error) {
      console.error(`Error subscribing to ${symbol}:`, error);
    }
  }

  // Unsubscribe from specific symbol
  async unsubscribeFromSymbol(symbol: string): Promise<void> {
    if (!this.connection) {
      console.error('SignalR connection not established');
      return;
    }

    try {
      await this.connection.invoke('UnsubscribeFromSymbol', symbol);
      console.log(`Unsubscribed from ${symbol}`);
    } catch (error) {
      console.error(`Error unsubscribing from ${symbol}:`, error);
    }
  }

  // Remove all event listeners
  removeAllListeners(): void {
    if (this.connection) {
      this.connection.off('MarketDataUpdate');
      this.connection.off('OrderUpdate');
      this.connection.off('PositionUpdate');
      this.connection.off('PortfolioUpdate');
      this.connection.off('TradeExecuted');
      this.connection.off('BacktestProgress');
      this.connection.off('MLTrainingUpdate');
      this.connection.off('Alert');
    }
  }

  // Get connection state
  get isConnected(): boolean {
    return this.connection?.state === signalR.HubConnectionState.Connected;
  }
}

// Export singleton instance
export const signalRService = new SignalRService();
