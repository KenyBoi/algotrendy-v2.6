# GAP04: Frontend Migration Implementation Plan

**Status:** CRITICAL SHOWSTOPPER
**Priority:** P0 (Blocks all frontend work)
**Effort:** 10-12 days
**Current State:** 0% implementation - Empty directories only
**Target:** Complete Next.js 15 frontend with C# backend integration

---

## Executive Summary

### Current State Analysis

**v2.5 Frontend Inventory (Source: `/root/algotrendy_v2.5/algotrendy-web/`)**

- **Framework:** Next.js 14.0.0 (Pages Router)
- **React:** 18.2.0
- **State Management:** Zustand 4.4.0
- **Styling:** Tailwind CSS 3.3.0
- **API Client:** Axios 1.5.0
- **Real-time:** Socket.IO Client 4.7.0
- **Data Fetching:** TanStack React Query 5.0.0
- **Search:** Algolia InstantSearch 6.47.3
- **Charts:** Recharts 3.3.0

**Asset Inventory:**

```
Total Components: 14 TSX files (~2,814 lines)
Total Pages: 9 (dashboard, backtesting, dev-systems, login, search, settings, test, index, _app)
Hooks: 4 (useWebSocket, useFreqtrade, useToast, useAlgoliaAnalytics)
Services: 3 (api.ts, websocket.ts, backtest.ts)
Stores: 2 (authStore.ts, tradingStore.ts)
Types: 2 files (index.ts, speech.d.ts)
```

**Component Breakdown:**

| Category | Components | Purpose |
|----------|-----------|---------|
| Layout | 2 | Header, Sidebar |
| Dashboard | 6 | BotControlPanel, PositionsTable, BotPerformanceChart, PerformanceChart, LivePriceTicker, PortfolioCard |
| UI | 6 | Toast, ToastContainer, WebSocketStatus, PasswordStrength, EmptyState |
| Search | 1 | EnhancedAlgoliaSearch |

**v2.6 Target State:**

- **Framework:** Next.js 15.1.2 (App Router)
- **Backend:** C# ASP.NET Core 9.0 (from GAP01)
- **Real-time:** SignalR (replacing Socket.IO)
- **API Endpoints:** RESTful C# controllers
- **Authentication:** JWT from AuthController (GAP01)

### Migration Strategy

**COPY-FIRST APPROACH:** Preserve existing structure, then upgrade incrementally.

1. **Phase 1 (Days 1-2):** Copy entire directory, audit dependencies
2. **Phase 2 (Days 3-4):** Upgrade to Next.js 15, migrate to App Router
3. **Phase 3 (Days 5-7):** Replace Python API calls with C# endpoints
4. **Phase 4 (Days 8-9):** Integrate SignalR, migrate authentication
5. **Phase 5 (Day 10):** Testing, validation, acceptance

---

## Day-by-Day Implementation Plan

### Day 1: Directory Copy & Dependency Audit

**Tasks:**

1. **Copy v2.5 frontend to v2.6**
   ```bash
   # Backup current v2.6 frontend (if needed)
   mv /root/AlgoTrendy_v2.6/frontend /root/AlgoTrendy_v2.6/frontend.backup

   # Copy v2.5 frontend
   cp -r /root/algotrendy_v2.5/algotrendy-web /root/AlgoTrendy_v2.6/frontend

   # Clean build artifacts
   cd /root/AlgoTrendy_v2.6/frontend
   rm -rf .next node_modules package-lock.json
   rm -f .env.local
   ```

2. **Create new package.json for Next.js 15**
   - Upgrade Next.js: 14.0.0 → 15.1.2
   - Upgrade React: 18.2.0 → 19.0.0
   - Add SignalR: @microsoft/signalr ^8.0.0
   - Remove Socket.IO dependencies
   - Update all dependencies to latest compatible versions

3. **Create environment template**
   ```bash
   # Create .env.example
   NEXT_PUBLIC_API_URL=http://localhost:5000/api
   NEXT_PUBLIC_SIGNALR_HUB_URL=http://localhost:5000/hubs/trading
   NEXT_PUBLIC_ALGOLIA_APP_ID=your_app_id
   NEXT_PUBLIC_ALGOLIA_SEARCH_KEY=your_search_key
   ```

4. **Audit migration scope**
   - Document all API calls in existing services
   - Map Python endpoints to C# controllers
   - Identify WebSocket → SignalR migration points

**Deliverables:**
- Clean v2.5 copy in v2.6 directory
- Updated package.json with Next.js 15
- Environment configuration template
- API migration mapping document

---

### Day 2: Next.js 15 Upgrade & Build Fix

**Tasks:**

1. **Install dependencies**
   ```bash
   npm install
   ```

2. **Update next.config.js for Next.js 15**
   ```javascript
   /** @type {import('next').NextConfig} */
   const nextConfig = {
     reactStrictMode: true,
     swcMinify: true,
     // Next.js 15 specific configurations
     experimental: {
       serverActions: {
         bodySizeLimit: '2mb',
       },
     },
     // API proxy for C# backend
     async rewrites() {
       return [
         {
           source: '/api/:path*',
           destination: 'http://localhost:5000/api/:path*',
         },
       ];
     },
   };

   module.exports = nextConfig;
   ```

3. **Fix React 19 compatibility issues**
   - Update component syntax if needed
   - Replace deprecated APIs (ReactDOM.render → createRoot)
   - Fix TypeScript errors

4. **Verify build succeeds**
   ```bash
   npm run build
   npm run dev
   ```

5. **Test existing pages load (even with broken API calls)**
   - Verify routing works
   - Check component rendering
   - Validate styling loads

**Deliverables:**
- Successfully building Next.js 15 app
- All pages accessible (with expected API errors)
- No TypeScript compilation errors

---

### Day 3: App Router Migration - Layout & Routing

**Tasks:**

1. **Create App Router structure**
   ```
   src/
   ├── app/
   │   ├── layout.tsx          # Root layout (Header, Sidebar)
   │   ├── page.tsx            # Home page (from index.tsx)
   │   ├── dashboard/
   │   │   └── page.tsx        # Dashboard page
   │   ├── backtesting/
   │   │   └── page.tsx        # Backtesting page
   │   ├── settings/
   │   │   └── page.tsx        # Settings page
   │   ├── search/
   │   │   └── page.tsx        # Search page
   │   ├── dev-systems/
   │   │   └── page.tsx        # Dev systems page
   │   ├── login/
   │   │   └── page.tsx        # Login page
   │   └── test/
   │       └── page.tsx        # Test page
   ├── components/            # Keep existing structure
   ├── hooks/
   ├── services/
   ├── store/
   └── types/
   ```

2. **Create root layout.tsx**
   ```typescript
   import { Inter } from 'next/font/google';
   import './globals.css';
   import { Providers } from './providers';

   const inter = Inter({ subsets: ['latin'] });

   export const metadata = {
     title: 'AlgoTrendy v2.6',
     description: 'Modern algorithmic trading platform',
   };

   export default function RootLayout({
     children,
   }: {
     children: React.ReactNode;
   }) {
     return (
       <html lang="en">
         <body className={inter.className}>
           <Providers>{children}</Providers>
         </body>
       </html>
     );
   }
   ```

