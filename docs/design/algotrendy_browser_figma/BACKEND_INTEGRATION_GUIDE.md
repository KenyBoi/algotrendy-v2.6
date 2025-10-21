# Backend Integration Guide

**Status**: Configuration Complete - Ready for Implementation
**Date**: October 20, 2025
**Backend**: Running on `http://localhost:5002`
**Frontend**: Ready at `/root/AlgoTrendy_v2.6/docs/design/algotrendy_browser_figma/`

---

## ✅ Completed Setup

- [x] Backend running and healthy on port 5002
- [x] Frontend `.env` file configured with correct backend URL
- [x] API client infrastructure exists (`lib/api-client.ts`)
- [x] SignalR WebSocket client ready (`lib/signalr-client.ts`)
- [x] All frontend pages built (Dashboard, Orders, Positions, Strategies)
- [x] 40+ UI components ready

---

## 🔌 Backend API Endpoints (Available)

### Trading Operations (`/api/trading`)
| Method | Endpoint | Purpose | Frontend Usage |
|--------|----------|---------|----------------|
| POST | `/api/trading/orders` | Place new order | Orders page, Dashboard quick trade |
| GET | `/api/trading/orders/{orderId}` | Get order status | Orders page, live updates |
| DELETE | `/api/trading/orders/{orderId}` | Cancel order | Orders page cancel button |

### Portfolio Management (`/api/portfolio`)
| Method | Endpoint | Purpose | Frontend Usage |
|--------|----------|---------|----------------|
| GET | `/api/portfolio/debt-summary` | Get debt/margin summary | Dashboard, Positions page |
| GET | `/api/portfolio/leverage/{symbol}` | Get leverage info | Position detail modal |

### Market Data (`/api/v1/marketdata`)
| Method | Endpoint | Purpose | Frontend Usage |
|--------|----------|---------|----------------|
| GET | `/api/v1/marketdata/{exchange}/latest?symbols={symbols}` | Latest prices | Dashboard price tickers |
| GET | `/api/v1/marketdata/{exchange}/{symbol}` | Historical data | Charts, backtesting |

### Backtesting (`/api/v1/backtesting`)
| Method | Endpoint | Purpose | Frontend Usage |
|--------|----------|---------|----------------|
| POST | `/api/v1/backtesting/run` | Run backtest | Strategies page |
| GET | `/api/v1/backtesting/results/{id}` | Get results | Strategies results view |
| GET | `/api/v1/backtesting/config` | Get config options | Strategies builder |

### QuantConnect (`/api/quantconnect`)
| Method | Endpoint | Purpose | Frontend Usage |
|--------|----------|---------|----------------|
| GET | `/api/quantconnect/projects` | List projects | Advanced backtesting |
| POST | `/api/quantconnect/backtest` | Run QC backtest | Strategies page |
| GET | `/api/quantconnect/results/{projectId}` | Get QC results | Results viewer |

### Health Check
| Method | Endpoint | Purpose | Frontend Usage |
|--------|----------|---------|----------------|
| GET | `/health` | Backend health | App initialization, status indicator |

---

## 📊 Frontend-to-Backend Mapping

### Dashboard Page (`/dashboard`)

**Current State**: Mock data
**Needs**:
- Portfolio summary → `GET /api/portfolio/debt-summary`
- Latest prices → `GET /api/v1/marketdata/{exchange}/latest?symbols=...`
- Active orders count → Need to add `GET /api/trading/orders` endpoint
- Position summary → Need to add `GET /api/positions` endpoint

**Priority**: HIGH

---

### Orders Page (`/orders`)

**Current State**: Mock data table
**Needs**:
- List orders → Need `GET /api/trading/orders` (list all)
- Place order → `POST /api/trading/orders` ✅
- Cancel order → `DELETE /api/trading/orders/{orderId}` ✅
- Order status → `GET /api/trading/orders/{orderId}` ✅
- Real-time updates → SignalR `OrderUpdate` event

**Priority**: HIGH

**Missing Backend Endpoints**:
- `GET /api/trading/orders` (list all orders) - Currently only has single order by ID

---

### Positions Page (`/positions`)

**Current State**: Mock data table
**Needs**:
- List positions → Need `GET /api/positions` endpoint
- Position details → Need `GET /api/positions/{symbol}` endpoint
- Close position → Need `DELETE /api/positions/{symbol}` endpoint
- Leverage info → `GET /api/portfolio/leverage/{symbol}` ✅
- Real-time updates → SignalR `PositionUpdate` event

**Priority**: HIGH

**Missing Backend Endpoints**:
- `GET /api/positions` (list all)
- `GET /api/positions/{symbol}` (single position)
- `DELETE /api/positions/{symbol}` (close position)

---

### Strategies Page (`/strategies`)

**Current State**: Strategy builder UI
**Needs**:
- Run backtest → `POST /api/v1/backtesting/run` ✅
- Get results → `GET /api/v1/backtesting/results/{id}` ✅
- List strategies → `GET /api/v1/backtesting/config` ✅
- Save strategy → Need `POST /api/strategies` endpoint
- List saved strategies → Need `GET /api/strategies` endpoint

**Priority**: MEDIUM

**Missing Backend Endpoints**:
- `POST /api/strategies` (save strategy)
- `GET /api/strategies` (list saved strategies)
- `GET /api/strategies/{id}` (get strategy details)

