'use client';

// COMPLETED: Dashboard has full Header and Sidebar navigation
// COMPLETED: Real-time portfolio and positions data refresh every 30 seconds
// COMPLETED: Freqtrade bot data integration with useFreqtradeData hook
// COMPLETED: Bot filtering functionality (all bots or individual bot selection)
// COMPLETED: Quick stats cards showing Active Bots, Open Trades, and Total P&L
// COMPLETED: Manual refresh button for both traditional and Freqtrade data
// COMPLETED: Best performing bot highlight section
// COMPLETED: Bot status indicators (online/offline)
// TODO: Add real-time WebSocket updates for positions and bot status
// TODO: Implement chart visualizations for portfolio performance over time
// TODO: Add trade execution interface for manual trading
// TODO: Implement position size calculator with risk management
// TODO: Add risk management warnings and alerts (stop loss, take profit)
// TODO: Create custom dashboard widgets system (drag and drop)
// TODO: Add bot performance comparison charts
// TODO: Implement bot control actions (start/stop/pause bots)
// TODO: Add advanced analytics dashboard for strategy performance
import React, { useState, useEffect } from 'react';
import { Header } from '@/components/layout/Header';
import { Sidebar } from '@/components/layout/Sidebar';
import { PortfolioCard } from '@/components/dashboard/PortfolioCard';
import { PositionsTable } from '@/components/dashboard/PositionsTable';
import { PerformanceChart } from '@/components/dashboard/PerformanceChart';
import { BotPerformanceChart } from '@/components/dashboard/BotPerformanceChart';
import { BotControlPanel } from '@/components/dashboard/BotControlPanel';
import { LivePriceTicker } from '@/components/dashboard/LivePriceTicker';
import { useAuthStore } from '@/store/authStore';
import { useTradingStore } from '@/store/tradingStore';
import { useFreqtradeData } from '@/hooks/useFreqtrade';
import { apiService } from '@/services/api';
import { Bot, RefreshCw, TrendingUp, Activity } from 'lucide-react';

