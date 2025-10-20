// Common types
export interface ApiResponse<T> {
  success: boolean;
  data: T;
  message?: string;
  error?: string;
}

// Dataset types
export interface CompanyData {
  ticker: string;
  name: string;
  sector: string;
  industry?: string;
  headquarters: string;
  exchange: string;
  fye: string;
  lastUpdated: string;
}

export interface DailyData {
  sharePrice: string;
  dailyVolume: string;
  pe: string;
  avgVolume: string;
  marketCap?: string;
  week52High?: string;
  week52Low?: string;
}

export interface FinancialMetric {
  metric: string;
  q3_2024: { value: string; growth: string | null };
  q2_2024: { value: string; growth: string | null };
  q1_2024: { value: string; growth: string | null };
  q4_2023?: { value: string; growth: string | null };
}

export interface Filing {
  type: string;
  date: string;
  url?: string;
}

// Strategy types
export interface Condition {
  id: string;
  indicator: string;
  operator: string;
  value: string;
  params?: Record<string, any>;
}

export interface Strategy {
  id?: string;
  name: string;
  description?: string;
  conditions: Condition[];
  exitConditions?: Condition[];
  timeframe: string;
  assetClass: string;
  createdAt?: string;
  updatedAt?: string;
}

export interface BacktestRequest {
  strategyId: string;
  startDate: string;
  endDate: string;
  initialCapital: number;
  symbols?: string[];
}

export interface BacktestResult {
  totalReturn: string;
  sharpeRatio: string;
  maxDrawdown: string;
  winRate: string;
  trades: number;
  profitFactor?: string;
  avgWin?: string;
  avgLoss?: string;
  tradeHistory?: Trade[];
}

export interface Trade {
  date: string;
  type: 'Long' | 'Short';
  symbol: string;
  entryPrice: string;
  exitPrice: string;
  quantity: number;
  return: string;
  pnl?: string;
}

// Query types
export interface QueryRequest {
  query: string;
  ticker?: string;
  parameters?: Record<string, any>;
}

export interface QueryResult {
  title: string;
  data: any[];
  commentary?: Commentary[];
  sources?: Source[];
}

export interface Commentary {
  quarter: string;
  text: string;
}

export interface Source {
  company: string;
  items: SourceItem[];
}

export interface SourceItem {
  type: string;
  date: string;
  url?: string;
}

// AI types
export interface ChatMessage {
  id: string;
  role: 'user' | 'assistant';
  content: string;
  timestamp: Date;
}

export interface ChatRequest {
  message: string;
  history?: ChatMessage[];
  context?: {
    currentView?: string;
    ticker?: string;
    strategyId?: string;
  };
}

export interface ChatResponse {
  message: string;
  suggestions?: string[];
}

// Analytics types
export interface PortfolioMetrics {
  totalValue: string;
  totalReturn: string;
  sharpeRatio: string;
  volatility: string;
  positions: Position[];
}

export interface Position {
  symbol: string;
  quantity: number;
  avgCost: string;
  currentPrice: string;
  pnl: string;
  pnlPercent: string;
}

// ============================================
// BACKEND API TYPES (AlgoTrendy v2.6)
// ============================================

// Market Data Types
export interface MarketData {
  symbol: string;
  timestamp: string;
  open: number;
  high: number;
  low: number;
  close: number;
  volume: number;
  source?: string;
}

export interface MarketDataLatest {
  symbol: string;
  close: number;
  volume: number;
  timestamp: string;
  open?: number;
  high?: number;
  low?: number;
}

export interface AggregatedDataRequest {
  symbol: string;
  interval: '1h' | '1d' | '1w' | '1M';
  startTime?: string;
  endTime?: string;
}

// Trading Types
export type OrderSide = 'Buy' | 'Sell';
export type OrderType = 'Market' | 'Limit' | 'StopLoss' | 'StopLimit' | 'TakeProfit';
export type OrderStatus = 'Pending' | 'Open' | 'PartiallyFilled' | 'Filled' | 'Cancelled' | 'Rejected' | 'Expired';

export interface Order {
  orderId: string;
  clientOrderId?: string;
  exchangeOrderId?: string;
  symbol: string;
  exchange: string;
  side: OrderSide;
  type: OrderType;
  status: OrderStatus;
  quantity: number;
  filledQuantity?: number;
  price?: number;
  averageFillPrice?: number;
  createdAt: string;
  updatedAt: string;
}