3. **Create providers.tsx for client-side state**
   ```typescript
   'use client';

   import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
   import { useState } from 'react';
   import { ToastContainer } from '@/components/ui/ToastContainer';

   export function Providers({ children }: { children: React.ReactNode }) {
     const [queryClient] = useState(() => new QueryClient({
       defaultOptions: {
         queries: {
           staleTime: 60 * 1000,
           refetchOnWindowFocus: false,
         },
       },
     }));

     return (
       <QueryClientProvider client={queryClient}>
         {children}
         <ToastContainer />
       </QueryClientProvider>
     );
   }
   ```

4. **Migrate pages to app directory**
   - Convert each page from Pages Router to App Router
   - Mark client components with 'use client'
   - Update imports for new structure

5. **Update navigation components**
   - Replace `next/link` usage (now requires no `<a>` child)
   - Update `useRouter` imports (`next/navigation` vs `next/router`)

**Deliverables:**
- Complete App Router structure
- All pages migrated to `app/` directory
- Root layout with providers
- Navigation working correctly

---

### Day 4: App Router Migration - Data Fetching

**Tasks:**

1. **Identify client vs server components**
   - Mark interactive components with 'use client'
   - Keep data fetching in Server Components where possible
   - Document state management strategy

2. **Update data fetching patterns**

   **Before (Pages Router):**
   ```typescript
   export default function Dashboard() {
     const { data } = useQuery(['portfolio'], fetchPortfolio);
     // ...
   }
   ```

   **After (App Router - Server Component):**
   ```typescript
   async function getPortfolio() {
     const res = await fetch('http://localhost:5000/api/portfolio', {
       cache: 'no-store', // or 'force-cache' for static
     });
     return res.json();
   }

   export default async function Dashboard() {
     const portfolio = await getPortfolio();
     return <PortfolioCard data={portfolio} />;
   }
   ```

   **After (App Router - Client Component with React Query):**
   ```typescript
   'use client';

   export default function Dashboard() {
     const { data } = useQuery({
       queryKey: ['portfolio'],
       queryFn: () => apiService.getPortfolio(),
     });
     // ...
   }
   ```

3. **Create loading and error UI**
   ```typescript
   // app/dashboard/loading.tsx
   export default function Loading() {
     return <div>Loading dashboard...</div>;
   }

   // app/dashboard/error.tsx
   'use client';

   export default function Error({ error, reset }: {
     error: Error;
     reset: () => void;
   }) {
     return (
       <div>
         <h2>Something went wrong!</h2>
         <button onClick={reset}>Try again</button>
       </div>
     );
   }
   ```

4. **Update Zustand stores for App Router**
   - Ensure stores work with React 19
   - Add proper SSR handling

**Deliverables:**
- Server Components for static pages
- Client Components for interactive UI
- Loading and error states for all routes
- Updated data fetching patterns

---

### Day 5: C# API Integration - Authentication

**Tasks:**

1. **Create new API service for C# backend**

   **File: `src/services/api-v2.ts`**

   ```typescript
   import axios, { AxiosInstance, AxiosError } from 'axios';

   const API_BASE_URL = process.env.NEXT_PUBLIC_API_URL || 'http://localhost:5000/api';

   interface LoginRequest {
     username: string;
     password: string;
   }

   interface LoginResponse {
     token: string;
     refreshToken: string;
     expiresAt: string;
     user: {
       id: string;
       username: string;
       email: string;
     };
   }

   interface RegisterRequest {
     username: string;
     email: string;
     password: string;
   }

   class ApiServiceV2 {
     private client: AxiosInstance;
     private cachedToken: string | null = null;

     constructor() {
       this.client = axios.create({
         baseURL: API_BASE_URL,
         headers: {
           'Content-Type': 'application/json',
         },
         timeout: 10000,
       });

       // Request interceptor
       this.client.interceptors.request.use((config) => {
         const token = this.getToken();
         if (token) {
           config.headers.Authorization = `Bearer ${token}`;
         }
         return config;
       });

       // Response interceptor
       this.client.interceptors.response.use(
         (response) => response,
         async (error: AxiosError) => {
           if (error.response?.status === 401) {
             // Try refresh token
             const refreshed = await this.refreshToken();
             if (refreshed && error.config) {
               return this.client(error.config);
             }

             // Redirect to login
             this.clearToken();
             if (typeof window !== 'undefined') {
               window.location.href = '/login';
             }
           }
           return Promise.reject(error);
         }
       );
     }

     // Authentication
     async login(username: string, password: string): Promise<LoginResponse> {
       const response = await this.client.post<LoginResponse>('/auth/login', {
         username,
         password,
       });

       this.setToken(response.data.token);
       this.setRefreshToken(response.data.refreshToken);

       return response.data;
     }

     async register(data: RegisterRequest): Promise<LoginResponse> {
       const response = await this.client.post<LoginResponse>('/auth/register', data);

       this.setToken(response.data.token);
       this.setRefreshToken(response.data.refreshToken);

       return response.data;
     }

     async logout(): Promise<void> {
       try {
         await this.client.post('/auth/logout');
       } finally {
         this.clearToken();
         this.clearRefreshToken();
       }
     }

     async refreshToken(): Promise<boolean> {
       try {
         const refreshToken = this.getRefreshToken();
         if (!refreshToken) return false;

         const response = await this.client.post<LoginResponse>('/auth/refresh', {
           refreshToken,
         });

         this.setToken(response.data.token);
         this.setRefreshToken(response.data.refreshToken);

         return true;
       } catch {
         return false;
       }
     }

     async getCurrentUser() {
       const response = await this.client.get('/auth/me');
       return response.data;
     }

     // Token management
     private getToken(): string | null {
       if (typeof window === 'undefined') return null;
       if (this.cachedToken) return this.cachedToken;
       this.cachedToken = localStorage.getItem('auth_token');
       return this.cachedToken;
     }

     private setToken(token: string): void {
       this.cachedToken = token;
       if (typeof window !== 'undefined') {
         localStorage.setItem('auth_token', token);
       }
     }

     private clearToken(): void {
       this.cachedToken = null;
       if (typeof window !== 'undefined') {
         localStorage.removeItem('auth_token');
       }
     }

     private getRefreshToken(): string | null {
       if (typeof window === 'undefined') return null;
       return localStorage.getItem('refresh_token');
     }

     private setRefreshToken(token: string): void {
       if (typeof window !== 'undefined') {
         localStorage.setItem('refresh_token', token);
       }
     }

     private clearRefreshToken(): void {
       if (typeof window !== 'undefined') {
         localStorage.removeItem('refresh_token');
       }
     }
   }

   export const apiService = new ApiServiceV2();
   ```

