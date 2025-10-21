// COMPLETED: Zustand store for trading state management
// COMPLETED: Standard portfolio, strategies, and positions state
// COMPLETED: Freqtrade bots, portfolio, and positions state integration
// COMPLETED: Bot selection and filtering support
// COMPLETED: Separate loading states for Freqtrade data
// TODO: Add persistence layer (localStorage/sessionStorage)
// TODO: Implement undo/redo functionality for trades
// TODO: Add computed selectors for derived state
// TODO: Implement state hydration from server
// TODO: Add state synchronization across tabs
// TODO: Implement state migration for schema changes
import { create } from 'zustand';
import { Strategy, Position, Portfolio, FreqtradeBot, FreqtradePortfolio, FreqtradePosition } from '@/types';

interface TradingState {
  portfolio: Portfolio | null;
  strategies: Strategy[];
  positions: Position[];
  selectedStrategy: Strategy | null;
  isLoading: boolean;
  error: string | null;

  // Freqtrade-specific state
  freqtradeBots: FreqtradeBot[];
  freqtradePortfolio: FreqtradePortfolio | null;
  freqtradePositions: FreqtradePosition[];
  selectedBot: FreqtradeBot | null;
  freqtradeLoading: boolean;

  // Standard actions
  setPortfolio: (portfolio: Portfolio) => void;
  setStrategies: (strategies: Strategy[]) => void;
  setPositions: (positions: Position[]) => void;
  setSelectedStrategy: (strategy: Strategy | null) => void;
  setLoading: (loading: boolean) => void;
  setError: (error: string | null) => void;
  addStrategy: (strategy: Strategy) => void;
  removeStrategy: (strategyId: string) => void;

  // Freqtrade actions
  setFreqtradeBots: (bots: FreqtradeBot[]) => void;
  setFreqtradePortfolio: (portfolio: FreqtradePortfolio) => void;
  setFreqtradePositions: (positions: FreqtradePosition[]) => void;
  setSelectedBot: (bot: FreqtradeBot | null) => void;
  setFreqtradeLoading: (loading: boolean) => void;
}

export const useTradingStore = create<TradingState>((set) => ({
  // Standard state
  portfolio: null,
  strategies: [],
  positions: [],
  selectedStrategy: null,
  isLoading: false,
  error: null,

  // Freqtrade state
  freqtradeBots: [],
  freqtradePortfolio: null,
  freqtradePositions: [],
  selectedBot: null,
  freqtradeLoading: false,

  // Standard actions
  setPortfolio: (portfolio) => set({ portfolio }),
  setStrategies: (strategies) => set({ strategies }),
  setPositions: (positions) => set({ positions }),
  setSelectedStrategy: (strategy) => set({ selectedStrategy: strategy }),
  setLoading: (loading) => set({ isLoading: loading }),
  setError: (error) => set({ error }),
  addStrategy: (strategy) => set((state) => ({
    strategies: [...state.strategies, strategy]
  })),
  removeStrategy: (strategyId) => set((state) => ({
    strategies: state.strategies.filter(s => s.id !== strategyId)
  })),

  // Freqtrade actions
  setFreqtradeBots: (bots) => set({ freqtradeBots: bots }),
  setFreqtradePortfolio: (portfolio) => set({ freqtradePortfolio: portfolio }),
  setFreqtradePositions: (positions) => set({ freqtradePositions: positions }),
  setSelectedBot: (bot) => set({ selectedBot: bot }),
  setFreqtradeLoading: (loading) => set({ freqtradeLoading: loading }),
}));