export interface OrderRequest {
  symbol: string;
  exchange: string;
  side: OrderSide;
  type: OrderType;
  quantity: number;
  price?: number;
  stopPrice?: number;
  clientOrderId?: string;
}

export interface Balance {
  exchange: string;
  currency: string;
  balance: number;
  timestamp: string;
}

// Portfolio Types
export type MarginType = 'Cross' | 'Isolated';
export type MarginHealthStatus = 'HEALTHY' | 'CAUTION' | 'WARNING' | 'CRITICAL';

export interface PortfolioSummary {
  totalBorrowedAmount: number;
  totalCollateralAmount: number;
  marginHealthRatio: number;
  activeLeveragedPositions: number;
  isAtLiquidationRisk: boolean;
}

export interface MarginHealth {
  marginHealthRatio: number;
  status: MarginHealthStatus;
  isLiquidationRisk: boolean;
}

export interface TradingPosition {
  positionId: string;
  symbol: string;
  exchange: string;
  side: OrderSide;
  quantity: number;
  entryPrice: number;
  currentPrice: number;
  stopLoss?: number;
  takeProfit?: number;
  leverage: number;
  marginType: MarginType;
  liquidationPrice?: number;
  marginHealthRatio: number;
  unrealizedPnL: number;
  unrealizedPnLPercent: number;
  openedAt: string;
}

export interface LeverageInfo {
  symbol: string;
  currentLeverage: number;
  maxLeverage: number;
  marginType: MarginType;
}

export interface SetLeverageRequest {
  symbol: string;
  leverage: number;
  marginType: MarginType;
}

// Backtesting Types
export type AssetClass = 'Cryptocurrency' | 'Stock' | 'Futures' | 'Options' | 'ETF' | 'Forex';

export interface BacktestConfig {
  symbols: string[];
  timeframes: string[];
  assetClasses: AssetClass[];
  indicators: string[];
  maxBacktestDuration: string;
}

export interface BacktestRunRequest {
  symbol: string;
  assetClass: AssetClass;
  startDate: string;
  endDate: string;
  strategy: string;
  initialCapital: number;
  parameters?: Record<string, any>;
}

export interface BacktestResultDetailed {
  id: string;
  symbol: string;
  assetClass: AssetClass;
  startDate: string;
  endDate: string;
  initialCapital: number;
  finalCapital: number;
  totalReturn: number;
  totalReturnPercent: number;
  sharpeRatio: number;
  maxDrawdown: number;
  maxDrawdownPercent: number;
  winRate: number;
  totalTrades: number;
  profitFactor: number;
  avgWin: number;
  avgLoss: number;
  largestWin: number;
  largestLoss: number;
  trades: BacktestTrade[];
  equityCurve: EquityPoint[];
  status: 'Running' | 'Completed' | 'Failed';
  createdAt: string;
  completedAt?: string;
}

export interface BacktestTrade {
  tradeId: string;
  entryDate: string;
  exitDate: string;
  side: OrderSide;
  entryPrice: number;
  exitPrice: number;
  quantity: number;
  pnl: number;
  pnlPercent: number;
  commission: number;
}

export interface EquityPoint {
  date: string;
  equity: number;
}

export interface BacktestHistoryItem {
  id: string;
  symbol: string;
  assetClass: AssetClass;
  strategy: string;
  startDate: string;
  endDate: string;
  totalReturn: number;
  sharpeRatio: number;
  status: string;
  createdAt: string;
}

export interface Indicator {
  name: string;
  displayName: string;
  description: string;
  category: string;
  parameters: IndicatorParameter[];
}

export interface IndicatorParameter {
  name: string;
  displayName: string;
  type: 'number' | 'string' | 'boolean';
  defaultValue: any;
  min?: number;
  max?: number;
  options?: string[];
}

// Crypto Data Types (Finnhub)
export interface CryptoCandle {
  timestamp: number;
  open: number;
  high: number;
  low: number;
  close: number;
  volume: number;
}

export interface CryptoQuote {
  symbol: string;
  price: number;
  timestamp: number;
}

export interface CryptoExchange {
  name: string;
  code: string;
}

export interface CryptoSymbol {
  symbol: string;
  displaySymbol: string;
  description: string;
}

// WebSocket Types
export interface WebSocketMessage {
  type: 'MarketData' | 'OrderUpdate' | 'PositionUpdate' | 'Ping' | 'Pong';
  data: any;
  timestamp: string;
}

export interface MarketDataUpdate {
  symbol: string;
  timestamp: string;
  open: number;
  high: number;
  low: number;
  close: number;
  volume: number;
}