2. **Update auth store for C# JWT**

   ```typescript
   import { create } from 'zustand';
   import { persist } from 'zustand/middleware';

   interface User {
     id: string;
     username: string;
     email: string;
   }

   interface AuthState {
     user: User | null;
     token: string | null;
     isAuthenticated: boolean;
     login: (username: string, password: string) => Promise<void>;
     logout: () => void;
     refreshUser: () => Promise<void>;
   }

   export const useAuthStore = create<AuthState>()(
     persist(
       (set, get) => ({
         user: null,
         token: null,
         isAuthenticated: false,

         login: async (username, password) => {
           const response = await apiService.login(username, password);
           set({
             user: response.user,
             token: response.token,
             isAuthenticated: true,
           });
         },

         logout: () => {
           apiService.logout();
           set({ user: null, token: null, isAuthenticated: false });
         },

         refreshUser: async () => {
           try {
             const user = await apiService.getCurrentUser();
             set({ user });
           } catch (error) {
             get().logout();
           }
         },
       }),
       {
         name: 'auth-storage',
         partialize: (state) => ({
           user: state.user,
           token: state.token,
           isAuthenticated: state.isAuthenticated,
         }),
       }
     )
   );
   ```

3. **Update login page**
   ```typescript
   'use client';

   import { useAuthStore } from '@/store/authStore';
   import { useRouter } from 'next/navigation';
   import { useState } from 'react';

   export default function LoginPage() {
     const router = useRouter();
     const login = useAuthStore((state) => state.login);
     const [username, setUsername] = useState('');
     const [password, setPassword] = useState('');
     const [error, setError] = useState('');

     const handleSubmit = async (e: React.FormEvent) => {
       e.preventDefault();
       try {
         await login(username, password);
         router.push('/dashboard');
       } catch (err) {
         setError('Invalid credentials');
       }
     };

     return (
       <form onSubmit={handleSubmit}>
         {/* Form UI */}
       </form>
     );
   }
   ```

4. **Create auth middleware**
   ```typescript
   // middleware.ts (root level)
   import { NextResponse } from 'next/server';
   import type { NextRequest } from 'next/server';

   export function middleware(request: NextRequest) {
     const token = request.cookies.get('auth_token')?.value;
     const isLoginPage = request.nextUrl.pathname === '/login';

     if (!token && !isLoginPage) {
       return NextResponse.redirect(new URL('/login', request.url));
     }

     if (token && isLoginPage) {
       return NextResponse.redirect(new URL('/dashboard', request.url));
     }

     return NextResponse.next();
   }

   export const config = {
     matcher: ['/dashboard/:path*', '/settings/:path*', '/backtesting/:path*'],
   };
   ```

**Deliverables:**
- C# API service with JWT authentication
- Updated auth store with token refresh
- Login/logout flow working
- Auth middleware protecting routes

---

### Day 6: C# API Integration - Trading Endpoints

**Tasks:**

1. **Map Python endpoints to C# controllers**

   | v2.5 Python Endpoint | v2.6 C# Endpoint | Controller |
   |---------------------|------------------|------------|
   | `/api/portfolio` | `/api/portfolio` | PortfolioController |
   | `/api/portfolio/positions` | `/api/positions` | PositionController |
   | `/api/strategies` | `/api/strategies` | StrategyController |
   | `/api/trading/buy` | `/api/trading/buy` | TradingController |
   | `/api/trading/sell` | `/api/trading/sell` | TradingController |
   | `/api/freqtrade/bots` | `/api/bots` | BotController |

2. **Update API service with trading methods**

   ```typescript
   // Add to ApiServiceV2 class

   // Portfolio
   async getPortfolio() {
     const response = await this.client.get('/portfolio');
     return response.data;
   }

   async getPositions() {
     const response = await this.client.get('/positions');
     return response.data;
   }

   // Strategies
   async getStrategies() {
     const response = await this.client.get('/strategies');
     return response.data;
   }

   async getStrategy(id: string) {
     const response = await this.client.get(`/strategies/${id}`);
     return response.data;
   }

   async createStrategy(data: CreateStrategyRequest) {
     const response = await this.client.post('/strategies', data);
     return response.data;
   }

   async updateStrategy(id: string, data: UpdateStrategyRequest) {
     const response = await this.client.put(`/strategies/${id}`, data);
     return response.data;
   }

   async deleteStrategy(id: string) {
     await this.client.delete(`/strategies/${id}`);
   }

   // Trading
   async placeBuyOrder(data: PlaceBuyOrderRequest) {
     const response = await this.client.post('/trading/buy', data);
     return response.data;
   }

   async placeSellOrder(data: PlaceSellOrderRequest) {
     const response = await this.client.post('/trading/sell', data);
     return response.data;
   }

   async closePosition(positionId: string) {
     await this.client.delete(`/positions/${positionId}`);
   }

   // Bots
   async getBots() {
     const response = await this.client.get('/bots');
     return response.data;
   }

   async getBotStatus(botId: string) {
     const response = await this.client.get(`/bots/${botId}/status`);
     return response.data;
   }

   async startBot(botId: string) {
     await this.client.post(`/bots/${botId}/start`);
   }

   async stopBot(botId: string) {
     await this.client.post(`/bots/${botId}/stop`);
   }
   ```

3. **Create TypeScript types for C# DTOs**

   ```typescript
   // src/types/api.ts

   export interface Portfolio {
     totalValue: number;
     cash: number;
     positions: Position[];
     totalPnl: number;
     totalPnlPercent: number;
   }

   export interface Position {
     id: string;
     symbol: string;
     quantity: number;
     entryPrice: number;
     currentPrice: number;
     pnl: number;
     pnlPercent: number;
     side: 'Long' | 'Short';
     openedAt: string;
   }

   export interface Strategy {
     id: string;
     name: string;
     description: string;
     isActive: boolean;
     parameters: Record<string, any>;
     createdAt: string;
     updatedAt: string;
   }

   export interface CreateStrategyRequest {
     name: string;
     description: string;
     parameters: Record<string, any>;
   }

   export interface PlaceBuyOrderRequest {
     symbol: string;
     quantity: number;
     strategyId?: string;
     orderType: 'Market' | 'Limit';
     limitPrice?: number;
   }

   export interface PlaceSellOrderRequest {
     positionId: string;
     quantity: number;
     orderType: 'Market' | 'Limit';
     limitPrice?: number;
   }

   export interface Bot {
     id: string;
     name: string;
     status: 'Running' | 'Stopped' | 'Error';
     strategy: Strategy;
     performance: {
       totalTrades: number;
       winRate: number;
       totalPnl: number;
     };
   }
   ```

4. **Update dashboard components to use C# API**

   ```typescript
   'use client';

   import { useQuery } from '@tanstack/react-query';
   import { apiService } from '@/services/api-v2';
   import { PortfolioCard } from '@/components/dashboard/PortfolioCard';
   import { PositionsTable } from '@/components/dashboard/PositionsTable';

   export default function DashboardPage() {
     const { data: portfolio, isLoading } = useQuery({
       queryKey: ['portfolio'],
       queryFn: () => apiService.getPortfolio(),
       refetchInterval: 5000, // Refresh every 5 seconds
     });

     const { data: positions } = useQuery({
       queryKey: ['positions'],
       queryFn: () => apiService.getPositions(),
       refetchInterval: 5000,
     });

     if (isLoading) return <div>Loading...</div>;

     return (
       <div className="space-y-6">
         <PortfolioCard portfolio={portfolio} />
         <PositionsTable positions={positions} />
       </div>
     );
   }
   ```

**Deliverables:**
- Complete API service with all trading endpoints
- TypeScript types matching C# DTOs
- Dashboard fetching data from C# backend
- All CRUD operations working