---

## 🚀 Implementation Plan

### Phase 1: Core Trading (THIS WEEK)

**Step 1: Add Missing Backend Endpoints** (2-3 hours)
- Add `GET /api/trading/orders` - list all orders
- Add `GET /api/positions` - list all positions
- Add `GET /api/positions/{symbol}` - get single position
- Add `DELETE /api/positions/{symbol}` - close position

**Step 2: Integrate Orders Page** (2 hours)
- Replace mock data with real API calls
- Connect place order form to `POST /api/trading/orders`
- Connect cancel button to `DELETE /api/trading/orders/{orderId}`
- Add real-time updates via SignalR

**Step 3: Integrate Positions Page** (2 hours)
- Replace mock data with `GET /api/positions`
- Add position detail modal with `GET /api/portfolio/leverage/{symbol}`
- Connect close button to `DELETE /api/positions/{symbol}`
- Add real-time P&L updates via SignalR

**Step 4: Integrate Dashboard** (3 hours)
- Connect to `/api/portfolio/debt-summary`
- Add latest prices from `/api/v1/marketdata/.../latest`
- Show active orders count
- Show positions summary
- Add quick trade widget

---

### Phase 2: Real-Time Features (NEXT WEEK)

**Step 1: Complete SignalR Integration** (3 hours)
- Connect to `/hubs/market` hub
- Subscribe to `MarketUpdate` events
- Subscribe to `OrderUpdate` events
- Subscribe to `PositionUpdate` events
- Add live price ticker
- Add live P&L updates

**Step 2: Add Notifications** (1 hour)
- Toast notifications for order fills
- Toast notifications for position changes
- Alert for margin warnings

---

### Phase 3: Advanced Features (LATER)

**Step 1: Strategy Management** (4 hours)
- Add backend `POST /api/strategies` endpoint
- Add backend `GET /api/strategies` endpoint
- Connect strategy builder to save/load
- Integrate backtesting results display

**Step 2: MEM AI Integration** (6 hours)
- Add MEM backend endpoints (`/api/mem/*`)
- Connect MEMCorner to real status
- Connect AI chat to real MEM service
- Add MEM-powered insights

---

## 🛠️ Quick Start for Developers

### 1. Start Backend (if not running)
```bash
cd /root/AlgoTrendy_v2.6/backend/AlgoTrendy.API
dotnet run
# Backend will start at http://localhost:5002
```

### 2. Start Frontend
```bash
cd /root/AlgoTrendy_v2.6/docs/design/algotrendy_browser_figma
npm install  # if not done yet
npm run dev
# Frontend will start at http://localhost:5173
```

### 3. Test API Connection
```bash
# From frontend terminal
curl http://localhost:5002/health
# Should return: "Healthy"

# Test from browser console
fetch('http://localhost:5002/health').then(r => r.text()).then(console.log)
```

### 4. Start Coding
Open `src/pages/Orders.tsx` and replace mock data:

```typescript
// Before (mock):
const [orders, setOrders] = useState(mockOrders);

// After (real API):
import { api } from '../lib/api-client';

const [orders, setOrders] = useState<Order[]>([]);

useEffect(() => {
  const fetchOrders = async () => {
    const response = await api.orders.getAll();
    setOrders(response.data);
  };
  fetchOrders();
}, []);
```

---

## 📝 Notes

### CORS Configuration
Backend is already configured to allow frontend origin. If you get CORS errors:
1. Check backend `Program.cs` CORS policy
2. Ensure frontend URL (http://localhost:5173) is allowed

### Authentication
Current setup: No authentication required (development mode)
Future: JWT tokens will be required for production

### Rate Limiting
Backend has rate limiting enabled:
- General: 100 requests/minute
- Trading: 60 requests/minute
- Market Data: 1200 requests/minute

### WebSocket Hub Events
SignalR hub at `/hubs/market` sends:
- `MarketUpdate` - Price updates for subscribed symbols
- `OrderUpdate` - Order status changes
- `PositionUpdate` - Position P&L changes

Subscribe pattern:
```typescript
import { signalRClient } from '../lib/signalr-client';

await signalRClient.connect();
signalRClient.onMarketUpdate((update) => {
  console.log('Price update:', update);
  // Update UI
});
```

---

## ✅ Testing Checklist

Before considering integration complete:

### API Integration
- [ ] Orders page shows real orders from backend
- [ ] Can place new order and see it appear
- [ ] Can cancel order and see status update
- [ ] Positions page shows real positions
- [ ] Can close position
- [ ] Dashboard shows real portfolio summary
- [ ] Latest prices display on dashboard

### Real-Time Features
- [ ] SignalR connects successfully
- [ ] Price updates arrive via WebSocket
- [ ] Order updates arrive via WebSocket
- [ ] Position P&L updates in real-time
- [ ] No memory leaks from WebSocket

### Error Handling
- [ ] Graceful handling when backend is offline
- [ ] Error messages displayed to user
- [ ] Retry logic for failed requests
- [ ] Loading states during API calls

---

**Last Updated**: October 20, 2025
**Next Steps**: Add missing backend endpoints, then integrate Orders page
**Status**: READY TO IMPLEMENT 🚀
