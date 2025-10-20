import { useState, useEffect } from 'react';
import { Brain, TrendingUp, Activity, AlertCircle, Play, Pause } from 'lucide-react';
import { motion, AnimatePresence } from 'motion/react';
import { Badge } from './ui/badge';
import { Button } from './ui/button';
import { Card } from './ui/card';
import { memService, type MEMStatusResponse, type MEMActivity } from '../services/memService';

interface MEMCornerProps {
  onOpenChat: () => void;
  isExpanded?: boolean;
  onToggleExpand?: () => void;
}

export function MEMCorner({ onOpenChat, isExpanded = false, onToggleExpand }: MEMCornerProps) {
  const [status, setStatus] = useState<MEMStatusResponse | null>(null);
  const [recentActivity, setRecentActivity] = useState<MEMActivity[]>([]);
  const [isLoading, setIsLoading] = useState(true);

  // Poll MEM's status
  useEffect(() => {
    const fetchStatus = async () => {
      try {
        const response = await memService.getStatus();
        if (response.success && response.data) {
          setStatus(response.data);
          setIsLoading(false);
        }
      } catch (error) {
        console.error('Failed to fetch MEM status:', error);
        setStatus(null);
        setIsLoading(false);
      }
    };

    fetchStatus();
    const interval = setInterval(fetchStatus, 5000); // Poll every 5 seconds
    return () => clearInterval(interval);
  }, []);

  // Fetch recent activity
  useEffect(() => {
    const fetchActivity = async () => {
      try {
        const response = await memService.getActivityFeed(3);
        if (response.success && response.data) {
          setRecentActivity(response.data);
        }
      } catch (error) {
        console.error('Failed to fetch MEM activity:', error);
        setRecentActivity([]);
      }
    };

    fetchActivity();
    const interval = setInterval(fetchActivity, 10000);
    return () => clearInterval(interval);
  }, []);

  if (isLoading) {
    return (
      <div className="fixed bottom-6 right-6 z-50">
        <Card className="p-4 bg-slate-900 backdrop-blur border-slate-700 w-80">
          <div className="flex items-center gap-3">
            <div className="w-12 h-12 rounded-lg bg-blue-500/10 animate-pulse" />
            <div className="flex-1 space-y-2">
              <div className="h-4 bg-slate-800 rounded w-24" />
              <div className="h-3 bg-slate-800 rounded w-32" />
            </div>
          </div>
        </Card>
      </div>
    );
  }

  return (
    <div className="fixed bottom-6 right-6 z-50">
      <AnimatePresence>
        {isExpanded ? (
          <motion.div
            initial={{ opacity: 0, scale: 0.95, y: 10 }}
            animate={{ opacity: 1, scale: 1, y: 0 }}
            exit={{ opacity: 0, scale: 0.95, y: 10 }}
            transition={{ duration: 0.15, ease: [0.4, 0, 0.2, 1] }}
          >
            <Card className="p-6 bg-slate-900 backdrop-blur border-slate-700 w-96 shadow-2xl">
              {/* Header */}
              <div className="flex items-start justify-between mb-4">
                <div className="flex items-center gap-3">
                  <div className="relative">
                    <div className="w-10 h-10 rounded-lg bg-blue-600 flex items-center justify-center">
                      <Brain className="w-5 h-5 text-white" />
                    </div>
                    {status?.isTrading && (
                      <div className="absolute -top-0.5 -right-0.5 w-2.5 h-2.5 bg-green-500 rounded-full border border-slate-900" />
                    )}
                  </div>
                  <div>
                    <div className="text-xs font-medium text-gray-400 uppercase tracking-wider">
                      MEM Status
                    </div>
                    <div className="text-sm font-numeric text-gray-300 mt-0.5">
                      {status?.status || 'Active'}
                    </div>
                  </div>
                </div>
                <Button
                  variant="ghost"
                  size="sm"
                  onClick={onToggleExpand}
                  className="h-8 w-8 p-0 text-gray-400 hover:text-gray-300 hover:bg-slate-800"
                >
                  <span className="text-lg">×</span>
                </Button>
              </div>

              {/* Stats Grid */}
              <div className="grid grid-cols-2 gap-3 mb-4">
                <div className="p-3 bg-slate-800/50 rounded-lg border border-slate-700">
                  <div className="text-xs text-gray-400 mb-1">Active Strategies</div>
                  <div className="text-lg font-numeric font-semibold text-gray-100">
                    {status?.activeStrategies || 0}
                  </div>
                </div>
                <div className="p-3 bg-slate-800/50 rounded-lg border border-slate-700">
                  <div className="text-xs text-gray-400 mb-1">Open Positions</div>
                  <div className="text-lg font-numeric font-semibold text-gray-100">
                    {status?.openPositions || 0}
                  </div>
                </div>
                <div className="p-3 bg-slate-800/50 rounded-lg border border-slate-700">
                  <div className="text-xs text-gray-400 mb-1">Today's P&L</div>
                  <div className={`text-lg font-numeric font-semibold ${
                    (status?.todayPnL || 0) >= 0 ? 'text-green-400' : 'text-red-400'
                  }`}>
                    ${Math.abs(status?.todayPnL || 0).toFixed(2)}
                  </div>
                </div>
                <div className="p-3 bg-slate-800/50 rounded-lg border border-slate-700">
                  <div className="text-xs text-gray-400 mb-1">Health</div>
                  <div className="text-lg font-numeric font-semibold text-gray-100">
                    {status?.healthScore || 0}%
                  </div>
                </div>
              </div>

              {/* Recent Activity */}
              {recentActivity.length > 0 && (
                <div className="mb-4">
                  <div className="text-xs font-medium text-gray-400 uppercase tracking-wider mb-2">
                    Recent Activity
                  </div>
                  <div className="space-y-2">
                    {recentActivity.map((activity) => (
                      <div
                        key={activity.id}
                        className="p-2.5 bg-slate-800/30 rounded-lg border border-slate-700/50 
                                   hover:border-slate-600 transition-colors cursor-pointer"
                      >
                        <div className="flex items-start gap-2">
                          {activity.type === 'Trade' && <TrendingUp className="w-3.5 h-3.5 text-green-400 mt-0.5 flex-shrink-0" />}
                          {activity.type === 'Analysis' && <Activity className="w-3.5 h-3.5 text-blue-400 mt-0.5 flex-shrink-0" />}
                          {activity.type === 'Alert' && <AlertCircle className="w-3.5 h-3.5 text-amber-400 mt-0.5 flex-shrink-0" />}
                          <div className="flex-1 min-w-0">
                            <div className="flex items-start justify-between gap-2">
                              <p className="text-xs text-gray-200 font-medium">{activity.title}</p>
                              <span className="text-xs text-gray-500 whitespace-nowrap">
                                {new Date(activity.timestamp).toLocaleTimeString([], { 
                                  hour: '2-digit', 
                                  minute: '2-digit' 
                                })}
                              </span>
                            </div>
                            <p className="text-xs text-gray-400 mt-0.5">{activity.description}</p>
                            {activity.symbol && (
                              <Badge 
                                variant="outline" 
                                className="mt-1.5 text-xs px-1.5 py-0 h-5 border-blue-500/30 text-blue-400 font-numeric"
                              >
                                {activity.symbol}
                              </Badge>
                            )}
                          </div>
                        </div>
                      </div>
                    ))}
                  </div>
                </div>
              )}

              {/* Actions */}
              <div className="flex gap-2">
                <Button
                  onClick={onOpenChat}
                  className="flex-1 bg-blue-600 hover:bg-blue-700 text-white text-sm h-9"
                >
                  Open MEM Chat
                </Button>
                {status?.isTrading ? (
                  <Button
                    variant="outline"
                    size="icon"
                    className="h-9 w-9 border-amber-500/30 text-amber-400 hover:bg-amber-500/10 hover:border-amber-500/50"
                    title="Pause trading"
                  >
                    <Pause className="w-4 h-4" />
                  </Button>
                ) : (
                  <Button
                    variant="outline"
                    size="icon"
                    className="h-9 w-9 border-green-500/30 text-green-400 hover:bg-green-500/10 hover:border-green-500/50"
                    title="Resume trading"
                  >
                    <Play className="w-4 h-4" />
                  </Button>
                )}
              </div>
            </Card>
          </motion.div>
        ) : (
          <motion.button
            initial={{ opacity: 0, scale: 0.9 }}
            animate={{ opacity: 1, scale: 1 }}
            exit={{ opacity: 0, scale: 0.9 }}
            transition={{ duration: 0.15 }}
            whileHover={{ scale: 1.02 }}
            onClick={onToggleExpand}
            className="group relative"
          >
            <div className="w-14 h-14 rounded-xl bg-slate-900 border border-slate-700 
                            hover:border-blue-500/50 transition-colors
                            flex items-center justify-center shadow-lg">
              <Brain className="w-6 h-6 text-blue-400" />
              
              {/* Status Indicator */}
              {status?.isTrading && (
                <motion.div
                  className="absolute -top-0.5 -right-0.5 w-2.5 h-2.5 bg-green-500 rounded-full border border-slate-900"
                  animate={{ opacity: [1, 0.5, 1] }}
                  transition={{ duration: 2, repeat: Infinity, ease: "easeInOut" }}
                />
              )}
              
              {/* Activity Badge */}
              {recentActivity.length > 0 && (
                <div className="absolute -bottom-0.5 -right-0.5 w-5 h-5 bg-blue-600 
                                rounded-full flex items-center justify-center 
                                text-xs text-white font-numeric font-medium border border-slate-900">
                  {recentActivity.length}
                </div>
              )}
            </div>
          </motion.button>
        )}
      </AnimatePresence>
    </div>
  );
}
