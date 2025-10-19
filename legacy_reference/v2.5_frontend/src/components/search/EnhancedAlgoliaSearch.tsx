import React, { useState, useEffect, useCallback } from 'react';
import {
    InstantSearch,
    SearchBox,
    Hits,
    RefinementList,
    Pagination,
    Stats,
    Configure,
    ClearRefinements,
    RangeInput,
    SortBy,
    useSearchBox,
    useStats,
} from 'react-instantsearch-hooks-web';
import algoliasearch from 'algoliasearch';
import { useAlgoliaAnalytics } from '@/hooks/useAlgoliaAnalytics';
import { useDemoSearchClient } from '@/lib/demoSearchClient';
import { useFreqtradeData } from '@/hooks/useFreqtrade';
import { Bot, Activity, TrendingUp } from 'lucide-react';

// Algolia client setup with optimizations
const searchClient = algoliasearch(
    process.env.NEXT_PUBLIC_ALGOLIA_APP_ID || 'demo-app-id',
    process.env.NEXT_PUBLIC_ALGOLIA_SEARCH_KEY || 'demo-search-key'
);

// Add search caching for cost optimization
const searchCache = new Map();
const cacheTimeout = 5 * 60 * 1000; // 5 minutes

const cachedSearchClient = {
    ...searchClient,
    search(requests: any[]) {
        const cacheKey = JSON.stringify(requests);
        const cached = searchCache.get(cacheKey);

        if (cached && Date.now() - cached.timestamp < cacheTimeout) {
            return Promise.resolve(cached.results);
        }

        return searchClient.search(requests).then((results) => {
            searchCache.set(cacheKey, {
                results,
                timestamp: Date.now(),
            });

            // Clean old cache entries
            if (searchCache.size > 100) {
                const oldestKey = searchCache.keys().next().value;
                searchCache.delete(oldestKey);
            }

            return results;
        });
    },
};

const indexName = process.env.NEXT_PUBLIC_ALGOLIA_INDEX_NAME || 'algotrendy_trades';

// AI-Powered Query Suggestions (Enhanced with Freqtrade)
const AI_QUERY_SUGGESTIONS = [
    'profitable BTC trades last week',
    'RSI strategy performance',
    'high volume ETH trades',
    'momentum strategies with >70% win rate',
    'trades with stop loss triggered',
    'MACD crossover signals',
    'volatility breakout opportunities',
    'range-bound market strategies',
    // Freqtrade-specific suggestions
    'Conservative RSI bot trades',
    'MACD Hunter profitable signals',
    'Aggressive RSI high volume',
    'freqtrade bot performance comparison',
    'automated trading profits',
    'bot vs manual trading results',
    'freqtrade stop loss analysis',
    'multi-bot portfolio performance',
];

