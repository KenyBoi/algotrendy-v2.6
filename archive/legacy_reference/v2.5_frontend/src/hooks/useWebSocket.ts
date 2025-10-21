// React Hook for WebSocket
// COMPLETED: useWebSocket hook for components
// COMPLETED: Auto-connect/disconnect on mount/unmount
// COMPLETED: Connection status tracking
// COMPLETED: Event subscription management
// TODO: Add message buffering
// TODO: Add offline queue

import { useEffect, useState, useCallback, useRef } from 'react';
import { getWebSocketService } from '@/services/websocket';

interface UseWebSocketOptions {
  autoConnect?: boolean;
  onConnect?: () => void;
  onDisconnect?: () => void;
  onReconnecting?: () => void;
}

export const useWebSocket = (options: UseWebSocketOptions = {}) => {
  const {
    autoConnect = true,
    onConnect,
    onDisconnect,
    onReconnecting,
  } = options;

  const [status, setStatus] = useState<'connected' | 'disconnected' | 'reconnecting'>('disconnected');
  const wsRef = useRef(getWebSocketService());
  const unsubscribeStatusRef = useRef<(() => void) | null>(null);

  useEffect(() => {
    const ws = wsRef.current;

    // Set up status change listener
    unsubscribeStatusRef.current = ws.onStatusChange((newStatus) => {
      setStatus(newStatus);

      switch (newStatus) {
        case 'connected':
          onConnect?.();
          break;
        case 'disconnected':
          onDisconnect?.();
          break;
        case 'reconnecting':
          onReconnecting?.();
          break;
      }
    });

    // Auto-connect if enabled
    if (autoConnect && !ws.isConnected()) {
      ws.connect().catch(error => {
        console.error('[useWebSocket] Connection failed:', error);
      });
    }

    // Cleanup on unmount
    return () => {
      if (unsubscribeStatusRef.current) {
        unsubscribeStatusRef.current();
      }
    };
  }, [autoConnect, onConnect, onDisconnect, onReconnecting]);

  const connect = useCallback(() => {
    return wsRef.current.connect();
  }, []);

  const disconnect = useCallback(() => {
    wsRef.current.disconnect();
  }, []);

  const send = useCallback((data: any) => {
    wsRef.current.send(data);
  }, []);

  return {
    status,
    isConnected: status === 'connected',
    connect,
    disconnect,
    send,
    ws: wsRef.current,
  };
};

/**
 * Hook for subscribing to specific WebSocket events
 */
export const useWebSocketEvent = <T = any>(
  event: string,
  handler: (data: T) => void,
  deps: React.DependencyList = []
) => {
  const wsRef = useRef(getWebSocketService());

  useEffect(() => {
    const ws = wsRef.current;
    const unsubscribe = ws.subscribe(event, handler);

    return () => {
      unsubscribe();
    };
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [event, ...deps]);
};

/**
 * Hook for live portfolio data updates
 */
export const useWebSocketPortfolio = (onUpdate: (data: any) => void) => {
  useWebSocketEvent('portfolio:update', onUpdate, [onUpdate]);
};

/**
 * Hook for live position updates
 */
export const useWebSocketPositions = (onUpdate: (data: any) => void) => {
  useWebSocketEvent('positions:update', onUpdate, [onUpdate]);
};

/**
 * Hook for live bot status updates
 */
export const useWebSocketBotStatus = (onUpdate: (data: any) => void) => {
  useWebSocketEvent('bot:status', onUpdate, [onUpdate]);
};

/**
 * Hook for live market data updates
 */
export const useWebSocketMarketData = (symbol: string, onUpdate: (data: any) => void) => {
  const ws = getWebSocketService();

  useEffect(() => {
    // Subscribe to specific symbol
    const unsubscribe = ws.subscribe(`market:${symbol}`, onUpdate);

    // Send subscription request with symbol
    ws.send({
      type: 'subscribe',
      event: 'market',
      symbol,
    });

    return () => {
      unsubscribe();
      ws.send({
        type: 'unsubscribe',
        event: 'market',
        symbol,
      });
    };
  }, [symbol, onUpdate]);
};