---

### Day 7: C# API Integration - Backtesting & Search

**Tasks:**

1. **Add backtesting endpoints**

   ```typescript
   // Add to ApiServiceV2

   interface BacktestRequest {
     strategyId: string;
     symbol: string;
     startDate: string;
     endDate: string;
     initialCapital: number;
   }

   interface BacktestResult {
     id: string;
     strategyId: string;
     symbol: string;
     startDate: string;
     endDate: string;
     initialCapital: number;
     finalCapital: number;
     totalReturn: number;
     totalReturnPercent: number;
     sharpeRatio: number;
     maxDrawdown: number;
     trades: BacktestTrade[];
   }

   async runBacktest(data: BacktestRequest): Promise<BacktestResult> {
     const response = await this.client.post('/backtest/run', data);
     return response.data;
   }

   async getBacktestResults(strategyId?: string) {
     const params = strategyId ? { strategyId } : {};
     const response = await this.client.get('/backtest/results', { params });
     return response.data;
   }

   async getBacktestResult(id: string) {
     const response = await this.client.get(`/backtest/results/${id}`);
     return response.data;
   }
   ```

2. **Update backtesting page**

   ```typescript
   'use client';

   import { useState } from 'react';
   import { useMutation, useQuery } from '@tanstack/react-query';
   import { apiService } from '@/services/api-v2';

   export default function BacktestingPage() {
     const [config, setConfig] = useState<BacktestRequest>({
       strategyId: '',
       symbol: 'BTCUSDT',
       startDate: '',
       endDate: '',
       initialCapital: 10000,
     });

     const { data: strategies } = useQuery({
       queryKey: ['strategies'],
       queryFn: () => apiService.getStrategies(),
     });

     const runBacktest = useMutation({
       mutationFn: (data: BacktestRequest) => apiService.runBacktest(data),
       onSuccess: (result) => {
         console.log('Backtest completed:', result);
       },
     });

     const handleSubmit = () => {
       runBacktest.mutate(config);
     };

     return (
       <div>
         {/* Backtest form and results UI */}
       </div>
     );
   }
   ```

3. **Integrate search (if Algolia is kept)**
   - Keep Algolia search component as-is
   - Or replace with C# search endpoint if available

4. **Add settings endpoints**

   ```typescript
   async getUserSettings() {
     const response = await this.client.get('/settings');
     return response.data;
   }

   async updateUserSettings(settings: UserSettings) {
     const response = await this.client.put('/settings', settings);
     return response.data;
   }

   async getBrokerConnections() {
     const response = await this.client.get('/settings/brokers');
     return response.data;
   }

   async addBrokerConnection(data: BrokerConnectionRequest) {
     const response = await this.client.post('/settings/brokers', data);
     return response.data;
   }
   ```

**Deliverables:**
- Backtesting page integrated with C# API
- Settings page with broker configuration
- Search functionality working
- All v2.5 pages migrated to C# backend

---

### Day 8: SignalR Integration - Real-time Trading Updates

**Tasks:**

1. **Install SignalR client**
   ```bash
   npm install @microsoft/signalr
   ```

2. **Create SignalR service**

   **File: `src/services/signalr.ts`**

   ```typescript
   import * as signalR from '@microsoft/signalr';

   type MessageHandler = (data: any) => void;
   type ConnectionStatusCallback = (status: 'Connected' | 'Disconnected' | 'Reconnecting') => void;

   interface SignalRConfig {
     hubUrl: string;
     accessTokenFactory?: () => string | Promise<string>;
   }

   export class SignalRService {
     private connection: signalR.HubConnection | null = null;
     private config: SignalRConfig;
     private messageHandlers: Map<string, Set<MessageHandler>> = new Map();
     private statusCallbacks: Set<ConnectionStatusCallback> = new Set();

     constructor(config: SignalRConfig) {
       this.config = config;
     }

     /**
      * Start SignalR connection
      */
     async start(): Promise<void> {
       if (this.connection) {
         console.warn('[SignalR] Already connected');
         return;
       }

       this.connection = new signalR.HubConnectionBuilder()
         .withUrl(this.config.hubUrl, {
           accessTokenFactory: this.config.accessTokenFactory,
           skipNegotiation: false,
           transport: signalR.HttpTransportType.WebSockets | signalR.HttpTransportType.LongPolling,
         })
         .withAutomaticReconnect({
           nextRetryDelayInMilliseconds: (retryContext) => {
             // Exponential backoff: 0s, 2s, 10s, 30s, then 30s
             if (retryContext.previousRetryCount === 0) return 0;
             if (retryContext.previousRetryCount === 1) return 2000;
             if (retryContext.previousRetryCount === 2) return 10000;
             return 30000;
           },
         })
         .configureLogging(signalR.LogLevel.Information)
         .build();

       // Connection events
       this.connection.onreconnecting(() => {
         console.log('[SignalR] Reconnecting...');
         this.notifyStatusChange('Reconnecting');
       });

       this.connection.onreconnected(() => {
         console.log('[SignalR] Reconnected');
         this.notifyStatusChange('Connected');
         this.resubscribeAll();
       });

       this.connection.onclose((error) => {
         console.log('[SignalR] Disconnected', error);
         this.notifyStatusChange('Disconnected');
       });

       // Register all message handlers
       this.registerHandlers();

       try {
         await this.connection.start();
         console.log('[SignalR] Connected');
         this.notifyStatusChange('Connected');
       } catch (error) {
         console.error('[SignalR] Connection failed:', error);
         throw error;
       }
     }

     /**
      * Stop SignalR connection
      */
     async stop(): Promise<void> {
       if (this.connection) {
         await this.connection.stop();
         this.connection = null;
         this.notifyStatusChange('Disconnected');
       }
     }

     /**
      * Subscribe to a SignalR method
      */
     on(methodName: string, handler: MessageHandler): () => void {
       if (!this.messageHandlers.has(methodName)) {
         this.messageHandlers.set(methodName, new Set());
       }

       this.messageHandlers.get(methodName)!.add(handler);

       // If already connected, register handler immediately
       if (this.connection) {
         this.connection.on(methodName, handler);
       }

       // Return unsubscribe function
       return () => this.off(methodName, handler);
     }

     /**
      * Unsubscribe from a SignalR method
      */
     off(methodName: string, handler: MessageHandler): void {
       const handlers = this.messageHandlers.get(methodName);
       if (handlers) {
         handlers.delete(handler);
         if (handlers.size === 0) {
           this.messageHandlers.delete(methodName);
         }
       }

       if (this.connection) {
         this.connection.off(methodName, handler);
       }
     }

     /**
      * Invoke a server method
      */
     async invoke(methodName: string, ...args: any[]): Promise<any> {
       if (!this.connection) {
         throw new Error('[SignalR] Not connected');
       }
       return this.connection.invoke(methodName, ...args);
     }

     /**
      * Subscribe to connection status changes
      */
     onStatusChange(callback: ConnectionStatusCallback): () => void {
       this.statusCallbacks.add(callback);
       return () => this.statusCallbacks.delete(callback);
     }

     /**
      * Check if connected
      */
     isConnected(): boolean {
       return this.connection?.state === signalR.HubConnectionState.Connected;
     }

     // Private methods

     private registerHandlers(): void {
       if (!this.connection) return;

       this.messageHandlers.forEach((handlers, methodName) => {
         handlers.forEach((handler) => {
           this.connection!.on(methodName, handler);
         });
       });
     }

     private resubscribeAll(): void {
       // Re-register all handlers after reconnection
       this.registerHandlers();
     }

     private notifyStatusChange(status: 'Connected' | 'Disconnected' | 'Reconnecting'): void {
       this.statusCallbacks.forEach((callback) => callback(status));
     }
   }

   // Singleton instance
   let signalRInstance: SignalRService | null = null;

   export const getSignalRService = (): SignalRService => {
     if (!signalRInstance) {
       const hubUrl = process.env.NEXT_PUBLIC_SIGNALR_HUB_URL || 'http://localhost:5000/hubs/trading';

       signalRInstance = new SignalRService({
         hubUrl,
         accessTokenFactory: () => {
           // Get token from localStorage
           if (typeof window !== 'undefined') {
             return localStorage.getItem('auth_token') || '';
           }
           return '';
         },
       });
     }
     return signalRInstance;
   };
   ```