// Enhanced Trade Hit Component with Rich Data (Freqtrade Enhanced)
function EnhancedTradeHit({ hit }: { hit: any }) {
    const pnlColor = hit.pnl >= 0 ? 'text-green-400' : 'text-red-400';
    const pnlIcon = hit.pnl >= 0 ? '📈' : '📉';
    const confidenceColor = hit.confidence >= 0.8 ? 'text-green-400' :
        hit.confidence >= 0.6 ? 'text-yellow-400' : 'text-red-400';

    const riskLevel = hit.risk_score <= 0.3 ? 'LOW' :
        hit.risk_score <= 0.7 ? 'MED' : 'HIGH';
    const riskColor = hit.risk_score <= 0.3 ? 'text-green-400' :
        hit.risk_score <= 0.7 ? 'text-yellow-400' : 'text-red-400';

    // Freqtrade-specific data detection
    const isFreqtradeTrade = hit.bot_name || hit.freqtrade_trade_id || hit.source === 'freqtrade';
    const botName = hit.bot_name;

    return (
        <div className="bg-gray-800 border border-gray-700 rounded-lg p-4 hover:border-cyan-500 transition-all duration-300 transform hover:scale-105">
            {/* Header with Symbol, Source, and P&L */}
            <div className="flex justify-between items-start mb-3">
                <div className="flex items-center gap-3">
                    <div>
                        <h3 className="text-lg font-semibold text-white flex items-center gap-2">
                            {hit.symbol}
                            {/* Source Badge */}
                            {isFreqtradeTrade ? (
                                <span className="text-xs bg-blue-600 px-2 py-1 rounded flex items-center gap-1">
                                    <Bot size={10} />
                                    {botName || 'Bot'}
                                </span>
                            ) : (
                                <span className="text-xs bg-gray-600 px-2 py-1 rounded">Manual</span>
                            )}
                            {hit.ai_generated && <span className="text-xs bg-purple-600 px-2 py-1 rounded">🤖 AI</span>}
                            {hit.is_live && <span className="text-xs bg-green-600 px-2 py-1 rounded animate-pulse">🔴 LIVE</span>}
                        </h3>
                        <div className="flex items-center gap-2">
                            <p className="text-gray-400 text-sm">{hit.strategy}</p>
                            {hit.freqtrade_trade_id && (
                                <span className="text-xs text-blue-400">
                                    ID: {hit.freqtrade_trade_id}
                                </span>
                            )}
                        </div>
                    </div>
                </div>
                <div className="text-right">
                    <div className={`font-bold text-lg ${pnlColor}`}>
                        {pnlIcon} ${hit.pnl >= 0 ? '+' : ''}{hit.pnl?.toFixed(2)}
                    </div>
                    <div className={`text-sm ${pnlColor}`}>
                        {hit.pnl_percent >= 0 ? '+' : ''}{hit.pnl_percent?.toFixed(2)}%
                    </div>
                    {hit.duration_minutes && (
                        <div className="text-xs text-gray-400 mt-1">
                            ⏱️ {hit.duration_minutes < 60
                                ? `${hit.duration_minutes}m`
                                : `${Math.floor(hit.duration_minutes / 60)}h ${hit.duration_minutes % 60}m`
                            }
                        </div>
                    )}
                </div>
            </div>

            {/* AI Confidence & Risk Metrics */}
            {(hit.confidence || hit.risk_score) && (
                <div className="flex gap-4 mb-3 p-2 bg-gray-900 rounded">
                    {hit.confidence && (
                        <div className="flex items-center gap-2">
                            <span className="text-gray-400 text-xs">Confidence:</span>
                            <span className={`font-semibold ${confidenceColor}`}>
                                {(hit.confidence * 100).toFixed(0)}%
                            </span>
                        </div>
                    )}
                    {hit.risk_score && (
                        <div className="flex items-center gap-2">
                            <span className="text-gray-400 text-xs">Risk:</span>
                            <span className={`font-semibold text-xs ${riskColor}`}>
                                {riskLevel}
                            </span>
                        </div>
                    )}
                    {hit.volatility && (
                        <div className="flex items-center gap-2">
                            <span className="text-gray-400 text-xs">Vol:</span>
                            <span className="text-white text-xs">{hit.volatility.toFixed(2)}</span>
                        </div>
                    )}
                </div>
            )}

            {/* Trade Details Grid */}
            <div className="grid grid-cols-2 lg:grid-cols-4 gap-3 text-sm mb-3">
                <div>
                    <span className="text-gray-400">Entry:</span>
                    <span className="text-white ml-2 font-mono">${hit.entry_price?.toFixed(4)}</span>
                </div>
                <div>
                    <span className="text-gray-400">Current:</span>
                    <span className="text-white ml-2 font-mono">${hit.current_price?.toFixed(4)}</span>
                </div>
                <div>
                    <span className="text-gray-400">Size:</span>
                    <span className="text-white ml-2">{hit.quantity}</span>
                </div>
                <div>
                    <span className="text-gray-400">Side:</span>
                    <span className={`ml-2 font-semibold ${hit.side === 'long' ? 'text-green-400' : 'text-red-400'}`}>
                        {hit.side === 'long' ? '📈 LONG' : '📉 SHORT'}
                    </span>
                </div>
            </div>

            {/* Advanced Metrics (Enhanced for Freqtrade) */}
            {(hit.stop_loss || hit.take_profit || hit.max_drawdown || hit.entry_reason || hit.exit_reason) && (
                <div className="grid grid-cols-2 lg:grid-cols-3 gap-3 text-xs mb-3 p-2 bg-gray-900 rounded">
                    {hit.stop_loss && (
                        <div>
                            <span className="text-gray-400">Stop Loss:</span>
                            <span className="text-red-400 ml-1">${hit.stop_loss.toFixed(2)}</span>
                        </div>
                    )}
                    {hit.take_profit && (
                        <div>
                            <span className="text-gray-400">Take Profit:</span>
                            <span className="text-green-400 ml-1">${hit.take_profit.toFixed(2)}</span>
                        </div>
                    )}
                    {hit.max_drawdown && (
                        <div>
                            <span className="text-gray-400">Max DD:</span>
                            <span className="text-orange-400 ml-1">{hit.max_drawdown.toFixed(2)}%</span>
                        </div>
                    )}
                    {hit.entry_reason && (
                        <div className="col-span-2 lg:col-span-1">
                            <span className="text-gray-400">Entry:</span>
                            <span className="text-blue-400 ml-1 text-xs">{hit.entry_reason}</span>
                        </div>
                    )}
                    {hit.exit_reason && (
                        <div className="col-span-2 lg:col-span-1">
                            <span className="text-gray-400">Exit:</span>
                            <span className="text-orange-400 ml-1 text-xs">{hit.exit_reason}</span>
                        </div>
                    )}
                </div>
            )}

            {/* AI Insights */}
            {hit.ai_insight && (
                <div className="bg-purple-900/20 border border-purple-600/30 rounded p-2 mb-3">
                    <div className="flex items-center gap-2 mb-1">
                        <span className="text-purple-400 text-xs">🧠 AI Insight:</span>
                    </div>
                    <p className="text-purple-200 text-xs">{hit.ai_insight}</p>
                </div>
            )}

            {/* Market Context */}
            {hit.market_conditions && (
                <div className="flex gap-2 mb-3 text-xs">
                    {hit.market_conditions.map((condition: string, idx: number) => (
                        <span key={idx} className="bg-blue-600/20 text-blue-300 px-2 py-1 rounded">
                            {condition}
                        </span>
                    ))}
                </div>
            )}

            {/* Footer with Timestamps and Actions */}
            <div className="flex justify-between items-center pt-3 border-t border-gray-700">
                <div className="text-xs text-gray-500">
                    {hit.created_at && (
                        <span>Created: {new Date(hit.created_at).toLocaleString()}</span>
                    )}
                    {hit.updated_at && hit.updated_at !== hit.created_at && (
                        <span className="ml-3">Updated: {new Date(hit.updated_at).toLocaleString()}</span>
                    )}
                </div>
                <div className="flex gap-2">
                    <button className="text-xs bg-cyan-600 hover:bg-cyan-700 px-2 py-1 rounded transition-colors">
                        📊 Details
                    </button>
                    {hit.is_live && (
                        <button className="text-xs bg-red-600 hover:bg-red-700 px-2 py-1 rounded transition-colors">
                            ❌ Close
                        </button>
                    )}
                </div>
            </div>
        </div>
    );
}

