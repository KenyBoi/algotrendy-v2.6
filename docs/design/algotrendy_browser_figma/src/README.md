# AlgoTrendy Frontend

**Institutional-grade trading interface powered by MEM, your AI trading intelligence**

---

## 🎯 Overview

AlgoTrendy is a sophisticated trading platform that combines professional hedge fund-quality tools with intelligent AI assistance. Built for mathematicians and quantitative traders who want to explore markets, build strategies, and execute trades with millisecond precision.

### Key Features

- **MEM AI Assistant** - Your intelligent trading partner that makes millisecond decisions
- **Strategy Builder** - Design, backtest, and deploy algorithmic trading strategies
- **Data Explorer** - Access ultra-powerful datasets and market data
- **Live Trading Monitor** - Watch MEM make real-time trading decisions
- **Performance Analytics** - Track strategy performance and MEM's learning curve

---

## 🏗️ Architecture

### Tech Stack

- **Frontend:** React + TypeScript + Tailwind CSS
- **Backend:** .NET 8 / C# (http://localhost:5298/api)
- **Database:** QuestDB (time-series data)
- **Real-time:** SignalR WebSockets
- **UI Components:** shadcn/ui
- **Charts:** Recharts
- **Animations:** Motion (Framer Motion)

### Project Structure

```
├── components/          # React components
│   ├── MEMCorner.tsx   # MEM status widget
│   ├── AIAssistant.tsx # MEM chat interface
│   ├── StrategyBuilder.tsx
│   ├── DatasetBrowser.tsx
│   ├── QueryBuilder.tsx
│   └── ui/             # shadcn/ui components
├── services/           # API integration layer
│   ├── memService.ts   # MEM endpoints
│   ├── aiService.ts
│   ├── datasetService.ts
│   ├── strategyService.ts
│   └── queryService.ts
├── types/              # TypeScript types
├── hooks/              # Custom React hooks
├── config/             # Configuration
├── guidelines/         # Documentation
│   ├── Design-System.md
│   ├── MEM-Integration-Architecture.md
│   ├── Backend-API-Reference.md
│   └── Implementation-Status.md
└── styles/             # Global styles
```

---

## 🚀 Getting Started

### Prerequisites

- Node.js 18+
- .NET 8 SDK (for backend)
- QuestDB (for time-series data)

### Installation

```bash
# Install dependencies
npm install

# Start development server
npm run dev
```

### Backend Setup

```bash
# Navigate to backend directory
cd backend

# Run .NET backend
dotnet run
# Backend will run on http://localhost:5298
```

### Environment

The app will automatically use mock data if the backend is not running. Toggle the backend connection using the checkbox in the UI.

---

## 🧠 MEM - Your AI Trading Intelligence

### What is MEM?

MEM is AlgoTrendy's proprietary AI system that acts as both CEO and Executive Assistant:

- **Makes trading decisions** with millisecond precision
- **Executes strategies** designed by you
- **Analyzes markets** using ML and advanced algorithms
- **Provides insights** in friendly, conversational language
- **Learns continuously** to improve performance

### MEM's Personality

**Professional platform + Friendly AI collaborator**

MEM communicates like a knowledgeable friend who's enthusiastic about markets:

```
❌ Corporate: "The RSI indicator has reached the oversold threshold."
✅ MEM: "Yo! RSI just hit 28 - that's oversold territory. I'm seeing 
        a pattern here that worked 23 out of 27 times. Want me to grab some?"
```

### MEM's Visual Presence

- **Institutional aesthetics** - Dark, sophisticated, Bloomberg Terminal quality
- **Friendly personality** - In the words, not bouncy animations
- **Professional blue** - Clean #3B82F6 accent color
- **Minimal animations** - Subtle, purposeful, fast
- **Data-focused** - Numbers, charts, performance metrics

---

## 📱 Main Components

### 1. MEM Corner (Always Visible)

Persistent widget in bottom-right corner showing MEM's status:
- Active strategies and open positions
- Today's P&L and health score
- Recent activity feed
- Quick access to MEM chat
- Trading controls (pause/resume)

### 2. MEM Chat Interface

Full conversational interface with MEM:
- Ask questions about markets
- Get strategy optimization suggestions
- Request data analysis
- Understand MEM's decisions
- Natural language interaction

### 3. Strategy Builder

Design algorithmic trading strategies:
- Visual strategy builder
- Real-time backtesting
- MEM validation and optimization
- Deploy to MEM for live execution
- Performance comparison

### 4. Dataset Explorer

Browse and analyze market data:
- Access MEM's proprietary datasets
- Query using natural language
- Visualize data patterns
- Export findings
- MEM-powered insights

### 5. Live Trading Monitor (Coming Soon)

Watch MEM trade in real-time:
- Live decision feed
- Real-time P&L
- Execution quality metrics
- Manual override controls
- MEM's reasoning for each trade

---

## 🎨 Design System

### Color Palette

