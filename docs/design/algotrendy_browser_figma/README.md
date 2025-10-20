# AlgoTrendy Frontend - Browser Trading Interface

**Version:** 0.1.0
**Status:** 🎨 Design Implementation (Figma → Code)
**Tech Stack:** React 18 + Vite 6 + TypeScript + shadcn/ui
**Original Design:** [Figma - Browser Interface for Trading](https://www.figma.com/design/T2k3LiEtmOERY3Rjqyb4VW/Browser-Interface-for-Trading)

---

## 📋 Overview

This is the **web-based trading interface** for AlgoTrendy v2.6, a production-ready algorithmic trading platform. The frontend provides:

- **Real-time market data visualization** with TradingView-style charts
- **Advanced query builder** for strategy backtesting
- **Portfolio management dashboard** with live P&L tracking
- **Trade execution interface** with order management
- **Multi-asset support** (crypto, stocks, options, forex)
- **Responsive design** optimized for desktop trading

**Backend Integration:** Connects to AlgoTrendy v2.6 C# .NET 8 API
**Data Source:** QuestDB time-series database via REST API + SignalR WebSocket

---

## 🚀 Quick Start

### Local Development

```bash
# 1. Install dependencies
npm install

# 2. Create environment file
cp .env.example .env

# 3. Update .env with your backend URL
echo "VITE_API_BASE_URL=http://localhost:5002/api" > .env

# 4. Start development server
npm run dev

# Open http://localhost:5173
```

### Production Build

```bash
# Build optimized production bundle
npm run build

# Preview production build locally
npm run preview
```

---

## 🏗️ Architecture

### Frontend Stack

| Layer | Technology | Purpose |
|-------|-----------|---------|
| **Framework** | React 18.3 | Component-based UI |
| **Build Tool** | Vite 6.3 | Fast dev server, optimized builds |
| **Language** | TypeScript | Type safety |
| **UI Components** | shadcn/ui (Radix UI) | Accessible, customizable primitives |
| **Styling** | Tailwind CSS | Utility-first CSS |
| **Charts** | Recharts | Financial data visualization |
| **Forms** | React Hook Form | Form state management |
| **Theme** | next-themes | Dark/light mode support |
| **State** | React Context (future: Zustand/Redux) | Global state |

### Key Components

```
src/
├── components/
│   ├── QueryBuilder.tsx           # Advanced strategy query builder
│   └── ui/                         # shadcn/ui component library
│       ├── sidebar.tsx             # Navigation sidebar
│       ├── table.tsx               # Data tables (positions, orders)
│       ├── dialog.tsx              # Modals (order entry, settings)
│       ├── form.tsx                # Form components
│       └── [40+ UI primitives]
├── pages/                          # Page components (to be organized)
├── hooks/                          # Custom React hooks
├── lib/                            # Utilities, API client
└── styles/                         # Global styles, theme tokens
```

---

## 🔌 Backend Integration

### AlgoTrendy v2.6 Backend

**Location:** `/root/AlgoTrendy_v2.6/backend/`
**API Port:** 5002 (default)
**Technology:** ASP.NET Core 8 + SignalR
**Database:** QuestDB (time-series data)

### API Endpoints Used

| Endpoint | Method | Purpose |
|----------|--------|---------|
| `/api/health` | GET | Health check |
| `/api/market/latest` | GET | Latest market data |
| `/api/market/historical` | GET | Historical OHLCV data |
| `/api/orders` | POST | Place new order |
| `/api/orders/{id}` | GET | Get order status |
| `/api/positions` | GET | Get all positions |
| `/api/portfolio` | GET | Portfolio summary |
| `/api/backtest/run` | POST | Run backtest |
| `/api/backtest/results/{id}` | GET | Get backtest results |

### WebSocket Connection (SignalR)

```typescript
// Real-time market data streaming
const connection = new HubConnectionBuilder()
  .withUrl(`${API_BASE_URL}/hubs/market`)
  .build();

connection.on("MarketUpdate", (data) => {
  // Handle real-time price updates
});
```

---

## 🎨 Design System

### Colors

**Theme:** Dark mode optimized for trading (reduces eye strain)

```css
--background: 222.2 84% 4.9%      /* Dark background */
--foreground: 210 40% 98%         /* Light text */
--primary: 217.2 91.2% 59.8%      /* Blue accent */
--success: 142 76% 36%            /* Green for profits */
--destructive: 0 84.2% 60.2%      /* Red for losses */
```

### Typography

- **Font:** Inter (body), JetBrains Mono (code/numbers)
- **Scale:** 12px (xs) → 14px (sm) → 16px (base) → 20px (lg)

### Spacing

Tailwind scale: `4px` increments (p-1 = 4px, p-2 = 8px, etc.)

---

## 📦 Deployment

### Option 1: Vercel (Recommended - Fastest)

```bash
# Install Vercel CLI
npm i -g vercel

# Deploy to production
vercel --prod

# Set environment variable in Vercel dashboard:
# VITE_API_BASE_URL = https://your-backend.azurewebsites.net/api
```

**Cost:** Free tier available
**Time:** ~5 minutes
**Docs:** [DEPLOYMENT.md](./src/DEPLOYMENT.md)

---

### Option 2: Docker (Self-Hosted)

```bash
# Build Docker image
docker build -t algotrendy-frontend .

# Run container
docker run -d \
  -p 3000:80 \
  -e VITE_API_BASE_URL=http://backend:5002/api \
  algotrendy-frontend

# Access at http://localhost:3000
```

**Cost:** Infrastructure only (~$6/month for VPS)
**Time:** ~15 minutes
**Docs:** [DOCKER-SETUP.md](./src/DOCKER-SETUP.md)

---

### Option 3: Docker Compose (Full Stack)

```bash
# Start frontend + backend + QuestDB
docker-compose up -d

# Frontend: http://localhost:3000
# Backend API: http://localhost:5002
# QuestDB Console: http://localhost:9000
```

**Best for:** Local development and testing
**Docs:** [DOCKER-SETUP.md](./src/DOCKER-SETUP.md#integration-with-your-existing-docker-setup)

---

## 🔧 Configuration

### Environment Variables

Create `.env` in project root:

```env
# Backend API URL
VITE_API_BASE_URL=http://localhost:5002/api

# WebSocket URL (SignalR)
VITE_WS_URL=http://localhost:5002/hubs/market

# Feature Flags
VITE_ENABLE_BACKTESTING=true
VITE_ENABLE_OPTIONS_TRADING=true

# External Services (optional)
VITE_TRADINGVIEW_WIDGET_ID=your-widget-id
```

### Build-Time vs Runtime Config

**Important:** Vite environment variables are **baked into the build**. To change them after building, you must rebuild.

**Alternative:** Use nginx proxy pattern (see [DOCKER-SETUP.md](./src/DOCKER-SETUP.md#nginx-proxy-configuration-recommended))

---

## 🧪 Testing

```bash
# Run unit tests (to be added)
npm run test

# Run E2E tests with Playwright (to be added)
npm run test:e2e

# Run type checking
npm run type-check
```

**Current Status:** Tests not yet implemented (Phase 7F priority)

---

## 📂 Project Structure

```
algotrendy_browser_figma/
├── public/                     # Static assets
├── src/
│   ├── components/
│   │   ├── QueryBuilder.tsx    # Advanced strategy query interface
│   │   └── ui/                 # shadcn/ui components (40+ primitives)
│   ├── pages/                  # Page components (to be organized)
│   ├── hooks/                  # Custom React hooks
│   ├── lib/                    # Utilities, API client, helpers
│   ├── styles/
│   │   └── globals.css         # Global styles, Tailwind directives
│   ├── main.tsx                # App entry point
│   └── index.css               # Root styles
├── Dockerfile                  # Production Docker image
├── Dockerfile.dev              # Development Docker image
├── docker-compose.yml          # Full stack orchestration
├── nginx.conf                  # Production nginx config
├── vite.config.ts              # Vite configuration
├── package.json                # Dependencies
├── DEPLOYMENT.md               # Detailed deployment guide
├── DOCKER-SETUP.md             # Docker integration guide
└── README.md                   # This file
```

---

## 🔗 Integration with AlgoTrendy v2.6

### Directory Location

```
/root/AlgoTrendy_v2.6/
├── backend/                    # C# .NET 8 API (main trading engine)
├── frontend/                   # Next.js frontend (existing, to be replaced)
├── docs/design/
│   └── algotrendy_browser_figma/  # 👈 THIS APPLICATION
├── ml-service/                 # Python ML prediction service
└── docker-compose.prod.yml     # Production orchestration
```

### Deployment Strategy

**Phase 7F (Dashboard UI):** Replace `/frontend/` with this implementation

**Timeline:**
1. **Week 1-2:** Complete page implementations (Dashboard, Orders, Positions)
2. **Week 2-3:** Integrate with backend API, test all endpoints
3. **Week 3-4:** Add WebSocket real-time updates, optimize performance
4. **Week 4:** Production deployment, monitoring setup

**Estimated Effort:** 60-80 hours (see v2.6 roadmap)

---

## 🛠️ Development Workflow

### Adding New Pages

```bash
# 1. Create page component
touch src/pages/StrategyBuilder.tsx

# 2. Add route (router to be configured)
# 3. Connect to backend API
# 4. Test with mock data first
# 5. Integrate real backend
```

### Adding New UI Components

```bash
# Use shadcn CLI to add components
npx shadcn-ui@latest add [component-name]

# Example: Add a new button variant
npx shadcn-ui@latest add button
```

### API Integration Pattern

```typescript
// src/lib/api.ts
const API_BASE_URL = import.meta.env.VITE_API_BASE_URL;

export async function fetchPositions() {
  const response = await fetch(`${API_BASE_URL}/positions`);
  if (!response.ok) throw new Error('Failed to fetch positions');
  return response.json();
}
```

---

## 🐛 Known Issues / TODO

- [ ] Router not configured (react-router-dom to be added)
- [ ] API client not implemented (axios/fetch wrapper needed)
- [ ] WebSocket connection not implemented (SignalR integration)
- [ ] No authentication flow (JWT integration needed)
- [ ] No error boundary (global error handling needed)
- [ ] No loading states (skeleton components needed)
- [ ] QueryBuilder not connected to backend (backtest API)
- [ ] No real-time chart updates (TradingView widget integration)

**Priority:** Phase 7F implementation (see AlgoTrendy v2.6 roadmap)

---

## 🔐 Security Considerations

### Production Checklist

- [ ] Enable HTTPS (SSL/TLS) for all API calls
- [ ] Configure CORS in backend to allow frontend origin only
- [ ] Implement JWT token refresh flow
- [ ] Add rate limiting to prevent API abuse
- [ ] Sanitize all user inputs (QueryBuilder especially)
- [ ] Enable Content Security Policy (CSP) headers
- [ ] Use environment variables for all secrets (never commit .env)
- [ ] Enable nginx security headers (X-Frame-Options, etc.)

**See:** [DEPLOYMENT.md](./src/DEPLOYMENT.md#security-checklist)

---

## 📊 Performance Targets

| Metric | Target | Current |
|--------|--------|---------|
| **First Contentful Paint** | < 1.5s | TBD |
| **Time to Interactive** | < 3.0s | TBD |
| **Bundle Size** | < 300 KB | ~250 KB (estimated) |
| **Lighthouse Score** | > 90 | TBD |
| **WebSocket Latency** | < 100ms | TBD |

---

## 📚 Documentation

### Quick Links

- **Main Deployment Guide:** [DEPLOYMENT.md](./src/DEPLOYMENT.md)
- **Docker Setup Guide:** [DOCKER-SETUP.md](./src/DOCKER-SETUP.md)
- **AlgoTrendy v2.6 Docs:** `/root/AlgoTrendy_v2.6/docs/`
- **Backend API Docs:** `/root/AlgoTrendy_v2.6/backend/README.md`
- **Original Figma Design:** [Browser Interface for Trading](https://www.figma.com/design/T2k3LiEtmOERY3Rjqyb4VW/Browser-Interface-for-Trading)

### Related Documentation

- **AlgoTrendy v2.6 Overview:** `/root/AlgoTrendy_v2.6/README.md`
- **Implementation Roadmap:** `/root/AlgoTrendy_v2.6/START_HERE.md`
- **Phase 7F (Dashboard):** See CONTINUATION_STATUS.md in ai_context/

---

## 🚦 Status

**Current Phase:** Design Implementation (Figma → Code)
**Completion:** ~40% (UI components done, pages & integration pending)
**Next Steps:**
1. Add React Router for navigation
2. Implement API client with backend integration
3. Create main pages (Dashboard, Orders, Positions, Strategies)
4. Add SignalR WebSocket for real-time updates
5. Implement authentication flow
6. Production deployment

**Blockers:** None
**Dependencies:** AlgoTrendy v2.6 backend API (✅ Available)

---

## 💡 Contributing

### Code Style

- **Formatting:** Prettier (to be configured)
- **Linting:** ESLint (to be configured)
- **Naming:** PascalCase for components, camelCase for functions/variables
- **TypeScript:** Strict mode enabled

### Git Workflow

```bash
# Create feature branch
git checkout -b feature/add-dashboard-page

# Make changes, commit frequently
git add .
git commit -m "feat: Add portfolio dashboard page"

# Push and create PR
git push origin feature/add-dashboard-page
```

---

## 📞 Support

**Questions about:**
- **Frontend:** Check this README, DEPLOYMENT.md, or DOCKER-SETUP.md
- **Backend Integration:** See `/root/AlgoTrendy_v2.6/backend/README.md`
- **Overall Project:** See `/root/AlgoTrendy_v2.6/START_HERE.md`

**Common Issues:**
- **CORS errors:** Configure backend CORS policy (see DOCKER-SETUP.md)
- **Build fails:** Delete `node_modules` and run `npm install` again
- **API not connecting:** Check `VITE_API_BASE_URL` in `.env`
- **Styles not loading:** Run `npm run build` to rebuild Tailwind

---

## 📝 License

Private - AlgoTrendy Trading Platform
**Not for redistribution**

---

**Last Updated:** October 20, 2025
**Version:** 0.1.0
**Status:** 🎨 In Development (Phase 7F pending)
