'use client';

import React from 'react';
import { TrendingUp, TrendingDown, Bot, Activity, DollarSign } from 'lucide-react';
import { Portfolio } from '@/types';
import { useFreqtradeData } from '@/hooks/useFreqtrade';
import { EmptyState } from '@/components/ui/EmptyState';

interface PortfolioCardProps {
  portfolio: Portfolio | null;
  isLoading: boolean;
}

export const PortfolioCard: React.FC<PortfolioCardProps> = ({ portfolio, isLoading }) => {
  const {
    data: freqtradeData,
    isLoading: freqtradeLoading,
    totalActiveBots,
    totalPnL: freqtradePnL,
    totalOpenTrades,
    bestPerformingBot
  } = useFreqtradeData();

  const combinedLoading = isLoading || freqtradeLoading;

  if (combinedLoading) {
    return (
      <div className="card animate-pulse">
        <div className="h-4 bg-slate-700 rounded w-1/4 mb-4"></div>
        <div className="h-8 bg-slate-700 rounded w-1/3 mb-4"></div>
        <div className="grid grid-cols-2 gap-4">
          <div className="h-20 bg-slate-700 rounded"></div>
          <div className="h-20 bg-slate-700 rounded"></div>
        </div>
      </div>
    );
  }

  if (!portfolio && !freqtradeData.portfolio) {
    return (
      <div className="card">
        <EmptyState
          icon={DollarSign}
          title="No Portfolio Data"
          description="Connect your trading accounts to start tracking your portfolio performance."
          actionLabel="Configure API Keys"
          onAction={() => window.location.href = '/settings?tab=api'}
        />
      </div>
    );
  }

  const isProfitable = (portfolio?.unrealized_pnl || 0) >= 0;
  const isFreqtradeProfitable = freqtradePnL >= 0;
  const combinedPnL = (portfolio?.unrealized_pnl || 0) + freqtradePnL;
  const isCombinedProfitable = combinedPnL >= 0;

  return (
    <div className="card animate-slide-up">
      {/* Header with Combined Stats */}
      <div className="mb-6">
        <h3 className="text-lg font-semibold text-slate-100 mb-2">Portfolio Overview</h3>
        <div className="grid grid-cols-1 md:grid-cols-3 gap-4">
          {/* Combined P&L */}
          <div className="bg-slate-800/50 backdrop-blur-sm rounded-xl p-4 border border-slate-700/50 hover:border-slate-600/70 transition-all duration-300">
            <p className="text-slate-400 text-sm font-medium mb-1">Combined P&L</p>
            <div className="flex items-center gap-2">
              <p className={`text-xl font-bold ${isCombinedProfitable ? 'text-success' : 'text-error'}`}>
                ${combinedPnL.toLocaleString('en-US', { maximumFractionDigits: 2 })}
              </p>
              {isCombinedProfitable ? (
                <TrendingUp size={18} className="text-success" />
              ) : (
                <TrendingDown size={18} className="text-error" />
              )}
            </div>
          </div>

          {/* Active Freqtrade Bots */}
          <div className="bg-blue-900/30 backdrop-blur-sm rounded-xl p-4 border border-blue-700/50 hover:border-blue-600/70 transition-all duration-300">
            <p className="text-slate-400 text-sm font-medium mb-1">Active Bots</p>
            <div className="flex items-center gap-2">
              <Bot size={18} className="text-blue-400" />
              <p className="text-xl font-bold text-blue-300">{totalActiveBots}</p>
              <span className="text-sm text-slate-400">/ {freqtradeData.bots.length}</span>
            </div>
          </div>

          {/* Open Trades */}
          <div className="bg-emerald-900/30 backdrop-blur-sm rounded-xl p-4 border border-emerald-700/50 hover:border-emerald-600/70 transition-all duration-300">
            <p className="text-slate-400 text-sm font-medium mb-1">Open Trades</p>
            <div className="flex items-center gap-2">
              <Activity size={18} className="text-emerald-400" />
              <p className="text-xl font-bold text-emerald-300">{totalOpenTrades}</p>
            </div>
          </div>
        </div>
      </div>

      {/* Traditional Portfolio Section */}
      {portfolio && (
        <div className="mb-6">
          <div className="flex items-center gap-2 mb-3">
            <DollarSign size={18} className="text-primary" />
            <h4 className="font-medium text-primary">Traditional Portfolio</h4>
          </div>
          <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-4 gap-4">
            {/* Total Value */}
            <div>
              <p className="text-neutral text-sm font-medium mb-1">Total Value</p>
              <p className="text-lg font-bold text-primary">
                ${portfolio.total_value.toLocaleString('en-US', { maximumFractionDigits: 2 })}
              </p>
            </div>

            {/* Cash */}
            <div>
              <p className="text-neutral text-sm font-medium mb-1">Cash</p>
              <p className="text-lg font-bold text-primary">
                ${portfolio.cash.toLocaleString('en-US', { maximumFractionDigits: 2 })}
              </p>
            </div>

            {/* Unrealized P&L */}
            <div>
              <p className="text-neutral text-sm font-medium mb-1">Unrealized P&L</p>
              <div className="flex items-center gap-2">
                <p className={`text-lg font-bold ${isProfitable ? 'text-success' : 'text-error'}`}>
                  ${portfolio.unrealized_pnl.toLocaleString('en-US', { maximumFractionDigits: 2 })}
                </p>
                {isProfitable ? (
                  <TrendingUp size={16} className="text-success" />
                ) : (
                  <TrendingDown size={16} className="text-error" />
                )}
              </div>
            </div>

            {/* Buying Power */}
            <div>
              <p className="text-neutral text-sm font-medium mb-1">Buying Power</p>
              <p className="text-lg font-bold text-primary">
                ${portfolio.buying_power.toLocaleString('en-US', { maximumFractionDigits: 2 })}
              </p>
            </div>
          </div>
        </div>
      )}

      {/* Freqtrade Portfolio Section */}
      {freqtradeData.portfolio && (
        <div>
          <div className="flex items-center gap-2 mb-3">
            <Bot size={18} className="text-blue-600" />
            <h4 className="font-medium text-blue-600">Freqtrade Portfolio</h4>
          </div>
          <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-4 gap-4">
            {/* Total Balance */}
            <div>
              <p className="text-neutral text-sm font-medium mb-1">Total Balance</p>
              <p className="text-lg font-bold text-blue-600">
                ${freqtradeData.portfolio.total_balance.toLocaleString('en-US', { maximumFractionDigits: 2 })}
              </p>
            </div>

            {/* Total Profit */}
            <div>
              <p className="text-neutral text-sm font-medium mb-1">Total Profit</p>
              <div className="flex items-center gap-2">
                <p className={`text-lg font-bold ${isFreqtradeProfitable ? 'text-success' : 'text-error'}`}>
                  ${freqtradePnL.toLocaleString('en-US', { maximumFractionDigits: 2 })}
                </p>
                {isFreqtradeProfitable ? (
                  <TrendingUp size={16} className="text-success" />
                ) : (
                  <TrendingDown size={16} className="text-error" />
                )}
              </div>
            </div>

            {/* Best Performing Bot */}
            <div>
              <p className="text-neutral text-sm font-medium mb-1">Best Bot</p>
              <p className="text-lg font-bold text-blue-600">
                {bestPerformingBot?.name || 'N/A'}
              </p>
              {bestPerformingBot && (
                <p className="text-xs text-success">
                  +${bestPerformingBot.profit.toLocaleString('en-US', { maximumFractionDigits: 2 })}
                </p>
              )}
            </div>

            {/* Win Rate */}
            <div>
              <p className="text-neutral text-sm font-medium mb-1">Avg Win Rate</p>
              <p className="text-lg font-bold text-blue-600">
                {freqtradeData.bots.length > 0
                  ? `${(freqtradeData.bots.reduce((sum, bot) => sum + bot.win_rate, 0) / freqtradeData.bots.length).toFixed(1)}%`
                  : 'N/A'
                }
              </p>
            </div>
          </div>
        </div>
      )}

      {/* Bot Status Indicators */}
      {freqtradeData.bots.length > 0 && (
        <div className="mt-6 pt-4 border-t border-gray-200">
          <h5 className="text-sm font-medium text-neutral mb-2">Bot Status</h5>
          <div className="flex gap-2 flex-wrap">
            {freqtradeData.bots.map((bot) => (
              <div
                key={bot.name}
                className={`px-3 py-1 rounded-full text-xs font-medium ${bot.status === 'online'
                    ? 'bg-green-100 text-green-800'
                    : 'bg-red-100 text-red-800'
                  }`}
              >
                {bot.name} ({bot.status})
              </div>
            ))}
          </div>
        </div>
      )}
    </div>
  );
};