export default function Dashboard() {
  const [sidebarOpen, setSidebarOpen] = useState(false);
  const [selectedBotFilter, setSelectedBotFilter] = useState<string>('all');
  const [refreshing, setRefreshing] = useState(false);

  const { user } = useAuthStore();
  const {
    portfolio,
    positions,
    isLoading,
    setPortfolio,
    setPositions,
    setLoading,
    setError,
  } = useTradingStore();

  // Freqtrade data integration
  const {
    data: freqtradeData,
    isLoading: freqtradeLoading,
    totalActiveBots,
    totalPnL,
    totalOpenTrades,
    bestPerformingBot,
    refetchAll: refetchFreqtradeData,
  } = useFreqtradeData(selectedBotFilter === 'all' ? undefined : selectedBotFilter);

  // Redirect to login if not authenticated
  useEffect(() => {
    if (!user) {
      window.location.href = '/login';
    }
  }, [user]);

  // Fetch portfolio data on mount
  useEffect(() => {
    const fetchData = async () => {
      setLoading(true);
      try {
        const [portfolioRes, positionsRes] = await Promise.all([
          apiService.getPortfolio(),
          apiService.getPositions(),
        ]);

        if (portfolioRes.data) {
          setPortfolio(portfolioRes.data);
        }
        if (positionsRes.data) {
          setPositions(positionsRes.data);
        }
      } catch (error) {
        setError('Failed to fetch portfolio data');
        console.error(error);
      } finally {
        setLoading(false);
      }
    };

    fetchData();
    // Refresh data every 30 seconds
    const interval = setInterval(fetchData, 30000);
    return () => clearInterval(interval);
  }, [setPortfolio, setPositions, setLoading, setError]);

  // Manual refresh function for both traditional and Freqtrade data
  const handleManualRefresh = async () => {
    setRefreshing(true);
    try {
      // Refresh traditional data
      const [portfolioRes, positionsRes] = await Promise.all([
        apiService.getPortfolio(),
        apiService.getPositions(),
      ]);

      if (portfolioRes.data) {
        setPortfolio(portfolioRes.data);
      }
      if (positionsRes.data) {
        setPositions(positionsRes.data);
      }

      // Refresh Freqtrade data
      await refetchFreqtradeData();
    } catch (error) {
      setError('Failed to refresh data');
      console.error(error);
    } finally {
      setRefreshing(false);
    }
  };

  const handleClosePosition = async (positionId: string) => {
    try {
      await apiService.closePosition(positionId);
      // Refresh positions after closing
      const res = await apiService.getPositions();
      if (res.data) {
        setPositions(res.data);
      }
    } catch (error) {
      setError('Failed to close position');
      console.error(error);
    }
  };

  return (
    <div className="flex flex-col h-screen bg-background">
      <Header onMenuClick={() => setSidebarOpen(!sidebarOpen)} />
      <div className="flex flex-1 overflow-hidden">
        <Sidebar isOpen={sidebarOpen} onClose={() => setSidebarOpen(false)} />

        {/* Main content */}
        <main className="flex-1 overflow-auto">
          <div className="p-6 max-w-7xl mx-auto">
            {/* Header with Stats and Actions */}
            <div className="mb-8 animate-fade-in">
              <div className="flex flex-col lg:flex-row lg:items-center lg:justify-between gap-4">
                <div>
                  <h1 className="text-4xl font-bold text-slate-100 mb-2">Dashboard</h1>
                  <p className="text-slate-400">Welcome back, {user?.name}! Here's your trading overview.</p>
                </div>

                {/* Quick Stats and Actions */}
                <div className="flex flex-col sm:flex-row gap-4">
                  {/* Quick Stats Cards */}
                  <div className="flex gap-4">
                    <div className="bg-blue-900/30 backdrop-blur-sm border border-blue-700/50 rounded-xl p-3 min-w-[120px] hover:border-blue-600/70 transition-all duration-300">
                      <div className="flex items-center gap-2">
                        <Bot size={18} className="text-blue-400" />
                        <div>
                          <p className="text-xs text-slate-400">Active Bots</p>
                          <p className="text-lg font-bold text-blue-300">{totalActiveBots}</p>
                        </div>
                      </div>
                    </div>

                    <div className="bg-emerald-900/30 backdrop-blur-sm border border-emerald-700/50 rounded-xl p-3 min-w-[120px] hover:border-emerald-600/70 transition-all duration-300">
                      <div className="flex items-center gap-2">
                        <Activity size={18} className="text-emerald-400" />
                        <div>
                          <p className="text-xs text-slate-400">Open Trades</p>
                          <p className="text-lg font-bold text-emerald-300">{totalOpenTrades}</p>
                        </div>
                      </div>
                    </div>

                    <div className={`backdrop-blur-sm border rounded-xl p-3 min-w-[120px] transition-all duration-300 ${totalPnL >= 0 ? 'bg-emerald-900/30 border-emerald-700/50 hover:border-emerald-600/70' : 'bg-red-900/30 border-red-700/50 hover:border-red-600/70'}`}>
                      <div className="flex items-center gap-2">
                        <TrendingUp size={18} className={totalPnL >= 0 ? 'text-emerald-400' : 'text-red-400'} />
                        <div>
                          <p className="text-xs text-slate-400">Total P&L</p>
                          <p className={`text-lg font-bold ${totalPnL >= 0 ? 'text-emerald-300' : 'text-red-300'}`}>
                            ${totalPnL.toFixed(2)}
                          </p>
                        </div>
                      </div>
                    </div>
                  </div>

                  {/* Actions */}
                  <div className="flex gap-2">
                    <button
                      onClick={handleManualRefresh}
                      disabled={refreshing || isLoading || freqtradeLoading}
                      className="btn-primary flex items-center gap-2 px-4 py-2 rounded-xl disabled:opacity-50 disabled:cursor-not-allowed shadow-lg hover:shadow-accent/30"
                    >
                      <RefreshCw size={16} className={refreshing ? 'animate-spin' : ''} />
                      Refresh
                    </button>
                  </div>
                </div>
              </div>
            </div>

            {/* Bot Filter Selection */}
            {freqtradeData.bots.length > 0 && (
              <div className="mb-6 animate-slide-up">
                <div className="card">
                  <h3 className="text-sm font-medium text-slate-300 mb-3">Filter by Bot</h3>
                  <div className="flex flex-wrap gap-2">
                    <button
                      onClick={() => setSelectedBotFilter('all')}
                      className={`px-4 py-2 rounded-xl text-sm font-medium transition-all duration-300 ${selectedBotFilter === 'all'
                          ? 'bg-gradient-accent text-white shadow-lg'
                          : 'bg-slate-800/50 text-slate-300 hover:bg-slate-700/50 border border-slate-700/50'
                        }`}
                    >
                      All Bots
                    </button>
                    {freqtradeData.bots.map((bot) => (
                      <button
                        key={bot.name}
                        onClick={() => setSelectedBotFilter(bot.name.toLowerCase().replace(/\s+/g, '_'))}
                        className={`px-4 py-2 rounded-xl text-sm font-medium transition-all duration-300 ${selectedBotFilter === bot.name.toLowerCase().replace(/\s+/g, '_')
                            ? 'bg-blue-600 text-white shadow-lg'
                            : 'bg-slate-800/50 text-slate-300 hover:bg-slate-700/50 border border-slate-700/50'
                          }`}
                      >
                        <div className="flex items-center gap-2">
                          <div className={`w-2 h-2 rounded-full ${bot.status === 'online' ? 'bg-emerald-500 animate-pulse' : 'bg-red-500'
                            }`} />
                          {bot.name}
                        </div>
                      </button>
                    ))}
                  </div>
                </div>
              </div>
            )}

            {/* Live Price Ticker */}
            <div className="mb-8">
              <LivePriceTicker />
            </div>

            {/* Best Performing Bot Highlight */}
            {bestPerformingBot && (
              <div className="mb-6 animate-slide-up">
                <div className="bg-gradient-to-r from-emerald-900/40 to-blue-900/40 backdrop-blur-sm rounded-xl p-4 border border-emerald-600/50 glow-success">
                  <div className="flex items-center gap-3">
                    <Bot size={20} className="text-emerald-400" />
                    <div>
                      <p className="text-sm text-slate-300">🏆 Best Performing Bot Today</p>
                      <p className="font-semibold text-emerald-300">
                        {bestPerformingBot.name} • +${bestPerformingBot.profit.toFixed(2)} • {bestPerformingBot.win_rate.toFixed(1)}% Win Rate
                      </p>
                    </div>
                  </div>
                </div>
              </div>
            )}

            {/* Portfolio Card */}
            <div className="mb-8">
              <PortfolioCard portfolio={portfolio} isLoading={isLoading || freqtradeLoading} />
            </div>

            {/* Charts Section */}
            <div className="grid grid-cols-1 lg:grid-cols-2 gap-6 mb-8">
              <PerformanceChart />
              <BotPerformanceChart bots={freqtradeData.bots} />
            </div>

            {/* Bot Control Panel */}
            <div className="mb-8">
              <BotControlPanel
                bots={freqtradeData.bots}
                onAction={async (botName, action) => {
                  console.log(`${action} bot: ${botName}`);
                  // TODO: Implement actual API call for bot actions
                  await new Promise(resolve => setTimeout(resolve, 1000));
                  // Refresh data after action
                  await refetchFreqtradeData();
                }}
              />
            </div>

            {/* Positions Section */}
            <div className="animate-slide-up">
              <h2 className="text-2xl font-semibold text-slate-100 mb-4">Open Positions</h2>
              <PositionsTable
                positions={positions}
                isLoading={isLoading || freqtradeLoading}
                onClose={handleClosePosition}
              />
            </div>
          </div>
        </main>
      </div>
    </div>
  );
}