3. **Create SignalR hook for React**

   ```typescript
   'use client';

   import { useEffect, useState } from 'react';
   import { getSignalRService } from '@/services/signalr';

   export function useSignalR() {
     const [isConnected, setIsConnected] = useState(false);
     const [status, setStatus] = useState<'Connected' | 'Disconnected' | 'Reconnecting'>('Disconnected');

     useEffect(() => {
       const signalR = getSignalRService();

       // Subscribe to status changes
       const unsubscribeStatus = signalR.onStatusChange((newStatus) => {
         setStatus(newStatus);
         setIsConnected(newStatus === 'Connected');
       });

       // Start connection
       signalR.start().catch((error) => {
         console.error('Failed to start SignalR:', error);
       });

       // Cleanup
       return () => {
         unsubscribeStatus();
         signalR.stop();
       };
     }, []);

     return { isConnected, status, signalR: getSignalRService() };
   }

   // Hook for specific event subscriptions
   export function useSignalREvent<T>(
     eventName: string,
     handler: (data: T) => void,
     dependencies: any[] = []
   ) {
     const { signalR } = useSignalR();

     useEffect(() => {
       const unsubscribe = signalR.on(eventName, handler);
       return unsubscribe;
     }, [eventName, signalR, ...dependencies]);
   }
   ```

4. **Update components to use SignalR**

   **Example: Live Price Ticker**

   ```typescript
   'use client';

   import { useState } from 'react';
   import { useSignalREvent } from '@/hooks/useSignalR';

   interface PriceUpdate {
     symbol: string;
     price: number;
     change: number;
     changePercent: number;
     timestamp: string;
   }

   export function LivePriceTicker({ symbol }: { symbol: string }) {
     const [price, setPrice] = useState<PriceUpdate | null>(null);

     useSignalREvent<PriceUpdate>(
       'PriceUpdate',
       (data) => {
         if (data.symbol === symbol) {
           setPrice(data);
         }
       },
       [symbol]
     );

     if (!price) return <div>Loading price...</div>;

     return (
       <div className="flex items-center gap-2">
         <span className="font-bold">{price.symbol}</span>
         <span className="text-2xl">${price.price.toFixed(2)}</span>
         <span className={price.change >= 0 ? 'text-green-500' : 'text-red-500'}>
           {price.change >= 0 ? '+' : ''}{price.changePercent.toFixed(2)}%
         </span>
       </div>
     );
   }
   ```

   **Example: Position Updates**

   ```typescript
   'use client';

   import { useState, useEffect } from 'react';
   import { useSignalREvent } from '@/hooks/useSignalR';
   import { useQuery, useQueryClient } from '@tanstack/react-query';
   import { apiService } from '@/services/api-v2';

   export function PositionsTable() {
     const queryClient = useQueryClient();

     const { data: positions } = useQuery({
       queryKey: ['positions'],
       queryFn: () => apiService.getPositions(),
     });

     // Listen for position updates via SignalR
     useSignalREvent('PositionUpdate', (updatedPosition) => {
       // Invalidate and refetch positions
       queryClient.invalidateQueries({ queryKey: ['positions'] });
     });

     // Listen for new positions
     useSignalREvent('PositionOpened', (newPosition) => {
       queryClient.invalidateQueries({ queryKey: ['positions'] });
     });

     // Listen for closed positions
     useSignalREvent('PositionClosed', (closedPosition) => {
       queryClient.invalidateQueries({ queryKey: ['positions'] });
     });

     return (
       <table>
         {/* Render positions */}
       </table>
     );
   }
   ```

**Deliverables:**
- SignalR service with auto-reconnect
- React hooks for SignalR integration
- Real-time price updates
- Real-time position updates
- Connection status indicator

---

### Day 9: SignalR Integration - Bot Status & Notifications

**Tasks:**

1. **Add bot status updates via SignalR**

   ```typescript
   'use client';

   import { useSignalREvent } from '@/hooks/useSignalR';
   import { useQuery, useQueryClient } from '@tanstack/react-query';

   interface BotStatusUpdate {
     botId: string;
     status: 'Running' | 'Stopped' | 'Error';
     message?: string;
     timestamp: string;
   }

   export function BotControlPanel() {
     const queryClient = useQueryClient();

     const { data: bots } = useQuery({
       queryKey: ['bots'],
       queryFn: () => apiService.getBots(),
     });

     // Listen for bot status updates
     useSignalREvent<BotStatusUpdate>('BotStatusUpdate', (update) => {
       queryClient.setQueryData(['bots'], (old: any) => {
         return old?.map((bot: any) =>
           bot.id === update.botId
             ? { ...bot, status: update.status }
             : bot
         );
       });
     });

     return (
       <div>
         {bots?.map((bot) => (
           <BotCard key={bot.id} bot={bot} />
         ))}
       </div>
     );
   }
   ```

2. **Add trade notifications**

   ```typescript
   'use client';

   import { useSignalREvent } from '@/hooks/useSignalR';
   import { useToast } from '@/hooks/useToast';

   interface TradeNotification {
     type: 'BuyOrder' | 'SellOrder';
     symbol: string;
     quantity: number;
     price: number;
     timestamp: string;
   }

   export function TradeNotifications() {
     const { showToast } = useToast();

     useSignalREvent<TradeNotification>('TradeExecuted', (trade) => {
       showToast({
         type: 'success',
         title: `${trade.type} Executed`,
         message: `${trade.quantity} ${trade.symbol} @ $${trade.price}`,
       });
     });

     return null; // This is a notification-only component
   }
   ```

3. **Add system alerts**

   ```typescript
   interface SystemAlert {
     severity: 'Info' | 'Warning' | 'Error';
     title: string;
     message: string;
     timestamp: string;
   }

   export function SystemAlerts() {
     const { showToast } = useToast();

     useSignalREvent<SystemAlert>('SystemAlert', (alert) => {
       showToast({
         type: alert.severity.toLowerCase() as 'info' | 'warning' | 'error',
         title: alert.title,
         message: alert.message,
       });
     });

     return null;
   }
   ```

