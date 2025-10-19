// COMPLETED: Freqtrade data integration hooks
// COMPLETED: useFreqtradeBots - Fetch all bots with auto-refresh every 60s
// COMPLETED: useFreqtradePortfolio - Fetch portfolio data with auto-refresh every 45s
// COMPLETED: useFreqtradePositions - Fetch positions with bot filtering
// COMPLETED: useFreqtradeIndexing - Manual trigger for data indexing
// COMPLETED: useFreqtradeData - Combined hook with aggregated stats
// COMPLETED: useBotData - Bot-specific data hook
// COMPLETED: Automatic query invalidation after indexing
// COMPLETED: Smart caching with staleTime and gcTime
// COMPLETED: Retry logic with exponential backoff
// TODO: Add WebSocket integration for real-time updates
// TODO: Implement optimistic updates for bot actions
// TODO: Add error recovery strategies
// TODO: Implement query cancellation on unmount
// TODO: Add data transformation and normalization layer
// TODO: Implement offline mode with cached data
import { useQuery, useQueryClient } from '@tanstack/react-query';
import { useTradingStore } from '@/store/tradingStore';
import { apiService } from '@/services/api';
import { FreqtradeBot, FreqtradePortfolio, FreqtradePosition } from '@/types';

// Query keys for consistent caching
const FREQTRADE_QUERY_KEYS = {
    bots: ['freqtrade', 'bots'] as const,
    portfolio: ['freqtrade', 'portfolio'] as const,
    positions: (botName?: string) => ['freqtrade', 'positions', botName] as const,
    indexing: ['freqtrade', 'indexing'] as const,
};

// Hook for fetching all Freqtrade bots
export function useFreqtradeBots() {
    const { setFreqtradeBots, setFreqtradeLoading } = useTradingStore();

    return useQuery({
        queryKey: FREQTRADE_QUERY_KEYS.bots,
        queryFn: async () => {
            setFreqtradeLoading(true);
            try {
                const response = await apiService.getFreqtradeBots();
                if (response.success) {
                    setFreqtradeBots(response.data);
                    return response.data;
                }
                throw new Error(response.error || 'Failed to fetch Freqtrade bots');
            } catch (error) {
                console.error('Error fetching Freqtrade bots:', error);
                throw error;
            } finally {
                setFreqtradeLoading(false);
            }
        },
        staleTime: 30_000, // 30 seconds
        gcTime: 5 * 60_000, // 5 minutes garbage collection
        retry: 3,
        retryDelay: attemptIndex => Math.min(1000 * 2 ** attemptIndex, 30_000),
        refetchOnWindowFocus: true,
        refetchInterval: 60_000, // Refetch every minute for live data
    });
}

// Hook for fetching Freqtrade portfolio data
export function useFreqtradePortfolio() {
    const { setFreqtradePortfolio, setFreqtradeLoading } = useTradingStore();

    return useQuery({
        queryKey: FREQTRADE_QUERY_KEYS.portfolio,
        queryFn: async () => {
            setFreqtradeLoading(true);
            try {
                const response = await apiService.getFreqtradePortfolio();
                if (response.success) {
                    setFreqtradePortfolio(response.data);
                    return response.data;
                }
                throw new Error(response.error || 'Failed to fetch Freqtrade portfolio');
            } catch (error) {
                console.error('Error fetching Freqtrade portfolio:', error);
                throw error;
            } finally {
                setFreqtradeLoading(false);
            }
        },
        staleTime: 30_000, // 30 seconds
        gcTime: 5 * 60_000, // 5 minutes
        retry: 3,
        retryDelay: attemptIndex => Math.min(1000 * 2 ** attemptIndex, 30_000),
        refetchOnWindowFocus: true,
        refetchInterval: 45_000, // Refetch every 45 seconds
    });
}

// Hook for fetching Freqtrade positions
export function useFreqtradePositions(botName?: string) {
    const { setFreqtradePositions, setFreqtradeLoading } = useTradingStore();

    return useQuery({
        queryKey: FREQTRADE_QUERY_KEYS.positions(botName),
        queryFn: async () => {
            setFreqtradeLoading(true);
            try {
                const response = await apiService.getFreqtradePositions(botName);
                if (response.success) {
                    setFreqtradePositions(response.data);
                    return response.data;
                }
                throw new Error(response.error || 'Failed to fetch Freqtrade positions');
            } catch (error) {
                console.error('Error fetching Freqtrade positions:', error);
                throw error;
            } finally {
                setFreqtradeLoading(false);
            }
        },
        staleTime: 20_000, // 20 seconds (more frequent for positions)
        gcTime: 3 * 60_000, // 3 minutes
        retry: 3,
        retryDelay: attemptIndex => Math.min(1000 * 2 ** attemptIndex, 20_000),
        refetchOnWindowFocus: true,
        refetchInterval: 30_000, // Refetch every 30 seconds for active positions
        enabled: true, // Always enabled, but can be controlled by passing undefined botName
    });
}

// Hook for triggering Freqtrade data indexing
export function useFreqtradeIndexing() {
    const queryClient = useQueryClient();

    return useQuery({
        queryKey: FREQTRADE_QUERY_KEYS.indexing,
        queryFn: async () => {
            try {
                const response = await apiService.triggerFreqtradeIndexing();
                if (response.success) {
                    // Invalidate and refetch all Freqtrade data after successful indexing
                    await queryClient.invalidateQueries({
                        queryKey: ['freqtrade'],
                    });
                    return response.data;
                }
                throw new Error(response.error || 'Failed to trigger Freqtrade indexing');
            } catch (error) {
                console.error('Error triggering Freqtrade indexing:', error);
                throw error;
            }
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
        bots: botsQuery.data || [],
        portfolio: portfolioQuery.data || null,
        positions: positionsQuery.data || [],
    };

    // Computed values
    const totalActiveBots = data.bots.filter(bot => bot.status === 'online').length;
    const totalPnL = data.portfolio?.total_profit || 0;
    const totalOpenTrades = data.positions.length;
    const bestPerformingBot = data.bots.reduce((best, current) =>
        (current.profit > (best?.profit || -Infinity)) ? current : best,
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
    const { data: allBots } = useFreqtradeBots();
    const { data: positions } = useFreqtradePositions(botName);

    const bot = allBots?.find(b => b.name.toLowerCase().replace(/\s+/g, '_') === botName.toLowerCase().replace(/\s+/g, '_'));
    const botPositions = positions?.filter(p =>
        p.bot_name?.toLowerCase().replace(/\s+/g, '_') === botName.toLowerCase().replace(/\s+/g, '_')
    ) || [];

    return {
        bot,
        positions: botPositions,
        isOnline: bot?.status === 'online',
        openTradesCount: botPositions.length,
        totalPnL: botPositions.reduce((sum, pos) => sum + (pos.pnl || 0), 0),
    };
}

// Export query keys for external use
export { FREQTRADE_QUERY_KEYS };