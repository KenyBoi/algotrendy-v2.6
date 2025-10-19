// WebSocket Connection Status Indicator
// COMPLETED: Visual connection status indicator
// COMPLETED: Auto-connect/reconnect display
// COMPLETED: Tooltip with connection info
// TODO: Add manual reconnect button
// TODO: Add connection metrics (latency, uptime)

import React from 'react';
import { Wifi, WifiOff, RefreshCw } from 'lucide-react';
import { useWebSocket } from '@/hooks/useWebSocket';

interface WebSocketStatusProps {
  showLabel?: boolean;
  className?: string;
}

export const WebSocketStatus: React.FC<WebSocketStatusProps> = ({
  showLabel = false,
  className = '',
}) => {
  const { status, isConnected, connect } = useWebSocket({
    autoConnect: true,
  });

  const getStatusConfig = () => {
    switch (status) {
      case 'connected':
        return {
          icon: Wifi,
          color: 'text-success-light',
          bgColor: 'bg-success/20',
          label: 'Live',
          tooltip: 'Connected to live data stream',
          animate: '',
        };
      case 'reconnecting':
        return {
          icon: RefreshCw,
          color: 'text-warning-light',
          bgColor: 'bg-warning/20',
          label: 'Reconnecting',
          tooltip: 'Reconnecting to server...',
          animate: 'animate-spin',
        };
      case 'disconnected':
      default:
        return {
          icon: WifiOff,
          color: 'text-error-light',
          bgColor: 'bg-error/20',
          label: 'Offline',
          tooltip: 'Disconnected from server',
          animate: '',
        };
    }
  };

  const config = getStatusConfig();
  const Icon = config.icon;

  return (
    <div
      className={`flex items-center gap-2 px-3 py-1.5 rounded-full ${config.bgColor} border border-${config.color}/20 transition-all duration-300 ${className}`}
      title={config.tooltip}
    >
      <Icon size={14} className={`${config.color} ${config.animate}`} />

      {showLabel && (
        <span className={`text-xs font-medium ${config.color}`}>
          {config.label}
        </span>
      )}

      {/* Pulse animation for connected state */}
      {isConnected && (
        <span className="relative flex h-2 w-2">
          <span className="animate-ping absolute inline-flex h-full w-full rounded-full bg-success opacity-75"></span>
          <span className="relative inline-flex rounded-full h-2 w-2 bg-success-light"></span>
        </span>
      )}
    </div>
  );
};

/**
 * Larger WebSocket status card for settings or dashboard
 */
export const WebSocketStatusCard: React.FC = () => {
  const { status, isConnected, connect, disconnect } = useWebSocket({
    autoConnect: true,
  });

  const handleReconnect = () => {
    disconnect();
    setTimeout(() => {
      connect().catch(console.error);
    }, 100);
  };

  return (
    <div className="card">
      <div className="flex items-center justify-between mb-4">
        <div>
          <h4 className="text-lg font-semibold text-slate-100">Live Data Connection</h4>
          <p className="text-sm text-slate-400">Real-time market and portfolio updates</p>
        </div>
        <WebSocketStatus showLabel />
      </div>

      <div className="space-y-3">
        <div className="flex items-center justify-between py-2 border-b border-slate-700/50">
          <span className="text-sm text-slate-400">Status</span>
          <span className={`text-sm font-medium ${
            isConnected ? 'text-success-light' :
            status === 'reconnecting' ? 'text-warning-light' :
            'text-error-light'
          }`}>
            {status.charAt(0).toUpperCase() + status.slice(1)}
          </span>
        </div>

        <div className="flex items-center justify-between py-2 border-b border-slate-700/50">
          <span className="text-sm text-slate-400">Protocol</span>
          <span className="text-sm font-medium text-slate-300">WebSocket</span>
        </div>

        <div className="flex items-center justify-between py-2">
          <span className="text-sm text-slate-400">Endpoint</span>
          <span className="text-xs font-mono text-slate-300">
            {process.env.NEXT_PUBLIC_WS_URL || 'ws://localhost:8000/ws'}
          </span>
        </div>

        {!isConnected && status !== 'reconnecting' && (
          <button
            onClick={handleReconnect}
            className="w-full btn-primary py-2 flex items-center justify-center gap-2"
          >
            <RefreshCw size={16} />
            Reconnect
          </button>
        )}

        {isConnected && (
          <div className="bg-success/10 border border-success/20 rounded-lg p-3 text-center">
            <p className="text-xs text-success-light">
              ✓ Connected - Receiving live updates
            </p>
          </div>
        )}
      </div>
    </div>
  );
};