4. **Update layout with SignalR provider**

   ```typescript
   'use client';

   import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
   import { useState, useEffect } from 'react';
   import { getSignalRService } from '@/services/signalr';
   import { ToastContainer } from '@/components/ui/ToastContainer';
   import { WebSocketStatus } from '@/components/ui/WebSocketStatus';
   import { TradeNotifications } from '@/components/notifications/TradeNotifications';
   import { SystemAlerts } from '@/components/notifications/SystemAlerts';

   export function Providers({ children }: { children: React.ReactNode }) {
     const [queryClient] = useState(() => new QueryClient());

     useEffect(() => {
       // Initialize SignalR on app load
       const signalR = getSignalRService();
       signalR.start().catch((error) => {
         console.error('Failed to start SignalR:', error);
       });

       return () => {
         signalR.stop();
       };
     }, []);

     return (
       <QueryClientProvider client={queryClient}>
         {children}
         <WebSocketStatus /> {/* Shows SignalR connection status */}
         <TradeNotifications />
         <SystemAlerts />
         <ToastContainer />
       </QueryClientProvider>
     );
   }
   ```

5. **Update WebSocketStatus component for SignalR**

   ```typescript
   'use client';

   import { useSignalR } from '@/hooks/useSignalR';

   export function WebSocketStatus() {
     const { status, isConnected } = useSignalR();

     return (
       <div className="fixed bottom-4 right-4 flex items-center gap-2 px-4 py-2 bg-white dark:bg-gray-800 rounded-lg shadow-lg">
         <div
           className={`w-3 h-3 rounded-full ${
             isConnected ? 'bg-green-500' : status === 'Reconnecting' ? 'bg-yellow-500' : 'bg-red-500'
           }`}
         />
         <span className="text-sm font-medium">
           {status === 'Connected' ? 'Live' : status === 'Reconnecting' ? 'Reconnecting...' : 'Disconnected'}
         </span>
       </div>
     );
   }
   ```

**Deliverables:**
- Real-time bot status updates
- Trade execution notifications
- System alert notifications
- SignalR connection status UI
- Complete real-time integration

---

### Day 10: Testing, Validation & Acceptance

**Tasks:**

1. **Comprehensive testing checklist**

   **Authentication:**
   - [ ] Login with valid credentials
   - [ ] Login with invalid credentials shows error
   - [ ] Logout clears token and redirects
   - [ ] Token refresh on 401 response
   - [ ] Protected routes redirect to login
   - [ ] Auth persists on page reload

   **API Integration:**
   - [ ] Portfolio data loads correctly
   - [ ] Positions display in real-time
   - [ ] Strategies CRUD operations work
   - [ ] Buy/sell orders execute successfully
   - [ ] Backtesting runs and shows results
   - [ ] Bot controls (start/stop) function
   - [ ] Settings save and persist

   **SignalR Real-time:**
   - [ ] SignalR connects on app load
   - [ ] Price updates stream in real-time
   - [ ] Position updates reflect immediately
   - [ ] Bot status changes show instantly
   - [ ] Trade notifications appear on execution
   - [ ] Connection status indicator works
   - [ ] Auto-reconnect after disconnect

   **UI/UX:**
   - [ ] All pages render without errors
   - [ ] Navigation between pages works
   - [ ] Loading states show during API calls
   - [ ] Error states display meaningful messages
   - [ ] Responsive design on mobile/tablet
   - [ ] Dark mode toggle works (if implemented)
   - [ ] Toast notifications appear correctly

   **Performance:**
   - [ ] Initial page load < 3 seconds
   - [ ] API responses < 500ms
   - [ ] SignalR latency < 100ms
   - [ ] No memory leaks during extended use
   - [ ] Smooth scrolling and interactions

2. **Fix any bugs discovered during testing**

3. **Update documentation**
   - Create frontend README.md
   - Document environment variables
   - Add component usage examples
   - Document SignalR events

4. **Create deployment scripts**

   ```bash
   # scripts/build-frontend.sh
   #!/bin/bash

   cd frontend
   npm ci
   npm run build
   npm run start
   ```

5. **Final code review**
   - Remove console.logs
   - Clean up commented code
   - Ensure TypeScript strict mode passes
   - Run linter and fix warnings

**Deliverables:**
- All tests passing
- Bug-free frontend application
- Complete documentation
- Deployment scripts
- Production-ready build

---

## Component Migration Reference

### Layout Components

| Component | Status | Migration Notes |
|-----------|--------|----------------|
| Header | Copy as-is | Update navigation links for App Router |
| Sidebar | Copy as-is | Update routing with next/navigation |

### Dashboard Components

| Component | Status | Migration Notes |
|-----------|--------|----------------|
| BotControlPanel | Migrate | Add SignalR for real-time bot status |
| PositionsTable | Migrate | Replace WebSocket with SignalR position updates |
| BotPerformanceChart | Migrate | Update API calls to C# endpoints |
| PerformanceChart | Copy as-is | Works with new data structure |
| LivePriceTicker | Migrate | Replace WebSocket with SignalR price feed |
| PortfolioCard | Migrate | Update API call to C# /api/portfolio |

### UI Components

| Component | Status | Migration Notes |
|-----------|--------|----------------|
| Toast | Copy as-is | No changes needed |
| ToastContainer | Copy as-is | No changes needed |
| WebSocketStatus | Migrate | Update for SignalR connection status |
| PasswordStrength | Copy as-is | No changes needed |
| EmptyState | Copy as-is | No changes needed |

### Search Components

| Component | Status | Migration Notes |
|-----------|--------|----------------|
| EnhancedAlgoliaSearch | Copy as-is | Keep Algolia integration or replace with C# search |

---

## API Endpoint Mapping

### v2.5 Python → v2.6 C#

| v2.5 Endpoint | v2.6 Endpoint | Method | Controller | Notes |
|--------------|--------------|--------|-----------|-------|
| `/api/auth/login` | `/api/auth/login` | POST | AuthController | Update request/response format |
| `/api/auth/register` | `/api/auth/register` | POST | AuthController | Add username field |
| `/api/auth/me` | `/api/auth/me` | GET | AuthController | JWT required |
| `/api/portfolio` | `/api/portfolio` | GET | PortfolioController | New format |
| `/api/portfolio/positions` | `/api/positions` | GET | PositionController | Simplified path |
| `/api/strategies` | `/api/strategies` | GET | StrategyController | No changes |
| `/api/strategies/{id}` | `/api/strategies/{id}` | GET/PUT/DELETE | StrategyController | No changes |
| `/api/trading/buy` | `/api/trading/buy` | POST | TradingController | New DTO format |
| `/api/trading/sell` | `/api/trading/sell` | POST | TradingController | New DTO format |
| `/api/freqtrade/bots` | `/api/bots` | GET | BotController | Simplified path |
| `/api/backtest/run` | `/api/backtest/run` | POST | BacktestController | New |
| `/api/backtest/results` | `/api/backtest/results` | GET | BacktestController | New |

---

## SignalR Events Reference

### Client → Server (Invoke)

