// WebSocket Service for Real-Time Data Streaming
// COMPLETED: WebSocket connection management
// COMPLETED: Auto-reconnect with exponential backoff
// COMPLETED: Event-based message handling
// COMPLETED: Subscription management
// TODO: Add message queue for offline messages
// TODO: Add connection health monitoring
// TODO: Add compression support

type MessageHandler = (data: any) => void;
type ConnectionStatusCallback = (status: 'connected' | 'disconnected' | 'reconnecting') => void;

interface WebSocketConfig {
  url: string;
  reconnectInterval?: number;
  maxReconnectAttempts?: number;
  heartbeatInterval?: number;
}

export class WebSocketService {
  private ws: WebSocket | null = null;
  private config: Required<WebSocketConfig>;
  private messageHandlers: Map<string, Set<MessageHandler>> = new Map();
  private reconnectAttempts = 0;
  private reconnectTimeout: NodeJS.Timeout | null = null;
  private heartbeatInterval: NodeJS.Timeout | null = null;
  private statusCallbacks: Set<ConnectionStatusCallback> = new Set();
  private isManualClose = false;

  constructor(config: WebSocketConfig) {
    this.config = {
      url: config.url,
      reconnectInterval: config.reconnectInterval || 5000,
      maxReconnectAttempts: config.maxReconnectAttempts || 10,
      heartbeatInterval: config.heartbeatInterval || 30000,
    };
  }

  /**
   * Connect to WebSocket server
   */
  connect(): Promise<void> {
    return new Promise((resolve, reject) => {
      try {
        this.isManualClose = false;
        this.ws = new WebSocket(this.config.url);

        this.ws.onopen = () => {
          console.log('[WebSocket] Connected');
          this.reconnectAttempts = 0;
          this.notifyStatusChange('connected');
          this.startHeartbeat();
          resolve();
        };

        this.ws.onclose = () => {
          console.log('[WebSocket] Disconnected');
          this.notifyStatusChange('disconnected');
          this.stopHeartbeat();

          if (!this.isManualClose) {
            this.attemptReconnect();
          }
        };

        this.ws.onerror = (error) => {
          console.error('[WebSocket] Error:', error);
          reject(error);
        };

        this.ws.onmessage = (event) => {
          this.handleMessage(event.data);
        };
      } catch (error) {
        console.error('[WebSocket] Connection failed:', error);
        reject(error);
      }
    });
  }

  /**
   * Disconnect from WebSocket server
   */
  disconnect(): void {
    this.isManualClose = true;
    this.stopHeartbeat();

    if (this.reconnectTimeout) {
      clearTimeout(this.reconnectTimeout);
      this.reconnectTimeout = null;
    }

    if (this.ws) {
      this.ws.close();
      this.ws = null;
    }
  }

  /**
   * Subscribe to a specific event type
   */
  subscribe(event: string, handler: MessageHandler): () => void {
    if (!this.messageHandlers.has(event)) {
      this.messageHandlers.set(event, new Set());
    }

    this.messageHandlers.get(event)!.add(handler);

    // Send subscription message to server
    this.send({
      type: 'subscribe',
      event,
    });

    // Return unsubscribe function
    return () => this.unsubscribe(event, handler);
  }

  /**
   * Unsubscribe from an event
   */
  unsubscribe(event: string, handler: MessageHandler): void {
    const handlers = this.messageHandlers.get(event);
    if (handlers) {
      handlers.delete(handler);

      if (handlers.size === 0) {
        this.messageHandlers.delete(event);

        // Send unsubscribe message to server
        this.send({
          type: 'unsubscribe',
          event,
        });
      }
    }
  }

  /**
   * Send data to WebSocket server
   */
  send(data: any): void {
    if (this.ws && this.ws.readyState === WebSocket.OPEN) {
      this.ws.send(JSON.stringify(data));
    } else {
      console.warn('[WebSocket] Cannot send message, not connected');
    }
  }

  /**
   * Register connection status callback
   */
  onStatusChange(callback: ConnectionStatusCallback): () => void {
    this.statusCallbacks.add(callback);
    return () => this.statusCallbacks.delete(callback);
  }

  /**
   * Get current connection status
   */
  isConnected(): boolean {
    return this.ws !== null && this.ws.readyState === WebSocket.OPEN;
  }

  // Private methods

  private handleMessage(data: string): void {
    try {
      const message = JSON.parse(data);
      const { type, event, payload } = message;

      // Handle heartbeat/pong
      if (type === 'pong') {
        return;
      }

      // Notify event subscribers
      if (event) {
        const handlers = this.messageHandlers.get(event);
        if (handlers) {
          handlers.forEach(handler => handler(payload));
        }
      }

      // Notify wildcard subscribers (listen to all events)
      const wildcardHandlers = this.messageHandlers.get('*');
      if (wildcardHandlers) {
        wildcardHandlers.forEach(handler => handler(message));
      }
    } catch (error) {
      console.error('[WebSocket] Failed to parse message:', error);
    }
  }

  private attemptReconnect(): void {
    if (this.reconnectAttempts >= this.config.maxReconnectAttempts) {
      console.error('[WebSocket] Max reconnect attempts reached');
      return;
    }

    this.reconnectAttempts++;
    this.notifyStatusChange('reconnecting');

    // Exponential backoff
    const delay = Math.min(
      this.config.reconnectInterval * Math.pow(2, this.reconnectAttempts - 1),
      30000 // Max 30 seconds
    );

    console.log(`[WebSocket] Reconnecting in ${delay}ms (attempt ${this.reconnectAttempts})`);

    this.reconnectTimeout = setTimeout(() => {
      this.connect().catch(error => {
        console.error('[WebSocket] Reconnect failed:', error);
      });
    }, delay);
  }

  private startHeartbeat(): void {
    this.heartbeatInterval = setInterval(() => {
      if (this.isConnected()) {
        this.send({ type: 'ping' });
      }
    }, this.config.heartbeatInterval);
  }

  private stopHeartbeat(): void {
    if (this.heartbeatInterval) {
      clearInterval(this.heartbeatInterval);
      this.heartbeatInterval = null;
    }
  }

  private notifyStatusChange(status: 'connected' | 'disconnected' | 'reconnecting'): void {
    this.statusCallbacks.forEach(callback => callback(status));
  }
}

// Create singleton instance
let wsInstance: WebSocketService | null = null;

export const getWebSocketService = (): WebSocketService => {
  if (!wsInstance) {
    const wsUrl = process.env.NEXT_PUBLIC_WS_URL || 'ws://localhost:8000/ws';
    wsInstance = new WebSocketService({ url: wsUrl });
  }
  return wsInstance;
};