// Smart Search Suggestions Component
function SmartSearchSuggestions({ onSuggestionClick }: { onSuggestionClick: (query: string) => void }) {
    const [suggestions, setSuggestions] = useState<string[]>([]);

    useEffect(() => {
        // Rotate suggestions every 10 seconds
        const interval = setInterval(() => {
            const shuffled = [...AI_QUERY_SUGGESTIONS].sort(() => Math.random() - 0.5);
            setSuggestions(shuffled.slice(0, 4));
        }, 10000);

        // Initial load
        const shuffled = [...AI_QUERY_SUGGESTIONS].sort(() => Math.random() - 0.5);
        setSuggestions(shuffled.slice(0, 4));

        return () => clearInterval(interval);
    }, []);

    return (
        <div className="bg-gray-800/50 border border-gray-700 rounded-lg p-4 mb-6">
            <h3 className="text-sm font-semibold text-gray-300 mb-3 flex items-center gap-2">
                💡 Smart Suggestions
                <span className="text-xs bg-purple-600 px-2 py-1 rounded">AI-Powered</span>
            </h3>
            <div className="flex flex-wrap gap-2">
                {suggestions.map((suggestion, idx) => (
                    <button
                        key={idx}
                        onClick={() => onSuggestionClick(suggestion)}
                        className="text-xs bg-gray-700 hover:bg-cyan-600 text-gray-300 hover:text-white px-3 py-2 rounded-full transition-all duration-300 transform hover:scale-105"
                    >
                        {suggestion}
                    </button>
                ))}
            </div>
        </div>
    );
}

