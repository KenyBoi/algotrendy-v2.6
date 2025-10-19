// COMPLETED: Bot control panel component
// COMPLETED: Start/Stop/Restart bot actions
// COMPLETED: Bot status indicators
// COMPLETED: Confirmation dialogs for actions
// TODO: Connect to actual backend API endpoints
// TODO: Add real-time status updates via WebSocket
// TODO: Add bot configuration quick edit
// TODO: Add bot logs viewer modal
// TODO: Implement optimistic UI updates
import React, { useState } from 'react';
import { Play, Square, RotateCcw, Settings, Activity, AlertCircle } from 'lucide-react';

interface Bot {
  name: string;
  status: string;
  profit: number;
  trades: number;
  win_rate: number;
  strategy?: string;
}

interface BotControlPanelProps {
  bots: Bot[];
  onAction?: (botName: string, action: 'start' | 'stop' | 'restart') => Promise<void>;
}

export const BotControlPanel: React.FC<BotControlPanelProps> = ({ bots, onAction }) => {
  const [actionLoading, setActionLoading] = useState<{ [key: string]: boolean }>({});
  const [confirmAction, setConfirmAction] = useState<{ botName: string; action: string } | null>(null);

  const handleAction = async (botName: string, action: 'start' | 'stop' | 'restart') => {
    // Show confirmation for stop/restart actions
    if (action !== 'start') {
      setConfirmAction({ botName, action });
      return;
    }

    await executeAction(botName, action);
  };

  const executeAction = async (botName: string, action: 'start' | 'stop' | 'restart') => {
    const key = `${botName}-${action}`;
    setActionLoading({ ...actionLoading, [key]: true });

    try {
      if (onAction) {
        await onAction(botName, action);
      } else {
        // Mock API call
        await new Promise(resolve => setTimeout(resolve, 1000));
        console.log(`${action} ${botName}`);
      }
    } catch (error) {
      console.error(`Failed to ${action} bot:`, error);
      alert(`Failed to ${action} ${botName}`);
    } finally {
      setActionLoading({ ...actionLoading, [key]: false });
      setConfirmAction(null);
    }
  };

  const confirmAndExecute = () => {
    if (confirmAction) {
      executeAction(confirmAction.botName, confirmAction.action as any);
    }
  };

  return (
    <div className="bg-white rounded-lg shadow-md p-6">
      <div className="flex items-center justify-between mb-6">
        <div>
          <h3 className="text-lg font-semibold text-primary">Bot Control Panel</h3>
          <p className="text-sm text-neutral mt-1">Manage your trading bots</p>
        </div>
        <div className="flex items-center gap-2 px-3 py-1 bg-blue-50 text-blue-700 rounded-lg">
          <Activity size={16} />
          <span className="text-sm font-medium">{bots.filter(b => b.status === 'online').length} Active</span>
        </div>
      </div>

      <div className="space-y-3">
        {bots.map((bot, index) => {
          const isOnline = bot.status === 'online';
          const startLoading = actionLoading[`${bot.name}-start`];
          const stopLoading = actionLoading[`${bot.name}-stop`];
          const restartLoading = actionLoading[`${bot.name}-restart`];

          return (
            <div
              key={index}
              className="border border-gray-200 rounded-lg p-4 hover:border-primary transition-colors"
            >
              <div className="flex items-center justify-between">
                <div className="flex items-center gap-3 flex-1">
                  <div className={`w-3 h-3 rounded-full ${isOnline ? 'bg-green-500 animate-pulse' : 'bg-gray-400'}`} />
                  <div className="flex-1">
                    <div className="flex items-center gap-2">
                      <h4 className="font-semibold text-primary">{bot.name}</h4>
                      {bot.strategy && (
                        <span className="text-xs px-2 py-1 bg-gray-100 rounded">
                          {bot.strategy}
                        </span>
                      )}
                    </div>
                    <div className="flex items-center gap-4 mt-1 text-sm text-neutral">
                      <span className={bot.profit >= 0 ? 'text-green-600' : 'text-red-600'}>
                        ${bot.profit.toFixed(2)}
                      </span>
                      <span>{bot.trades} trades</span>
                      <span>{bot.win_rate}% WR</span>
                    </div>
                  </div>
                </div>

                <div className="flex items-center gap-2">
                  {!isOnline ? (
                    <button
                      onClick={() => handleAction(bot.name, 'start')}
                      disabled={startLoading}
                      className="flex items-center gap-2 px-3 py-2 bg-green-600 text-white rounded-lg hover:bg-green-700 disabled:opacity-50 disabled:cursor-not-allowed transition-colors"
                    >
                      <Play size={16} />
                      {startLoading ? 'Starting...' : 'Start'}
                    </button>
                  ) : (
                    <>
                      <button
                        onClick={() => handleAction(bot.name, 'stop')}
                        disabled={stopLoading}
                        className="flex items-center gap-2 px-3 py-2 bg-red-600 text-white rounded-lg hover:bg-red-700 disabled:opacity-50 disabled:cursor-not-allowed transition-colors"
                      >
                        <Square size={16} />
                        {stopLoading ? 'Stopping...' : 'Stop'}
                      </button>
                      <button
                        onClick={() => handleAction(bot.name, 'restart')}
                        disabled={restartLoading}
                        className="flex items-center gap-2 px-3 py-2 bg-orange-600 text-white rounded-lg hover:bg-orange-700 disabled:opacity-50 disabled:cursor-not-allowed transition-colors"
                      >
                        <RotateCcw size={16} />
                        {restartLoading ? 'Restarting...' : 'Restart'}
                      </button>
                    </>
                  )}
                  <button className="p-2 text-gray-600 hover:bg-gray-100 rounded-lg transition-colors">
                    <Settings size={18} />
                  </button>
                </div>
              </div>
            </div>
          );
        })}
      </div>

      {bots.length === 0 && (
        <div className="text-center py-12">
          <AlertCircle size={48} className="text-gray-400 mx-auto mb-3" />
          <p className="text-neutral">No bots available</p>
          <button className="mt-4 px-4 py-2 bg-primary text-white rounded-lg hover:bg-primary/90 transition-colors">
            Add Bot
          </button>
        </div>
      )}

      {/* Confirmation Modal */}
      {confirmAction && (
        <div className="fixed inset-0 bg-black bg-opacity-50 flex items-center justify-center z-50">
          <div className="bg-white rounded-lg p-6 max-w-md w-full mx-4">
            <div className="flex items-center gap-3 mb-4">
              <AlertCircle size={24} className="text-orange-500" />
              <h3 className="text-lg font-semibold text-primary">Confirm Action</h3>
            </div>
            <p className="text-neutral mb-6">
              Are you sure you want to <strong>{confirmAction.action}</strong> the bot <strong>{confirmAction.botName}</strong>?
              {confirmAction.action === 'stop' && (
                <span className="block mt-2 text-sm text-red-600">
                  This will close all open positions for this bot.
                </span>
              )}
            </p>
            <div className="flex gap-3 justify-end">
              <button
                onClick={() => setConfirmAction(null)}
                className="px-4 py-2 border border-gray-300 rounded-lg hover:bg-gray-100 transition-colors"
              >
                Cancel
              </button>
              <button
                onClick={confirmAndExecute}
                className="px-4 py-2 bg-orange-600 text-white rounded-lg hover:bg-orange-700 transition-colors"
              >
                Confirm {confirmAction.action}
              </button>
            </div>
          </div>
        </div>
      )}
    </div>
  );
};
