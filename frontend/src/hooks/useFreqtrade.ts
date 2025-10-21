import { useQuery, useQueryClient } from '@tanstack/react-query';
import { api } from '../lib/api';

// Query keys for consistent caching
const FREQTRADE_QUERY_KEYS = {
    bots: ['freqtrade', 'bots'] as const,
    portfolio: ['freqtrade', 'portfolio'] as const,
    positions: (botName?: string) => ['freqtrade', 'positions', botName] as const,
    indexing: ['freqtrade', 'indexing'] as const,
};

// Types
export interface FreqtradeBot {
    name: string;
    port: number;
    strategy: string;
    status: 'online' | 'offline';
    balance: number;
    profit: number;
    profitPercent: number;
    openTrades: number;
    winRate: number;
    lastUpdated: string;
}

export interface BotPortfolio {
    balance: number;
    totalProfit: number;
    profitPercent: number;
    openTrades: number;
    closedTrades: number;
    winningTrades: number;
    losingTrades: number;
    winRate: number;
}

export interface FreqtradePortfolio {
    totalBalance: number;
    totalProfit: number;
    totalProfitPercent: number;
    openTrades: number;
    closedTrades: number;
    winningTrades: number;
    losingTrades: number;
    winRate: number;
    bestPerformingBot: string;
    worstPerformingBot: string;
    dailyPnL: number;
    bots: Record<string, BotPortfolio>;
}

export interface FreqtradePosition {
    id: string;
    botName: string;
    symbol: string;
    side: 'long' | 'short';
    entryPrice: number;
    currentPrice: number;
    quantity: number;
    pnL: number;
    pnLPercent: number;
    entryTime: string;
    durationMinutes: number;
    entryReason: string;
    stopLoss?: number;
    freqtradeTradeId: number;
    strategyId: string;
}

export interface FreqtradeBotsSummary {
    bots: FreqtradeBot[];
    totalBots: number;
    onlineBots: number;
    totalBalance: number;
    totalProfit: number;
    totalOpenTrades: number;
    lastUpdated: string;
}

// Hook for fetching all Freqtrade bots
export function useFreqtradeBots() {
    return useQuery({
        queryKey: FREQTRADE_QUERY_KEYS.bots,
        queryFn: async () => {
            const response = await api.get<{ success: boolean; data: FreqtradeBotsSummary }>('/freqtrade/bots');
            if (!response.data.success) {
                throw new Error('Failed to fetch Freqtrade bots');
            }
            return response.data.data;
        },
        staleTime: 30_000, // 30 seconds
        gcTime: 5 * 60_000, // 5 minutes garbage collection
        retry: 3,
        retryDelay: (attemptIndex) => Math.min(1000 * 2 ** attemptIndex, 30_000),
        refetchOnWindowFocus: true,
        refetchInterval: 60_000, // Refetch every minute for live data
    });
}

// Hook for fetching Freqtrade portfolio data
export function useFreqtradePortfolio() {
    return useQuery({
        queryKey: FREQTRADE_QUERY_KEYS.portfolio,
        queryFn: async () => {
            const response = await api.get<{ success: boolean; data: FreqtradePortfolio }>('/freqtrade/portfolio');
            if (!response.data.success) {
                throw new Error('Failed to fetch Freqtrade portfolio');
            }
            return response.data.data;
        },
        staleTime: 30_000, // 30 seconds
        gcTime: 5 * 60_000, // 5 minutes
        retry: 3,
        retryDelay: (attemptIndex) => Math.min(1000 * 2 ** attemptIndex, 30_000),
        refetchOnWindowFocus: true,
        refetchInterval: 45_000, // Refetch every 45 seconds
    });
}

