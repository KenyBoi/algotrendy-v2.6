'use client';

import React, { useState } from 'react';
import { TrendingUp, TrendingDown, X, Bot, Clock, Target, Shield, BarChart3 } from 'lucide-react';
import { Position, FreqtradePosition } from '@/types';
import { useFreqtradePositions } from '@/hooks/useFreqtrade';
import { EmptyState } from '@/components/ui/EmptyState';

interface PositionsTableProps {
  positions: Position[];
  isLoading: boolean;
  onClose: (positionId: string) => void;
}

type PositionSource = 'all' | 'traditional' | 'freqtrade';

export const PositionsTable: React.FC<PositionsTableProps> = ({
  positions,
  isLoading,
  onClose,
}) => {
  const [activeTab, setActiveTab] = useState<PositionSource>('all');
  const {
    data: freqtradePositions = [],
    isLoading: freqtradeLoading
  } = useFreqtradePositions();

  const combinedLoading = isLoading || freqtradeLoading;

  if (combinedLoading) {
    return (
      <div className="bg-white rounded-lg shadow-md p-6 animate-pulse">
        <div className="space-y-4">
          {[...Array(3)].map((_, i) => (
            <div key={i} className="h-12 bg-gray-200 rounded"></div>
          ))}
        </div>
      </div>
    );
  }

  // Filter positions based on active tab
  const getFilteredPositions = () => {
    switch (activeTab) {
      case 'traditional':
        return positions;
      case 'freqtrade':
        return freqtradePositions;
      case 'all':
      default:
        return [...positions, ...freqtradePositions];
    }
  };

  const filteredPositions = getFilteredPositions();
  const isFreqtradePosition = (pos: Position | FreqtradePosition): pos is FreqtradePosition => {
    return 'freqtrade_trade_id' in pos;
  };

  return (
    <div className="bg-white rounded-lg shadow-md overflow-hidden">
      {/* Tab Navigation */}
      <div className="border-b border-gray-200">
        <nav className="flex">
          {[
            { id: 'all', label: 'All Positions', count: positions.length + freqtradePositions.length },
            { id: 'traditional', label: 'Traditional', count: positions.length },
            { id: 'freqtrade', label: 'Freqtrade Bots', count: freqtradePositions.length }
          ].map((tab) => (
            <button
              key={tab.id}
              onClick={() => setActiveTab(tab.id as PositionSource)}
              className={`px-6 py-4 text-sm font-medium border-b-2 transition-colors ${activeTab === tab.id
                  ? 'border-primary text-primary bg-primary/5'
                  : 'border-transparent text-neutral hover:text-primary hover:border-gray-300'
                }`}
            >
              <div className="flex items-center gap-2">
                {tab.id === 'freqtrade' && <Bot size={16} />}
                {tab.label}
                <span className="bg-gray-100 text-gray-600 px-2 py-0.5 rounded-full text-xs">
                  {tab.count}
                </span>
              </div>
            </button>
          ))}
        </nav>
      </div>

      {/* Table */}
      <div className="overflow-x-auto">
        <table className="w-full">
          <thead className="bg-gray-50 border-b">
            <tr>
              <th className="px-4 py-3 text-left text-sm font-semibold text-primary">Symbol</th>
              <th className="px-4 py-3 text-left text-sm font-semibold text-primary">Source</th>
              <th className="px-4 py-3 text-left text-sm font-semibold text-primary">Side</th>
              <th className="px-4 py-3 text-right text-sm font-semibold text-primary">Entry</th>
              <th className="px-4 py-3 text-right text-sm font-semibold text-primary">Current</th>
              <th className="px-4 py-3 text-right text-sm font-semibold text-primary">Quantity</th>
              <th className="px-4 py-3 text-right text-sm font-semibold text-primary">P&L</th>
              <th className="px-4 py-3 text-right text-sm font-semibold text-primary">Return %</th>
              {(activeTab === 'all' || activeTab === 'freqtrade') && (
                <>
                  <th className="px-4 py-3 text-center text-sm font-semibold text-primary">Duration</th>
                  <th className="px-4 py-3 text-center text-sm font-semibold text-primary">SL/TP</th>
                </>
              )}
              <th className="px-4 py-3 text-center text-sm font-semibold text-primary">Action</th>
            </tr>
          </thead>
          <tbody className="divide-y">
            {filteredPositions.map((position) => {
              const isProfitable = position.pnl >= 0;
              const isFreqtrade = isFreqtradePosition(position);

              return (
                <tr key={position.id} className="hover:bg-gray-50">
                  <td className="px-4 py-4 font-semibold text-primary">{position.symbol}</td>

                  {/* Source Column */}
                  <td className="px-4 py-4">
                    {isFreqtrade ? (
                      <div className="flex items-center gap-1">
                        <Bot size={14} className="text-blue-600" />
                        <span className="text-xs text-blue-600 font-medium">
                          {position.bot_name || 'Bot'}
                        </span>
                      </div>
                    ) : (
                      <span className="text-xs text-gray-600 font-medium">Traditional</span>
                    )}
                  </td>

                  {/* Side Column */}
                  <td className="px-4 py-4">
                    <span
                      className={`inline-flex items-center gap-1 px-2 py-1 rounded-full text-xs font-medium ${position.side === 'long'
                          ? 'bg-success/10 text-success'
                          : 'bg-error/10 text-error'
                        }`}
                    >
                      {position.side === 'long' ? (
                        <TrendingUp size={12} />
                      ) : (
                        <TrendingDown size={12} />
                      )}
                      {position.side.toUpperCase()}
                    </span>
                  </td>

                  <td className="px-4 py-4 text-right">${position.entry_price.toFixed(2)}</td>
                  <td className="px-4 py-4 text-right">${position.current_price.toFixed(2)}</td>
                  <td className="px-4 py-4 text-right">{position.quantity}</td>

                  {/* P&L Column */}
                  <td className={`px-4 py-4 text-right font-semibold ${isProfitable ? 'text-success' : 'text-error'}`}>
                    ${position.pnl.toFixed(2)}
                  </td>

                  {/* Return % Column */}
                  <td className={`px-4 py-4 text-right font-semibold ${isProfitable ? 'text-success' : 'text-error'}`}>
                    {position.pnl_percent.toFixed(2)}%
                  </td>

                  {/* Freqtrade-specific columns */}
                  {(activeTab === 'all' || activeTab === 'freqtrade') && (
                    <>
                      {/* Duration Column */}
                      <td className="px-4 py-4 text-center">
                        {isFreqtrade && position.duration_minutes ? (
                          <div className="flex items-center justify-center gap-1">
                            <Clock size={12} className="text-gray-500" />
                            <span className="text-xs text-gray-600">
                              {position.duration_minutes < 60
                                ? `${position.duration_minutes}m`
                                : `${Math.floor(position.duration_minutes / 60)}h ${position.duration_minutes % 60}m`
                              }
                            </span>
                          </div>
                        ) : (
                          <span className="text-xs text-gray-400">-</span>
                        )}
                      </td>

                      {/* Stop Loss / Take Profit Column */}
                      <td className="px-4 py-4 text-center">
                        {isFreqtrade ? (
                          <div className="flex flex-col gap-1">
                            {position.stop_loss && (
                              <div className="flex items-center justify-center gap-1">
                                <Shield size={10} className="text-red-500" />
                                <span className="text-xs text-red-600">
                                  ${position.stop_loss.toFixed(2)}
                                </span>
                              </div>
                            )}
                            {position.take_profit && (
                              <div className="flex items-center justify-center gap-1">
                                <Target size={10} className="text-green-500" />
                                <span className="text-xs text-green-600">
                                  ${position.take_profit.toFixed(2)}
                                </span>
                              </div>
                            )}
                            {!position.stop_loss && !position.take_profit && (
                              <span className="text-xs text-gray-400">-</span>
                            )}
                          </div>
                        ) : (
                          <span className="text-xs text-gray-400">-</span>
                        )}
                      </td>
                    </>
                  )}

                  {/* Action Column */}
                  <td className="px-4 py-4 text-center">
                    <button
                      onClick={() => onClose(position.id)}
                      className="p-2 hover:bg-error/10 rounded-lg transition-colors text-error"
                      aria-label="Close position"
                      disabled={isFreqtrade} // Disable for Freqtrade positions as they're managed by bots
                    >
                      <X size={14} />
                    </button>
                  </td>
                </tr>
              );
            })}
          </tbody>
        </table>
      </div>

      {/* Empty State */}
      {filteredPositions.length === 0 && !combinedLoading && (
        <EmptyState
          icon={BarChart3}
          title="No Open Positions"
          description={
            activeTab !== 'all'
              ? 'Switch to "All Positions" to see positions from other sources.'
              : 'Start trading to see your positions here. Your active positions will appear once you open trades.'
          }
          actionLabel={activeTab !== 'all' ? 'View All Positions' : undefined}
          onAction={activeTab !== 'all' ? () => setActiveTab('all') : undefined}
        />
      )}

      {/* Summary Footer */}
      {filteredPositions.length > 0 && (
        <div className="border-t border-gray-200 bg-gray-50 px-6 py-4">
          <div className="flex justify-between items-center text-sm">
            <div className="flex gap-6">
              <span className="text-neutral">
                Total Positions: <span className="font-semibold">{filteredPositions.length}</span>
              </span>
              <span className="text-neutral">
                Total P&L: <span className={`font-semibold ${filteredPositions.reduce((sum, pos) => sum + pos.pnl, 0) >= 0 ? 'text-success' : 'text-error'
                  }`}>
                  ${filteredPositions.reduce((sum, pos) => sum + pos.pnl, 0).toFixed(2)}
                </span>
              </span>
            </div>
            {activeTab === 'freqtrade' && (
              <div className="text-xs text-blue-600">
                * Freqtrade positions are managed by bots
              </div>
            )}
          </div>
        </div>
      )}
    </div>
  );
};