// Search Analytics Dashboard Component
function SearchAnalyticsDashboard() {
    const { analytics, resetAnalytics } = useAlgoliaAnalytics(); return (
        <div className="bg-gray-800 border border-gray-700 rounded-lg p-4 mb-4">
            <div className="flex justify-between items-center mb-3">
                <h3 className="text-sm font-semibold text-gray-300 flex items-center gap-2">
                    📊 Search Analytics
                </h3>
                <button
                    onClick={resetAnalytics}
                    className="text-xs bg-red-600 hover:bg-red-700 px-2 py-1 rounded"
                >
                    Reset
                </button>
            </div>

            <div className="grid grid-cols-2 gap-4 text-xs">
                <div>
                    <span className="text-gray-400">Total Queries:</span>
                    <span className="text-white ml-2 font-bold">{analytics.totalQueries}</span>
                </div>
                <div>
                    <span className="text-gray-400">Avg Response:</span>
                    <span className="text-white ml-2 font-bold">{analytics.avgResponseTime.toFixed(0)}ms</span>
                </div>
                <div>
                    <span className="text-gray-400">This Month:</span>
                    <span className="text-white ml-2 font-bold">{analytics.operationsThisMonth}</span>
                </div>
                <div>
                    <span className="text-gray-400">Cost Est:</span>
                    <span className={`ml-2 font-bold ${analytics.costEstimate > 0 ? 'text-red-400' : 'text-green-400'}`}>
                        ${analytics.costEstimate.toFixed(2)}
                    </span>
                </div>
            </div>

            {analytics.remainingFreeOperations > 0 && (
                <div className="mt-2 p-2 bg-green-900/20 border border-green-600/30 rounded">
                    <div className="text-xs text-green-300">
                        🆓 {analytics.remainingFreeOperations} free operations remaining
                    </div>
                    <div className="w-full bg-gray-700 rounded-full h-1 mt-1">
                        <div
                            className="bg-green-400 h-1 rounded-full transition-all duration-300"
                            style={{
                                width: `${(analytics.remainingFreeOperations / 10000) * 100}%`
                            }}
                        />
                    </div>
                </div>
            )}

            {analytics.topQueries.length > 0 && (
                <div className="mt-3">
                    <div className="text-xs text-gray-400 mb-1">Top Queries:</div>
                    <div className="space-y-1">
                        {analytics.topQueries.slice(0, 3).map((query, idx) => (
                            <div key={idx} className="flex justify-between text-xs">
                                <span className="text-gray-300 truncate">{query.query}</span>
                                <span className="text-gray-500">{query.count}</span>
                            </div>
                        ))}
                    </div>
                </div>
            )}
        </div>
    );
}

// Enhanced Search Box with Voice and AI
function EnhancedSearchBox() {
    const { refine, query } = useSearchBox();
    const [isListening, setIsListening] = useState(false);
    const [voiceSupported, setVoiceSupported] = useState(false);

    useEffect(() => {
        setVoiceSupported('webkitSpeechRecognition' in window || 'SpeechRecognition' in window);
    }, []);

    const startVoiceSearch = useCallback(() => {
        if (!voiceSupported) return;

        const SpeechRecognition = window.webkitSpeechRecognition || window.SpeechRecognition;
        const recognition = new SpeechRecognition();

        recognition.continuous = false;
        recognition.interimResults = false;
        recognition.lang = 'en-US';

        recognition.onstart = () => setIsListening(true);
        recognition.onend = () => setIsListening(false);

        recognition.onresult = (event) => {
            const transcript = event.results[0][0].transcript;
            refine(transcript);
        };

        recognition.onerror = (event) => {
            console.error('Speech recognition error:', event.error);
            setIsListening(false);
        };

        recognition.start();
    }, [voiceSupported, refine]);

    return (
        <div className="relative">
            <div className="relative">
                <SearchBox
                    placeholder="Search trades, strategies, symbols... Try: 'profitable BTC trades' or 'RSI strategy performance'"
                    classNames={{
                        root: 'relative',
                        form: 'relative flex',
                        input: 'flex-1 bg-gray-800 border border-gray-600 rounded-l-lg px-4 py-3 pl-12 text-white placeholder-gray-400 focus:border-cyan-500 focus:ring-1 focus:ring-cyan-500 focus:outline-none',
                        submit: 'hidden',
                        reset: 'absolute right-20 top-1/2 transform -translate-y-1/2 text-gray-400 hover:text-white',
                    }}
                />
                <div className="absolute left-3 top-1/2 transform -translate-y-1/2 text-gray-400">
                    🔍
                </div>

                {voiceSupported && (
                    <button
                        onClick={startVoiceSearch}
                        disabled={isListening}
                        className={`absolute right-2 top-1/2 transform -translate-y-1/2 p-2 rounded ${isListening
                            ? 'bg-red-600 text-white animate-pulse'
                            : 'bg-gray-700 text-gray-400 hover:bg-gray-600 hover:text-white'
                            } transition-all`}
                        title="Voice Search"
                    >
                        {isListening ? '🎤' : '🎙️'}
                    </button>
                )}
            </div>

            {query && (
                <div className="absolute top-full left-0 right-0 mt-1 bg-gray-800 border border-gray-600 rounded-lg p-2 text-xs text-gray-300">
                    Searching for: <span className="text-cyan-400 font-semibold">"{query}"</span>
                </div>
            )}
        </div>
    );
}