// Hook for fetching Freqtrade positions
export function useFreqtradePositions(botName?: string) {
    return useQuery({
        queryKey: FREQTRADE_QUERY_KEYS.positions(botName),
        queryFn: async () => {
            const params = botName ? { botName } : {};
            const response = await api.get<{ success: boolean; data: FreqtradePosition[] }>(
                '/freqtrade/positions',
                { params }
            );
            if (!response.data.success) {
                throw new Error('Failed to fetch Freqtrade positions');
            }
            return response.data.data;
        },
        staleTime: 20_000, // 20 seconds (more frequent for positions)
        gcTime: 3 * 60_000, // 3 minutes
        retry: 3,
        retryDelay: (attemptIndex) => Math.min(1000 * 2 ** attemptIndex, 20_000),
        refetchOnWindowFocus: true,
        refetchInterval: 30_000, // Refetch every 30 seconds for active positions
        enabled: true,
    });
}

// Hook for triggering Freqtrade data indexing
export function useFreqtradeIndexing() {
    const queryClient = useQueryClient();

    return useQuery({
        queryKey: FREQTRADE_QUERY_KEYS.indexing,
        queryFn: async () => {
            const response = await api.post<{ success: boolean; data: any }>('/freqtrade/index');
            if (!response.data.success) {
                throw new Error('Failed to trigger Freqtrade indexing');
            }

            // Invalidate and refetch all Freqtrade data after successful indexing
            await queryClient.invalidateQueries({
                queryKey: ['freqtrade'],
            });

            return response.data.data;
        },
        enabled: false, // Manual trigger only
        retry: 2,
        staleTime: 0, // Always fresh when triggered
    });
}

// Combined hook for all Freqtrade data with loading states
export function useFreqtradeData(botName?: string) {
    const botsQuery = useFreqtradeBots();
    const portfolioQuery = useFreqtradePortfolio();
    const positionsQuery = useFreqtradePositions(botName);

    const isLoading = botsQuery.isLoading || portfolioQuery.isLoading || positionsQuery.isLoading;
    const isError = botsQuery.isError || portfolioQuery.isError || positionsQuery.isError;
    const error = botsQuery.error || portfolioQuery.error || positionsQuery.error;

    // Aggregate data
    const data = {
        bots: botsQuery.data?.bots || [],
        portfolio: portfolioQuery.data || null,
        positions: positionsQuery.data || [],
    };

    // Computed values
    const totalActiveBots = data.bots.filter((bot) => bot.status === 'online').length;
    const totalPnL = data.portfolio?.totalProfit || 0;
    const totalOpenTrades = data.positions.length;
    const bestPerformingBot = data.bots.reduce(
        (best, current) => ((current.profit > (best?.profit || -Infinity)) ? current : best),
        null as FreqtradeBot | null
    );

    const refetchAll = async () => {
        await Promise.all([
            botsQuery.refetch(),
            portfolioQuery.refetch(),
            positionsQuery.refetch(),
        ]);
    };

    return {
        data,
        isLoading,
        isError,
        error,
        totalActiveBots,
        totalPnL,
        totalOpenTrades,
        bestPerformingBot,
        refetchAll,
        queries: {
            bots: botsQuery,
            portfolio: portfolioQuery,
            positions: positionsQuery,
        },
    };
}

// Hook for bot-specific data
export function useBotData(botName: string) {
    const { data: botsData } = useFreqtradeBots();
    const { data: positions } = useFreqtradePositions(botName);

    const bot = botsData?.bots.find(
        (b) => b.name.toLowerCase().replace(/\s+/g, '_') === botName.toLowerCase().replace(/\s+/g, '_')
    );

    const botPositions = positions?.filter(
        (p) => p.botName?.toLowerCase().replace(/\s+/g, '_') === botName.toLowerCase().replace(/\s+/g, '_')
    ) || [];

    return {
        bot,
        positions: botPositions,
        isOnline: bot?.status === 'online',
        openTradesCount: botPositions.length,
        totalPnL: botPositions.reduce((sum, pos) => sum + (pos.pnL || 0), 0),
    };
}

// Export query keys for external use
export { FREQTRADE_QUERY_KEYS };
