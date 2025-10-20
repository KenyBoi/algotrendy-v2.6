import { useState, useRef, useEffect } from 'react';
import { BarChart3, Database, TrendingUp, FileText, Settings, Search, Brain, Send, ChevronUp, ChevronDown, Activity, AlertCircle, Play, Pause } from 'lucide-react';
import { Button } from './ui/button';
import { Input } from './ui/input';
import { Badge } from './ui/badge';
import { ScrollArea } from './ui/scroll-area';
import { memService, type MEMStatusResponse, type MEMActivity } from '../services/memService';

interface Message {
  id: string;
  role: 'user' | 'assistant';
  content: string;
  timestamp: Date;
}

interface SidebarProps {
  activeView: string;
  onViewChange: (view: string) => void;
}

export function Sidebar({ activeView, onViewChange }: SidebarProps) {
  const [isChatExpanded, setIsChatExpanded] = useState(false);
  const [hoveredItem, setHoveredItem] = useState<string | null>(null);
  const [messages, setMessages] = useState<Message[]>([
    {
      id: '1',
      role: 'assistant',
      content: 'Hey! 👋 I\'m MEM, your AI buddy for exploring markets and building strategies. What are you thinking about today?',
      timestamp: new Date()
    }
  ]);
  const [input, setInput] = useState('');
  const [isLoading, setIsLoading] = useState(false);
  const [status, setStatus] = useState<MEMStatusResponse | null>(null);
  const [recentActivity, setRecentActivity] = useState<MEMActivity[]>([]);
  const scrollRef = useRef<HTMLDivElement>(null);

  const menuItems = [
    { 
      id: 'dashboard', 
      label: 'Dashboard', 
      icon: Activity,
      description: 'Overview of portfolio and MEM activity',
      items: ['Portfolio', 'Live Positions', 'Performance', 'MEM Activity']
    },
    { 
      id: 'datasets', 
      label: 'Datasets', 
      icon: Database,
      description: 'Browse and analyze financial datasets',
      items: ['Companies', 'Market Data', 'Fundamentals', 'Technical Indicators', 'News Feed', 'Market Sentiments', 'Geographical Sentiment']
    },
    { 
      id: 'strategy', 
      label: 'Strategy Builder', 
      icon: TrendingUp,
      description: 'Design and test trading strategies',
      items: ['New Strategy', 'Templates', 'Backtesting', 'Live Testing']
    },
    { 
      id: 'query', 
      label: 'Query Interface', 
      icon: Search,
      description: 'Query time-series data with QuestDB',
      items: ['SQL Editor', 'Query History', 'Saved Queries', 'Export Data']
    },
    { 
      id: 'analytics', 
      label: 'Metrics & Reports', 
      icon: BarChart3,
      description: 'Advanced analytics and reporting',
      items: ['Performance', 'Risk Analysis', 'P&L Reports', 'Trade History']
    }
  ];

  // Poll MEM's status
  useEffect(() => {
    const fetchStatus = async () => {
      try {
        const response = await memService.getStatus();
        if (response.success && response.data) {
          setStatus(response.data);
        }
      } catch (error) {
        console.error('Failed to fetch MEM status:', error);
        setStatus(null);
      }
    };

    fetchStatus();
    const interval = setInterval(fetchStatus, 5000);
    return () => clearInterval(interval);
  }, []);

  // Fetch recent activity
  useEffect(() => {
    const fetchActivity = async () => {
      try {
        const response = await memService.getActivityFeed(2);
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

  useEffect(() => {
    if (scrollRef.current) {
      scrollRef.current.scrollTop = scrollRef.current.scrollHeight;
    }
  }, [messages]);

  const handleSend = async () => {
    if (!input.trim()) return;

    const userMessage: Message = {
      id: Date.now().toString(),
      role: 'user',
      content: input,
      timestamp: new Date()
    };

    setMessages((prev) => [...prev, userMessage]);
    setInput('');
    setIsLoading(true);

    // Simulate AI response
    setTimeout(() => {
      const aiMessage: Message = {
        id: (Date.now() + 1).toString(),
        role: 'assistant',
        content: 'That\'s an interesting question! I\'m analyzing the data now. Based on the datasets we have, I can help you explore that further.',
        timestamp: new Date()
      };
      setMessages((prev) => [...prev, aiMessage]);
      setIsLoading(false);
    }, 1000);
  };

  return (
    <div className="w-64 border-r border-slate-800 bg-slate-900 flex flex-col h-screen">
      {/* Top Section */}
      <div className="p-4">
        <div className="mb-6">
          <h2 className="mb-1 text-gray-100 font-semibold">AlgoTrendy</h2>
          <p className="text-xs text-gray-400">Powered by MEM</p>
        </div>

        <Input 
          placeholder="Search..." 
          className="mb-4 bg-slate-800 border-slate-700 text-gray-100 placeholder:text-gray-500" 
        />
      </div>

      {/* Navigation */}
      <nav className="px-4 space-y-1 flex-shrink-0 relative">
        {menuItems.map((item) => {
          const Icon = item.icon;
          const isHovered = hoveredItem === item.id;
          return (
            <div key={item.id} className="relative">
              <button
                onClick={() => onViewChange(item.id)}
                onMouseEnter={() => setHoveredItem(item.id)}
                onMouseLeave={() => setHoveredItem(null)}
                className={`w-full flex items-center gap-3 px-3 py-2 rounded-lg transition-colors text-sm ${
                  activeView === item.id
                    ? 'bg-blue-600 text-white'
                    : 'text-gray-300 hover:bg-slate-800 hover:text-gray-100'
                }`}
              >
                <Icon className="h-4 w-4" />
                <span>{item.label}</span>
              </button>
              
              {/* Hover Expansion Panel */}
              {isHovered && (
                <div 
                  className="absolute left-full top-0 ml-2 w-64 bg-slate-800 border border-slate-700 rounded-lg shadow-2xl p-4 z-50"
                  onMouseEnter={() => setHoveredItem(item.id)}
                  onMouseLeave={() => setHoveredItem(null)}
                >
                  <div className="flex items-start gap-3 mb-3">
                    <div className="w-8 h-8 rounded-lg bg-blue-600/10 flex items-center justify-center flex-shrink-0">
                      <Icon className="h-4 w-4 text-blue-400" />
                    </div>
                    <div>
                      <h4 className="text-sm text-gray-100 mb-1">{item.label}</h4>
                      <p className="text-xs text-gray-400">{item.description}</p>
                    </div>
                  </div>
                  
                  <div className="space-y-1">
                    {item.items.map((subItem, idx) => (
                      <button
                        key={idx}
                        onClick={() => onViewChange(item.id)}
                        className="w-full text-left px-3 py-2 rounded text-xs text-gray-300 hover:bg-slate-700 hover:text-gray-100 transition-colors"
                      >
                        {subItem}
                      </button>
                    ))}
                  </div>
                </div>
              )}
            </div>
          );
        })}
      </nav>

      {/* Spacer */}
      <div className="flex-1 min-h-0" />

      {/* MEM Chat Portal */}
      <div className="border-t border-slate-800 bg-slate-950/50 flex flex-col" style={{ height: isChatExpanded ? '500px' : 'auto' }}>
        {/* MEM Header - Always Visible */}
        <button
          onClick={() => setIsChatExpanded(!isChatExpanded)}
          className="p-4 flex items-center justify-between hover:bg-slate-800/50 transition-colors"
        >
          <div className="flex items-center gap-3">
            <div className={`w-3 h-3 rounded-full ${status?.isTrading ? 'bg-green-500' : 'bg-red-500'}`} />
            <div className="flex items-baseline gap-2">
              <span className="text-sm text-gray-100">MEM</span>
              <span className="text-xs text-gray-400">• {status?.status || 'Active'}</span>
            </div>
          </div>
          {isChatExpanded ? (
            <ChevronDown className="w-4 h-4 text-gray-400" />
          ) : (
            <ChevronUp className="w-4 h-4 text-gray-400" />
          )}
        </button>

        {/* MEM Expanded Content */}
        {isChatExpanded && (
          <div className="flex flex-col flex-1 min-h-0 border-t border-slate-800">
            {/* Quick Stats */}
            <div className="px-3 py-2 grid grid-cols-2 gap-2 border-b border-slate-800 bg-slate-900/50">
              <div className="flex items-center justify-center gap-1.5 py-1 px-2 bg-slate-800/50 rounded">
                <span className="text-[10px] text-gray-400">Strategies</span>
                <span className="text-xs font-numeric text-gray-100">{status?.activeStrategies || 0}</span>
              </div>
              <div className="flex items-center justify-center gap-1.5 py-1 px-2 bg-slate-800/50 rounded">
                <span className="text-[10px] text-gray-400">Positions</span>
                <span className="text-xs font-numeric text-gray-100">{status?.openPositions || 0}</span>
              </div>
            </div>

            {/* Chat Messages */}
            <ScrollArea className="flex-1 px-4 py-3">
              <div ref={scrollRef} className="space-y-3">
                {messages.map((message) => (
                  <div
                    key={message.id}
                    className={`flex ${message.role === 'user' ? 'justify-end' : 'justify-start'}`}
                  >
                    <div
                      className={`max-w-[85%] rounded-lg px-3 py-2 text-xs ${
                        message.role === 'user'
                          ? 'bg-blue-600 text-white'
                          : 'bg-slate-800 text-gray-100 border border-slate-700'
                      }`}
                    >
                      {message.content}
                    </div>
                  </div>
                ))}
                {isLoading && (
                  <div className="flex justify-start">
                    <div className="bg-slate-800 border border-slate-700 rounded-lg px-3 py-2">
                      <div className="flex gap-1">
                        <div className="w-1.5 h-1.5 bg-gray-400 rounded-full animate-pulse" />
                        <div className="w-1.5 h-1.5 bg-gray-400 rounded-full animate-pulse delay-100" />
                        <div className="w-1.5 h-1.5 bg-gray-400 rounded-full animate-pulse delay-200" />
                      </div>
                    </div>
                  </div>
                )}
              </div>
            </ScrollArea>

            {/* Recent Activity */}
            {recentActivity.length > 0 && (
              <div className="px-4 py-2 border-t border-slate-800 bg-slate-900/30">
                <div className="text-xs text-gray-400 mb-2">Recent</div>
                <div className="space-y-1">
                  {recentActivity.slice(0, 1).map((activity) => (
                    <div
                      key={activity.id}
                      className="p-2 bg-slate-800/30 rounded border border-slate-700/50 hover:border-slate-600 transition-colors"
                    >
                      <div className="flex items-start gap-2">
                        {activity.type === 'Trade' && <TrendingUp className="w-3 h-3 text-green-400 mt-0.5" />}
                        {activity.type === 'Analysis' && <Activity className="w-3 h-3 text-blue-400 mt-0.5" />}
                        {activity.type === 'Alert' && <AlertCircle className="w-3 h-3 text-amber-400 mt-0.5" />}
                        <div className="flex-1 min-w-0">
                          <p className="text-xs text-gray-200">{activity.title}</p>
                          <p className="text-xs text-gray-400 truncate">{activity.description}</p>
                        </div>
                      </div>
                    </div>
                  ))}
                </div>
              </div>
            )}

            {/* Input */}
            <div className="p-3 border-t border-slate-800 bg-slate-900">
              <div className="flex gap-2">
                <Input
                  value={input}
                  onChange={(e) => setInput(e.target.value)}
                  onKeyPress={(e) => e.key === 'Enter' && handleSend()}
                  placeholder="Ask MEM anything..."
                  className="flex-1 bg-slate-800 border-slate-700 text-gray-100 text-xs h-8"
                />
                <Button
                  onClick={handleSend}
                  disabled={!input.trim() || isLoading}
                  size="sm"
                  className="h-8 w-8 p-0 bg-blue-600 hover:bg-blue-700 text-white"
                >
                  <Send className="w-3 h-3" />
                </Button>
              </div>
            </div>
          </div>
        )}
      </div>

      {/* Settings */}
      <div className="p-4 border-t border-slate-800">
        <Button variant="ghost" className="w-full justify-start text-gray-300 hover:text-gray-100 hover:bg-slate-800">
          <Settings className="mr-3 h-4 w-4" />
          <span className="text-sm">Settings</span>
        </Button>
      </div>
    </div>
  );
}