| Method | Parameters | Description |
|--------|-----------|-------------|
| `SubscribeToPriceUpdates` | `string[] symbols` | Subscribe to price updates for symbols |
| `UnsubscribeFromPriceUpdates` | `string[] symbols` | Unsubscribe from price updates |
| `SubscribeToPositionUpdates` | - | Subscribe to all position updates |
| `SubscribeToBotUpdates` | `string botId` | Subscribe to specific bot status |

### Server → Client (On)

| Event | Payload | Description |
|-------|---------|-------------|
| `PriceUpdate` | `{ symbol, price, change, changePercent, timestamp }` | Real-time price update |
| `PositionUpdate` | `Position` | Existing position price/PnL update |
| `PositionOpened` | `Position` | New position opened |
| `PositionClosed` | `{ positionId, closedAt, finalPnl }` | Position closed |
| `BotStatusUpdate` | `{ botId, status, message, timestamp }` | Bot status changed |
| `TradeExecuted` | `{ type, symbol, quantity, price, timestamp }` | Trade completed |
| `SystemAlert` | `{ severity, title, message, timestamp }` | System notification |

---

## Testing Checklist

### Authentication Flow

- [ ] User can register with valid credentials
- [ ] User can login with registered credentials
- [ ] Invalid login shows error message
- [ ] JWT token stored in localStorage
- [ ] Token sent in Authorization header
- [ ] Protected routes redirect to /login when unauthenticated
- [ ] Authenticated users redirected from /login to /dashboard
- [ ] Token refresh works on 401 response
- [ ] Logout clears token and redirects to login
- [ ] Auth state persists across page reloads

### Dashboard Page

- [ ] Portfolio card displays total value, cash, PnL
- [ ] Positions table shows all open positions
- [ ] Real-time price updates via SignalR
- [ ] Position PnL updates in real-time
- [ ] Bot control panel shows all bots
- [ ] Start/stop bot buttons work
- [ ] Bot status updates in real-time
- [ ] Performance charts render correctly
- [ ] Live price ticker shows current prices

### Backtesting Page

- [ ] Strategy dropdown populated from API
- [ ] Date pickers work correctly
- [ ] Backtest runs successfully
- [ ] Results display with charts
- [ ] Trade history table shows all trades
- [ ] Performance metrics calculated correctly
- [ ] Can save backtest results
- [ ] Can view historical backtest results

### Settings Page

- [ ] User profile displays current info
- [ ] Can update user settings
- [ ] Broker connections list displays
- [ ] Can add new broker connection
- [ ] Can edit existing broker connection
- [ ] Can delete broker connection
- [ ] API keys are masked in UI
- [ ] Settings persist after save

### Search Page

- [ ] Algolia search input works
- [ ] Search results display correctly
- [ ] Can click result to view details
- [ ] Filters work (if implemented)
- [ ] Search history tracked (if implemented)

### Real-time Features

- [ ] SignalR connection established on load
- [ ] Connection status indicator accurate
- [ ] Auto-reconnect after network loss
- [ ] Price updates stream without lag
- [ ] Position updates reflect immediately
- [ ] Trade notifications appear on execution
- [ ] Bot status changes show instantly
- [ ] System alerts display correctly

### Performance

- [ ] Initial page load < 3 seconds
- [ ] API responses < 500ms average
- [ ] SignalR message latency < 100ms
- [ ] No memory leaks after 30 min use
- [ ] Smooth scrolling and animations
- [ ] Build size < 500KB (gzipped)

### Browser Compatibility

- [ ] Chrome (latest)
- [ ] Firefox (latest)
- [ ] Safari (latest)
- [ ] Edge (latest)
- [ ] Mobile Safari (iOS)
- [ ] Chrome Mobile (Android)

### Responsive Design

- [ ] Desktop (1920x1080)
- [ ] Laptop (1366x768)
- [ ] Tablet (768x1024)
- [ ] Mobile (375x667)
- [ ] Mobile landscape

---

## Acceptance Criteria

### Critical (Must Have)

1. **Authentication**
   - Users can register, login, and logout
   - JWT tokens from C# AuthController work correctly
   - Protected routes enforce authentication
   - Token refresh prevents unnecessary logouts

2. **Dashboard Functionality**
   - Portfolio and positions display correctly
   - Real-time updates via SignalR
   - All data from C# backend API
   - No Python API dependencies

3. **Trading Operations**
   - Can place buy/sell orders
   - Orders execute through C# TradingController
   - Positions update in real-time
   - Error handling for failed orders

4. **Bot Management**
   - Can view all bots
   - Can start/stop bots
   - Bot status updates in real-time
   - Bot performance metrics display

5. **Next.js 15 Compliance**
   - App Router structure
   - Server/Client components properly separated
   - No Pages Router remnants
   - Build completes without errors

### Important (Should Have)

6. **Backtesting**
   - Can configure and run backtests
   - Results display with charts
   - Historical results accessible

7. **Settings Management**
   - User can update profile
   - Broker connections configurable
   - Settings persist correctly

8. **Real-time Performance**
   - SignalR latency < 100ms
   - Auto-reconnect works reliably
   - No dropped messages

9. **UI/UX Polish**
   - Loading states for all async operations
   - Error messages are user-friendly
   - Toast notifications work correctly
   - Responsive on all screen sizes

### Nice to Have