```css
/* Dark Theme (Primary) */
Background: #0B0E13 (Deep charcoal)
Cards: #141922 (Elevated surfaces)
Borders: #1F2937 (Subtle dividers)
Text: #F9FAFB (Primary)
MEM Accent: #3B82F6 (Professional blue)

/* Status Colors */
Success: #10B981 (Green)
Warning: #F59E0B (Amber)
Error: #EF4444 (Red)
```

### Typography

```css
Interface: Inter (sans-serif)
Data/Numbers: JetBrains Mono (monospace)
```

### Principles

1. **Institutional Quality** - Every pixel matters
2. **Data Clarity** - Information density without clutter
3. **Subtle Intelligence** - Professional AI presence
4. **Performance First** - Fast, responsive, no bloat
5. **Dark Mode Native** - Designed for extended sessions

See `/guidelines/Design-System.md` for complete design system.

---

## 🔌 API Integration

### Backend Endpoints

All endpoints documented in `/guidelines/Backend-API-Reference.md`

#### MEM Endpoints

```typescript
GET  /api/mem/status              // MEM's current status
POST /api/mem/chat                // Chat with MEM
POST /api/mem/analyze             // Analyze data
GET  /api/mem/insights            // Get insights
GET  /api/mem/opportunities       // Trading opportunities
POST /api/mem/validate-strategy   // Validate strategy
POST /api/mem/optimize-strategy   // Optimize strategy
```

#### Market Data

```typescript
GET  /api/v1/marketdata/{symbol}/latest
GET  /api/v1/marketdata/{symbol}
GET  /api/v1/marketdata/latest?symbols={symbols}
```

#### Trading

```typescript
POST /api/trading/orders          // Place order
GET  /api/trading/orders/{id}     // Order status
GET  /api/portfolio/debt-summary  // Portfolio summary
```

#### WebSocket (SignalR)

```typescript
Hub: /hubs/marketdata
Events: 
  - ReceiveMarketData
  - MEM.StatusUpdate
  - MEM.DecisionMade
  - MEM.TradeExecuted
```

---

## 🛠️ Development

### Available Scripts

```bash
npm run dev      # Start development server
npm run build    # Build for production
npm run preview  # Preview production build
npm run lint     # Run ESLint
```

### Code Style

- **TypeScript** for type safety
- **Functional components** with hooks
- **Tailwind CSS** for styling
- **ESLint + Prettier** for code formatting

### Component Guidelines

1. Use TypeScript interfaces for all props
2. Extract reusable logic into custom hooks
3. Use shadcn/ui components when possible
4. Keep components focused and single-purpose
5. Document complex logic with comments

---

## 📚 Documentation

### For Developers

- **[Design System](/guidelines/Design-System.md)** - Complete design guidelines
- **[MEM Architecture](/guidelines/MEM-Integration-Architecture.md)** - Technical architecture
- **[API Reference](/guidelines/Backend-API-Reference.md)** - All backend endpoints
- **[Integration Map](/guidelines/Frontend-Backend-Integration.md)** - Component-to-API mapping
- **[Implementation Status](/guidelines/Implementation-Status.md)** - Current progress

### For Users

- **Strategy Building** - Design algorithmic trading strategies
- **Data Analysis** - Query and visualize market data
- **Live Trading** - Monitor MEM's real-time decisions
- **Performance Tracking** - Analyze strategy results

---

## 🎯 Roadmap

### ✅ Completed

- [x] Core UI framework
- [x] MEM Corner widget
- [x] MEM Chat interface
- [x] Strategy Builder (basic)
- [x] Dataset Browser (basic)
- [x] API service layer
- [x] TypeScript types
- [x] Design system

### 🚧 In Progress

- [ ] Backend integration (MEM endpoints)
- [ ] WebSocket real-time updates
- [ ] Strategy Builder enhancements
- [ ] Live Trading Monitor

### 📋 Planned

- [ ] Advanced charting
- [ ] Portfolio analytics
- [ ] Risk management tools
- [ ] Multi-exchange support
- [ ] Mobile responsive design

---

## 🤝 Contributing

This is a proprietary project for AlgoTrendy. For internal development:

1. Create a feature branch
2. Make your changes
3. Test thoroughly
4. Submit for review
5. Deploy to staging first

---

## 📄 License

Proprietary - AlgoTrendy © 2025

---

## 🆘 Support

For questions or issues:
- Check `/guidelines` documentation
- Review API reference
- Contact development team

---

## 🎨 Design Philosophy

**"Institutional-grade tools with intelligent AI collaboration"**

This isn't just another trading platform. It's a sophisticated workspace where professional traders collaborate with MEM, an AI that makes millisecond trading decisions while explaining its reasoning in friendly, conversational language.

The interface is dark, polished, and data-focused - something that belongs on a hedge fund trading desk. But underneath, MEM's personality shines through in its words, making complex trading accessible and collaborative.

**Built for mathematicians. Designed for professionals. Powered by AI.**

---

**Last Updated:** 2025-10-20  
**Version:** 1.0.0  
**Status:** Foundation Complete - Backend Integration In Progress
