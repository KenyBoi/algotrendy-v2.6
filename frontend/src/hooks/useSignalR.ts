import { useEffect, useState } from 'react';
import { signalRService } from '../services/signalr';

export function useSignalR() {
  const [isConnected, setIsConnected] = useState(false);

  useEffect(() => {
    const connect = async () => {
      try {
        await signalRService.connect();
        setIsConnected(true);
      } catch (error) {
        console.error('Failed to connect to SignalR:', error);
        setIsConnected(false);
      }
    };

    connect();

    return () => {
      signalRService.disconnect();
      setIsConnected(false);
    };
  }, []);

  return { isConnected, signalRService };
}

// Hook for market data subscription
export function useMarketData(callback: (data: any) => void) {
  const { isConnected } = useSignalR();

  useEffect(() => {
    if (isConnected) {
      signalRService.onMarketDataUpdate(callback);
    }

    return () => {
      signalRService.removeAllListeners();
    };
  }, [isConnected, callback]);

  return { isConnected };
}

// Hook for order updates
export function useOrderUpdates(callback: (order: any) => void) {
  const { isConnected } = useSignalR();

  useEffect(() => {
    if (isConnected) {
      signalRService.onOrderUpdate(callback);
    }

    return () => {
      signalRService.removeAllListeners();
    };
  }, [isConnected, callback]);

  return { isConnected };
}

// Hook for position updates
export function usePositionUpdates(callback: (position: any) => void) {
  const { isConnected } = useSignalR();

  useEffect(() => {
    if (isConnected) {
      signalRService.onPositionUpdate(callback);
    }

    return () => {
      signalRService.removeAllListeners();
    };
  }, [isConnected, callback]);

  return { isConnected };
}

// Hook for portfolio updates
export function usePortfolioUpdates(callback: (portfolio: any) => void) {
  const { isConnected } = useSignalR();

  useEffect(() => {
    if (isConnected) {
      signalRService.onPortfolioUpdate(callback);
    }

    return () => {
      signalRService.removeAllListeners();
    };
  }, [isConnected, callback]);

  return { isConnected };
}

// Hook for symbol subscription
export function useSymbolSubscription(symbol: string) {
  const { isConnected } = useSignalR();

  useEffect(() => {
    if (isConnected && symbol) {
      signalRService.subscribeToSymbol(symbol);

      return () => {
        signalRService.unsubscribeFromSymbol(symbol);
      };
    }
  }, [isConnected, symbol]);

  return { isConnected };
}