10. **Search Functionality**
    - Algolia search works (or C# alternative)
    - Fast and accurate results

11. **Advanced Features**
    - Dark mode support
    - Keyboard shortcuts
    - Export data functionality
    - Advanced charting options

---

## Risk Mitigation

### High Risk Areas

1. **SignalR Integration Complexity**
   - **Risk:** SignalR has different API than Socket.IO
   - **Mitigation:** Create abstraction layer, thorough testing, fallback to polling

2. **Next.js 15 Breaking Changes**
   - **Risk:** App Router patterns differ from Pages Router
   - **Mitigation:** Follow official migration guide, incremental migration, extensive testing

3. **API Schema Mismatches**
   - **Risk:** C# DTOs differ from Python responses
   - **Mitigation:** Generate TypeScript types from C# models, integration tests

4. **Real-time Performance**
   - **Risk:** SignalR might have higher latency than WebSocket
   - **Mitigation:** Performance benchmarks, optimize message payloads, use binary protocol

### Medium Risk Areas

5. **State Management Complexity**
   - **Risk:** Zustand stores might need updates for App Router
   - **Mitigation:** Test SSR compatibility, use React Query for server state

6. **Authentication Token Handling**
   - **Risk:** Token refresh logic might not work with C# backend
   - **Mitigation:** Implement proper refresh flow, test edge cases

7. **Third-party Dependencies**
   - **Risk:** Some npm packages might not support React 19
   - **Mitigation:** Check compatibility, find alternatives, pin versions

---

## Success Metrics

### Functional Metrics

- **API Integration:** 100% of Python endpoints migrated to C# equivalents
- **Component Migration:** 100% of v2.5 components working in v2.6
- **Test Coverage:** All critical paths tested and passing
- **Build Success:** Clean build with 0 errors, <10 warnings

### Performance Metrics

- **Load Time:** Initial page load <3 seconds on 4G connection
- **API Latency:** Average API response time <500ms
- **SignalR Latency:** Real-time updates delivered in <100ms
- **Bundle Size:** Production build <500KB gzipped

### Quality Metrics

- **Bug Count:** <5 known bugs at launch
- **TypeScript Coverage:** 100% of files type-checked
- **Accessibility:** WCAG 2.1 AA compliance
- **Browser Support:** Works on all major browsers (Chrome, Firefox, Safari, Edge)

---

## Post-Migration Tasks

### Immediate (Days 11-12)

1. **Performance Optimization**
   - Implement code splitting
   - Optimize images and assets
   - Enable compression
   - Add CDN for static assets

2. **Monitoring Setup**
   - Add error tracking (Sentry)
   - Add analytics (PostHog/Mixpanel)
   - Set up performance monitoring
   - Create alerting for API errors

3. **Documentation**
   - Write deployment guide
   - Create developer onboarding docs
   - Document API integration patterns
   - Create troubleshooting guide

### Short-term (Weeks 3-4)

4. **Advanced Features**
   - Implement dark mode
   - Add keyboard shortcuts
   - Enhance error handling
   - Add data export functionality

5. **Testing Enhancement**
   - Add E2E tests (Playwright/Cypress)
   - Add visual regression tests
   - Implement load testing
   - Add security testing

6. **DevOps**
   - Set up CI/CD pipeline
   - Automate deployments
   - Add staging environment
   - Implement feature flags

---

## Code Examples

### API Service Example (Complete)

```typescript
// src/services/api-v2.ts

import axios, { AxiosInstance, AxiosError } from 'axios';

const API_BASE_URL = process.env.NEXT_PUBLIC_API_URL || 'http://localhost:5000/api';

class ApiServiceV2 {
  private client: AxiosInstance;
  private cachedToken: string | null = null;

  constructor() {
    this.client = axios.create({
      baseURL: API_BASE_URL,
      headers: {
        'Content-Type': 'application/json',
      },
      timeout: 10000,
    });

    this.client.interceptors.request.use((config) => {
      const token = this.getToken();
      if (token) {
        config.headers.Authorization = `Bearer ${token}`;
      }
      return config;
    });

    this.client.interceptors.response.use(
      (response) => response,
      async (error: AxiosError) => {
        const originalRequest = error.config as any;

        if (error.response?.status === 401 && !originalRequest._retry) {
          originalRequest._retry = true;

          const refreshed = await this.refreshToken();
          if (refreshed && originalRequest) {
            return this.client(originalRequest);
          }

          this.clearToken();
          if (typeof window !== 'undefined') {
            window.location.href = '/login';
          }
        }

        return Promise.reject(error);
      }
    );
  }

  // Auth methods
  async login(username: string, password: string) {
    const response = await this.client.post('/auth/login', { username, password });
    this.setToken(response.data.token);
    this.setRefreshToken(response.data.refreshToken);
    return response.data;
  }

  async register(username: string, email: string, password: string) {
    const response = await this.client.post('/auth/register', { username, email, password });
    this.setToken(response.data.token);
    this.setRefreshToken(response.data.refreshToken);
    return response.data;
  }

  async logout() {
    try {
      await this.client.post('/auth/logout');
    } finally {
      this.clearToken();
      this.clearRefreshToken();
    }
  }

  async refreshToken(): Promise<boolean> {
    try {
      const refreshToken = this.getRefreshToken();
      if (!refreshToken) return false;

      const response = await this.client.post('/auth/refresh', { refreshToken });
      this.setToken(response.data.token);
      this.setRefreshToken(response.data.refreshToken);
      return true;
    } catch {
      return false;
    }
  }

  // Portfolio methods
  async getPortfolio() {
    const response = await this.client.get('/portfolio');
    return response.data;
  }

  async getPositions() {
    const response = await this.client.get('/positions');
    return response.data;
  }

  // Strategy methods
  async getStrategies() {
    const response = await this.client.get('/strategies');
    return response.data;
  }

  async createStrategy(data: any) {
    const response = await this.client.post('/strategies', data);
    return response.data;
  }

  // Trading methods
  async placeBuyOrder(data: any) {
    const response = await this.client.post('/trading/buy', data);
    return response.data;
  }

  async placeSellOrder(data: any) {
    const response = await this.client.post('/trading/sell', data);
    return response.data;
  }

  // Token management
  private getToken(): string | null {
    if (typeof window === 'undefined') return null;
    if (this.cachedToken) return this.cachedToken;
    this.cachedToken = localStorage.getItem('auth_token');
    return this.cachedToken;
  }

  private setToken(token: string): void {
    this.cachedToken = token;
    if (typeof window !== 'undefined') {
      localStorage.setItem('auth_token', token);
    }
  }

  private clearToken(): void {
    this.cachedToken = null;
    if (typeof window !== 'undefined') {
      localStorage.removeItem('auth_token');
    }
  }

  private getRefreshToken(): string | null {
    if (typeof window === 'undefined') return null;
    return localStorage.getItem('refresh_token');
  }

  private setRefreshToken(token: string): void {
    if (typeof window !== 'undefined') {
      localStorage.setItem('refresh_token', token);
    }
  }

  private clearRefreshToken(): void {
    if (typeof window !== 'undefined') {
      localStorage.removeItem('refresh_token');
    }
  }
}

export const apiService = new ApiServiceV2();
```

### SignalR Hook Example

```typescript
// src/hooks/useSignalR.ts

'use client';

import { useEffect, useState, useCallback } from 'react';
import { getSignalRService } from '@/services/signalr';

export function useSignalR() {
  const [isConnected, setIsConnected] = useState(false);
  const [status, setStatus] = useState<'Connected' | 'Disconnected' | 'Reconnecting'>('Disconnected');

  useEffect(() => {
    const signalR = getSignalRService();

    const unsubscribeStatus = signalR.onStatusChange((newStatus) => {
      setStatus(newStatus);
      setIsConnected(newStatus === 'Connected');
    });

    signalR.start().catch((error) => {
      console.error('Failed to start SignalR:', error);
    });

    return () => {
      unsubscribeStatus();
      signalR.stop();
    };
  }, []);

  return { isConnected, status, signalR: getSignalRService() };
}

export function useSignalREvent<T>(
  eventName: string,
  handler: (data: T) => void,
  dependencies: any[] = []
) {
  const { signalR, isConnected } = useSignalR();

  useEffect(() => {
    if (!isConnected) return;

    const unsubscribe = signalR.on(eventName, handler);
    return unsubscribe;
  }, [eventName, signalR, isConnected, ...dependencies]);
}
```

---

## Conclusion

This 10-day migration plan provides a structured approach to migrating the AlgoTrendy frontend from v2.5 to v2.6. The key strategy is **copy-first**, preserving the existing structure while incrementally upgrading to Next.js 15, migrating to App Router, and integrating with the C# backend.

**Critical Success Factors:**
1. Maintain existing component structure
2. Incremental migration (don't break everything at once)
3. Thorough testing at each phase
4. Close collaboration with backend team for API integration
5. Performance monitoring throughout

**Timeline Summary:**
- Days 1-2: Copy & dependency upgrade
- Days 3-4: App Router migration
- Days 5-7: C# API integration
- Days 8-9: SignalR real-time features
- Day 10: Testing & validation

By following this plan, the frontend will be fully migrated, integrated with the C# backend, and ready for production deployment within the 10-12 day timeframe.