// Freqtrade Live Stats Component
function FreqtradeLiveStats() {
    const {
        data: freqtradeData,
        totalActiveBots,
        totalPnL,
        totalOpenTrades,
        isLoading
    } = useFreqtradeData();

    if (isLoading) {
        return (
            <div className="bg-gradient-to-r from-blue-900/20 to-purple-900/20 border border-blue-600/30 rounded-lg p-4 mb-6">
                <div className="animate-pulse flex items-center gap-4">
                    <div className="h-4 bg-gray-600 rounded w-32"></div>
                    <div className="h-8 bg-gray-600 rounded w-16"></div>
                </div>
            </div>
        );
    }

    return (
        <div className="bg-gradient-to-r from-blue-900/20 to-purple-900/20 border border-blue-600/30 rounded-lg p-4 mb-6">
            <div className="flex items-center justify-between">
                <div>
                    <h3 className="text-sm font-semibold text-blue-300 mb-2 flex items-center gap-2">
                        <Bot size={16} />
                        Live Freqtrade Stats
                    </h3>
                </div>
                <div className="flex gap-4 text-sm">
                    <div className="flex items-center gap-2">
                        <Activity size={14} className="text-blue-400" />
                        <span className="text-gray-300">Active Bots:</span>
                        <span className="text-blue-400 font-bold">{totalActiveBots}</span>
                    </div>
                    <div className="flex items-center gap-2">
                        <TrendingUp size={14} className="text-green-400" />
                        <span className="text-gray-300">Open Trades:</span>
                        <span className="text-green-400 font-bold">{totalOpenTrades}</span>
                    </div>
                    <div className="flex items-center gap-2">
                        <span className="text-gray-300">Total P&L:</span>
                        <span className={`font-bold ${totalPnL >= 0 ? 'text-green-400' : 'text-red-400'}`}>
                            ${totalPnL.toFixed(2)}
                        </span>
                    </div>
                </div>
            </div>
            {freqtradeData.bots.length > 0 && (
                <div className="mt-3 flex gap-2 flex-wrap">
                    {freqtradeData.bots.map((bot) => (
                        <div
                            key={bot.name}
                            className={`px-3 py-1 rounded-full text-xs font-medium flex items-center gap-1 ${bot.status === 'online'
                                    ? 'bg-green-900/30 text-green-300 border border-green-600/30'
                                    : 'bg-red-900/30 text-red-300 border border-red-600/30'
                                }`}
                        >
                            <div className={`w-2 h-2 rounded-full ${bot.status === 'online' ? 'bg-green-500' : 'bg-red-500'
                                }`} />
                            {bot.name}
                            <span className="ml-1 text-gray-400">
                                (${bot.profit.toFixed(2)})
                            </span>
                        </div>
                    ))}
                </div>
            )}
        </div>
    );
}

