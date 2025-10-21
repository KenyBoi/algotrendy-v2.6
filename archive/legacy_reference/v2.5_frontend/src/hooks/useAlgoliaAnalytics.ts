import { useState, useEffect, useCallback } from 'react';

interface SearchAnalytics {
    totalQueries: number;
    avgResponseTime: number;
    topQueries: { query: string; count: number }[];
    costEstimate: number;
    operationsThisMonth: number;
    remainingFreeOperations: number;
}

interface SearchMetrics {
    query: string;
    responseTime: number;
    resultsCount: number;
    timestamp: number;
}

const COST_PER_1000_OPERATIONS = 0.50; // Essential plan pricing
const FREE_TIER_LIMIT = 10000;

export function useAlgoliaAnalytics() {
    const [analytics, setAnalytics] = useState<SearchAnalytics>({
        totalQueries: 0,
        avgResponseTime: 0,
        topQueries: [],
        costEstimate: 0,
        operationsThisMonth: 0,
        remainingFreeOperations: FREE_TIER_LIMIT,
    });

    const [searchMetrics, setSearchMetrics] = useState<SearchMetrics[]>([]);

    // Track search operations
    const trackSearch = useCallback((query: string, responseTime: number, resultsCount: number) => {
        const metric: SearchMetrics = {
            query,
            responseTime,
            resultsCount,
            timestamp: Date.now(),
        };

        setSearchMetrics(prev => {
            const updated = [...prev, metric];
            // Keep only last 1000 searches
            return updated.slice(-1000);
        });

        // Update analytics
        setAnalytics(prev => {
            const newTotalQueries = prev.totalQueries + 1;
            const newOperationsThisMonth = prev.operationsThisMonth + 1;

            // Calculate average response time
            const totalResponseTime = searchMetrics.reduce((sum, m) => sum + m.responseTime, 0) + responseTime;
            const avgResponseTime = totalResponseTime / newTotalQueries;

            // Calculate top queries
            const queryCount = new Map<string, number>();
            [...searchMetrics, metric].forEach(m => {
                if (m.query.trim()) {
                    queryCount.set(m.query, (queryCount.get(m.query) || 0) + 1);
                }
            });

            const topQueries = Array.from(queryCount.entries())
                .map(([query, count]) => ({ query, count }))
                .sort((a, b) => b.count - a.count)
                .slice(0, 10);

            // Calculate cost estimate
            const costEstimate = Math.max(0, (newOperationsThisMonth - FREE_TIER_LIMIT)) * (COST_PER_1000_OPERATIONS / 1000);
            const remainingFreeOperations = Math.max(0, FREE_TIER_LIMIT - newOperationsThisMonth);

            return {
                totalQueries: newTotalQueries,
                avgResponseTime,
                topQueries,
                costEstimate,
                operationsThisMonth: newOperationsThisMonth,
                remainingFreeOperations,
            };
        });

        // Store in localStorage for persistence
        localStorage.setItem('algolia_analytics', JSON.stringify({
            totalQueries: analytics.totalQueries + 1,
            operationsThisMonth: analytics.operationsThisMonth + 1,
            lastUpdate: Date.now(),
        }));
    }, [searchMetrics, analytics]);

    // Load persisted analytics on mount
    useEffect(() => {
        const stored = localStorage.getItem('algolia_analytics');
        if (stored) {
            try {
                const parsed = JSON.parse(stored);
                const now = Date.now();
                const oneMonth = 30 * 24 * 60 * 60 * 1000;

                // Reset if data is older than a month
                if (now - parsed.lastUpdate > oneMonth) {
                    localStorage.removeItem('algolia_analytics');
                } else {
                    setAnalytics(prev => ({
                        ...prev,
                        totalQueries: parsed.totalQueries || 0,
                        operationsThisMonth: parsed.operationsThisMonth || 0,
                    }));
                }
            } catch (error) {
                console.warn('Failed to parse stored analytics:', error);
            }
        }
    }, []);

    const resetAnalytics = useCallback(() => {
        setAnalytics({
            totalQueries: 0,
            avgResponseTime: 0,
            topQueries: [],
            costEstimate: 0,
            operationsThisMonth: 0,
            remainingFreeOperations: FREE_TIER_LIMIT,
        });
        setSearchMetrics([]);
        localStorage.removeItem('algolia_analytics');
    }, []);

    return {
        analytics,
        searchMetrics,
        trackSearch,
        resetAnalytics,
        isLoading: false,
        error: null,
    };
}