// Main Enhanced Algolia Search Component
export default function EnhancedAlgoliaSearch() {
    const { trackSearch } = useAlgoliaAnalytics();
    const { searchClient: demoClient, isDemo, setIsDemo } = useDemoSearchClient();

    const handleSuggestionClick = useCallback((query: string) => {
        // Handle suggestion click by updating search
        console.log('Suggestion clicked:', query);
    }, []);

    // Use demo client or real Algolia client based on mode
    const activeSearchClient = isDemo ? demoClient : cachedSearchClient;    // Custom stats component to track analytics
    function CustomStats() {
        const { nbHits, processingTimeMS, query } = useStats();

        useEffect(() => {
            if (query && processingTimeMS) {
                trackSearch(query, processingTimeMS, nbHits);
            }
        }, [query, processingTimeMS, nbHits]);

        return (
            <div className="flex items-center gap-4 text-sm text-gray-400">
                <Stats />
                {processingTimeMS && (
                    <span className="text-xs">
                        ⚡ {processingTimeMS}ms
                    </span>
                )}
            </div>
        );
    }

    return (
        <div className="min-h-screen bg-gradient-to-br from-gray-900 via-blue-900 to-gray-900 text-white">
            <InstantSearch
                searchClient={activeSearchClient}
                indexName={indexName}
            >
                <Configure />                <div className="container mx-auto px-4 py-8">
                    {/* Header */}
                    <div className="text-center mb-8">
                        <h1 className="text-4xl font-bold bg-gradient-to-r from-cyan-400 via-purple-500 to-pink-500 bg-clip-text text-transparent mb-2">
                            🚀 AlgoTrendy Intelligence Search
                        </h1>
                        <p className="text-gray-400">AI-powered search across trades, strategies & market insights</p>
                        <div className="mt-2 text-xs text-gray-500 flex items-center justify-center gap-4">
                            <span>Powered by Algolia • Real-time • Voice-enabled • Cost-optimized</span>
                            <button
                                onClick={() => setIsDemo(!isDemo)}
                                className={`px-3 py-1 rounded text-xs transition-colors ${isDemo
                                    ? 'bg-yellow-600 hover:bg-yellow-700 text-white'
                                    : 'bg-green-600 hover:bg-green-700 text-white'
                                    }`}
                            >
                                {isDemo ? '🧪 Demo Mode' : '🔴 Live Mode'}
                            </button>
                        </div>
                    </div>                    {/* Enhanced Search Box */}
                    <div className="max-w-4xl mx-auto mb-6">
                        <EnhancedSearchBox />
                    </div>

                    {/* Freqtrade Live Stats */}
                    <div className="max-w-4xl mx-auto">
                        <FreqtradeLiveStats />
                    </div>

                    {/* Smart Suggestions */}
                    <div className="max-w-4xl mx-auto">
                        <SmartSearchSuggestions onSuggestionClick={handleSuggestionClick} />
                    </div>

                    <div className="flex flex-col lg:flex-row gap-8">
                        {/* Enhanced Filters Sidebar */}
                        <div className="lg:w-1/4">
                            <div className="sticky top-8 space-y-4">
                                {/* Search Analytics */}
                                <SearchAnalyticsDashboard />

                                {/* Quick Actions */}
                                <div className="bg-gray-800 border border-gray-700 rounded-lg p-4">
                                    <h3 className="text-sm font-semibold text-gray-300 mb-3">⚡ Quick Actions</h3>
                                    <div className="space-y-2">
                                        <ClearRefinements
                                            classNames={{
                                                button: 'w-full bg-red-600 hover:bg-red-700 text-white py-2 px-4 rounded-lg transition-colors text-sm',
                                            }}
                                        />
                                        <button className="w-full bg-purple-600 hover:bg-purple-700 text-white py-2 px-4 rounded-lg transition-colors text-sm">
                                            🤖 AI Suggestions
                                        </button>
                                        <button className="w-full bg-blue-600 hover:bg-blue-700 text-white py-2 px-4 rounded-lg transition-colors text-sm">
                                            📈 Live Trades Only
                                        </button>
                                    </div>
                                </div>

                                {/* Enhanced Filters */}
                                {/* Trading Source Filter */}
                                <div className="bg-gray-800 border border-gray-700 rounded-lg p-4">
                                    <h3 className="text-sm font-semibold text-gray-300 mb-3 flex items-center gap-2">
                                        🤖 Trading Source
                                    </h3>
                                    <RefinementList
                                        attribute="source"
                                        classNames={{
                                            list: 'space-y-2',
                                            item: 'flex items-center',
                                            label: 'flex items-center cursor-pointer',
                                            checkbox: 'mr-2 rounded',
                                            labelText: 'text-sm text-gray-300',
                                            count: 'ml-auto text-xs bg-gray-700 px-2 py-1 rounded',
                                        }}
                                    />
                                </div>

                                {/* Freqtrade Bot Filter */}
                                <div className="bg-gray-800 border border-gray-700 rounded-lg p-4">
                                    <h3 className="text-sm font-semibold text-gray-300 mb-3 flex items-center gap-2">
                                        <Bot size={16} className="text-blue-400" />
                                        Freqtrade Bots
                                    </h3>
                                    <RefinementList
                                        attribute="bot_name"
                                        classNames={{
                                            list: 'space-y-2',
                                            item: 'flex items-center',
                                            label: 'flex items-center cursor-pointer',
                                            checkbox: 'mr-2 rounded',
                                            labelText: 'text-sm text-gray-300',
                                            count: 'ml-auto text-xs bg-blue-700 px-2 py-1 rounded',
                                        }}
                                    />
                                </div>

                                <div className="bg-gray-800 border border-gray-700 rounded-lg p-4">
                                    <h3 className="text-sm font-semibold text-gray-300 mb-3">🎯 Strategies</h3>
                                    <RefinementList
                                        attribute="strategy"
                                        limit={10}
                                        showMore={true}
                                        classNames={{
                                            list: 'space-y-2',
                                            item: 'flex items-center',
                                            label: 'flex items-center cursor-pointer',
                                            checkbox: 'mr-2 rounded',
                                            labelText: 'text-sm text-gray-300',
                                            count: 'ml-auto text-xs bg-gray-700 px-2 py-1 rounded',
                                            showMore: 'mt-2 text-xs text-cyan-400 hover:text-cyan-300 cursor-pointer',
                                        }}
                                    />
                                </div>

                                <div className="bg-gray-800 border border-gray-700 rounded-lg p-4">
                                    <h3 className="text-sm font-semibold text-gray-300 mb-3">💰 Symbols</h3>
                                    <RefinementList
                                        attribute="symbol"
                                        limit={8}
                                        showMore={true}
                                        classNames={{
                                            list: 'space-y-2',
                                            item: 'flex items-center',
                                            label: 'flex items-center cursor-pointer',
                                            checkbox: 'mr-2 rounded',
                                            labelText: 'text-sm text-gray-300',
                                            count: 'ml-auto text-xs bg-gray-700 px-2 py-1 rounded',
                                            showMore: 'mt-2 text-xs text-cyan-400 hover:text-cyan-300 cursor-pointer',
                                        }}
                                    />
                                </div>

                                <div className="bg-gray-800 border border-gray-700 rounded-lg p-4">
                                    <h3 className="text-sm font-semibold text-gray-300 mb-3">📊 Trade Side</h3>
                                    <RefinementList
                                        attribute="side"
                                        classNames={{
                                            list: 'space-y-2',
                                            item: 'flex items-center',
                                            label: 'flex items-center cursor-pointer',
                                            checkbox: 'mr-2 rounded',
                                            labelText: 'text-sm text-gray-300',
                                            count: 'ml-auto text-xs bg-gray-700 px-2 py-1 rounded',
                                        }}
                                    />
                                </div>

                                <div className="bg-gray-800 border border-gray-700 rounded-lg p-4">
                                    <h3 className="text-sm font-semibold text-gray-300 mb-3">💹 P&L Range</h3>
                                    <RangeInput
                                        attribute="pnl"
                                        classNames={{
                                            root: 'space-y-2',
                                            form: 'flex gap-2',
                                            input: 'bg-gray-700 border border-gray-600 rounded px-2 py-1 text-sm text-white w-full',
                                            separator: 'text-gray-400 self-center',
                                            submit: 'bg-cyan-600 hover:bg-cyan-700 text-white px-3 py-1 rounded text-sm',
                                        }}
                                    />
                                </div>

                                <div className="bg-gray-800 border border-gray-700 rounded-lg p-4">
                                    <h3 className="text-sm font-semibold text-gray-300 mb-3">🎯 Confidence</h3>
                                    <RangeInput
                                        attribute="confidence"
                                        classNames={{
                                            root: 'space-y-2',
                                            form: 'flex gap-2',
                                            input: 'bg-gray-700 border border-gray-600 rounded px-2 py-1 text-sm text-white w-full',
                                            separator: 'text-gray-400 self-center',
                                            submit: 'bg-cyan-600 hover:bg-cyan-700 text-white px-3 py-1 rounded text-sm',
                                        }}
                                    />
                                </div>

                                {/* Freqtrade-specific filters */}
                                <div className="bg-gray-800 border border-gray-700 rounded-lg p-4">
                                    <h3 className="text-sm font-semibold text-gray-300 mb-3 flex items-center gap-2">
                                        ⏱️ Trade Duration (min)
                                    </h3>
                                    <RangeInput
                                        attribute="duration_minutes"
                                        classNames={{
                                            root: 'space-y-2',
                                            form: 'flex gap-2',
                                            input: 'bg-gray-700 border border-gray-600 rounded px-2 py-1 text-sm text-white w-full',
                                            separator: 'text-gray-400 self-center',
                                            submit: 'bg-cyan-600 hover:bg-cyan-700 text-white px-3 py-1 rounded text-sm',
                                        }}
                                    />
                                </div>

                                <div className="bg-gray-800 border border-gray-700 rounded-lg p-4">
                                    <h3 className="text-sm font-semibold text-gray-300 mb-3">🎯 Entry Reason</h3>
                                    <RefinementList
                                        attribute="entry_reason"
                                        limit={6}
                                        showMore={true}
                                        classNames={{
                                            list: 'space-y-2',
                                            item: 'flex items-center',
                                            label: 'flex items-center cursor-pointer',
                                            checkbox: 'mr-2 rounded',
                                            labelText: 'text-sm text-gray-300',
                                            count: 'ml-auto text-xs bg-gray-700 px-2 py-1 rounded',
                                            showMore: 'mt-2 text-xs text-cyan-400 hover:text-cyan-300 cursor-pointer',
                                        }}
                                    />
                                </div>

                                <div className="bg-gray-800 border border-gray-700 rounded-lg p-4">
                                    <h3 className="text-sm font-semibold text-gray-300 mb-3">🚪 Exit Reason</h3>
                                    <RefinementList
                                        attribute="exit_reason"
                                        limit={6}
                                        showMore={true}
                                        classNames={{
                                            list: 'space-y-2',
                                            item: 'flex items-center',
                                            label: 'flex items-center cursor-pointer',
                                            checkbox: 'mr-2 rounded',
                                            labelText: 'text-sm text-gray-300',
                                            count: 'ml-auto text-xs bg-gray-700 px-2 py-1 rounded',
                                            showMore: 'mt-2 text-xs text-cyan-400 hover:text-cyan-300 cursor-pointer',
                                        }}
                                    />
                                </div>
                            </div>
                        </div>

                        {/* Enhanced Results */}
                        <div className="lg:w-3/4">
                            {/* Results Header with Advanced Stats */}
                            <div className="flex flex-col sm:flex-row justify-between items-start sm:items-center gap-4 mb-6">
                                <CustomStats />
                                <div className="flex gap-2">
                                    <SortBy
                                        items={[
                                            { label: '🕒 Most Recent', value: indexName },
                                            { label: '📈 Highest P&L', value: `${indexName}_pnl_desc` },
                                            { label: '📉 Lowest P&L', value: `${indexName}_pnl_asc` },
                                            { label: '🎯 Best Confidence', value: `${indexName}_confidence_desc` },
                                            { label: '⚡ Lowest Risk', value: `${indexName}_risk_asc` },
                                            { label: '🔤 Symbol A-Z', value: `${indexName}_symbol_asc` },
                                            { label: '🤖 Freqtrade First', value: `${indexName}_freqtrade_desc` },
                                            { label: '⏱️ Shortest Duration', value: `${indexName}_duration_asc` },
                                            { label: '⏱️ Longest Duration', value: `${indexName}_duration_desc` },
                                            { label: '🏆 Best Win Rate', value: `${indexName}_win_rate_desc` },
                                        ]}
                                        classNames={{
                                            select: 'bg-gray-800 border border-gray-600 rounded px-3 py-2 text-white text-sm',
                                        }}
                                    />
                                    <button className="bg-gray-800 border border-gray-600 rounded px-3 py-2 text-white text-sm hover:bg-gray-700">
                                        📊 Export
                                    </button>
                                </div>
                            </div>

                            {/* Enhanced Hits */}
                            <Hits
                                hitComponent={EnhancedTradeHit}
                                classNames={{
                                    root: 'space-y-4',
                                    list: 'space-y-4',
                                }}
                            />

                            {/* Enhanced Pagination */}
                            <div className="mt-8">
                                <Pagination
                                    classNames={{
                                        root: 'flex justify-center',
                                        list: 'flex gap-2',
                                        item: 'px-3 py-2 border border-gray-600 rounded hover:bg-gray-800 transition-colors',
                                        link: 'text-white',
                                        selectedItem: 'bg-cyan-600 border-cyan-600',
                                        disabledItem: 'opacity-50 cursor-not-allowed',
                                    }}
                                />
                            </div>
                        </div>
                    </div>
                </div>
            </InstantSearch>
        </div>
    );
}