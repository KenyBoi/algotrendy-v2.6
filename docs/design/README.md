# AlgoTrendy v2.6 - Design & Frontend Documentation

**Purpose:** This directory contains frontend design implementations and UI documentation for AlgoTrendy v2.6

---

## 📂 Directory Structure

```
/docs/design/
├── README.md                           # This file - Design directory index
└── algotrendy_browser_figma/          # React + Vite trading interface
    ├── README.md                       # 👈 START HERE - Comprehensive deployment guide
    ├── DEPLOYMENT.md                   # Cloud deployment options (Vercel, Netlify, Azure)
    ├── DOCKER-SETUP.md                 # Docker integration & self-hosting guide
    ├── src/                            # React application source code
    │   ├── components/                 # UI components (shadcn/ui)
    │   │   ├── QueryBuilder.tsx        # Advanced strategy query builder
    │   │   └── ui/                     # 40+ primitive components
    │   ├── pages/                      # Page components (to be organized)
    │   ├── hooks/                      # Custom React hooks
    │   ├── lib/                        # API client, utilities
    │   └── styles/                     # Global styles, Tailwind config
    ├── docker-compose.yml              # Full stack orchestration
    ├── Dockerfile                      # Production Docker image
    ├── nginx.conf                      # Production nginx config
    ├── vite.config.ts                  # Vite build configuration
    └── package.json                    # Dependencies
```

---

## 🎯 What's Here

### Frontend Application (algotrendy_browser_figma/)

**Technology Stack:**
- React 18.3 + TypeScript
- Vite 6.3 (build tool)
- shadcn/ui (Radix UI components)
- Tailwind CSS
- Recharts (financial charts)
- React Hook Form (forms)

**Status:** 🎨 Design Implementation (Figma → Code)
**Completion:** ~40%
**Phase:** 7F (Dashboard UI)

**Features:**
- ✅ 40+ UI components (shadcn/ui)
- ✅ Advanced query builder for backtesting
- ✅ Dark mode optimized for trading
- ✅ Responsive design
- ⏳ API integration (pending)
- ⏳ Real-time WebSocket updates (pending)
- ⏳ Authentication flow (pending)

---

## 🚀 Quick Start

### For Developers

**1. Read the main README first:**
```bash
cat /root/AlgoTrendy_v2.6/docs/design/algotrendy_browser_figma/README.md
```

**2. Local development:**
```bash
cd /root/AlgoTrendy_v2.6/docs/design/algotrendy_browser_figma

# Install dependencies
npm install

# Start dev server
npm run dev

# Access at http://localhost:5173
```

**3. Integration with backend:**
```bash
# Create .env file
echo "VITE_API_BASE_URL=http://localhost:5002/api" > .env

# Backend should be running at localhost:5002
# See: /root/AlgoTrendy_v2.6/backend/
```

---

### For DevOps / Deployment

**Option 1: Vercel (Cloud - Fastest)**
```bash
cd /root/AlgoTrendy_v2.6/docs/design/algotrendy_browser_figma
vercel --prod
```
**Time:** 5 minutes
**Cost:** Free tier available
**Docs:** [DEPLOYMENT.md](./algotrendy_browser_figma/src/DEPLOYMENT.md)

---

**Option 2: Docker (Self-Hosted)**
```bash
cd /root/AlgoTrendy_v2.6/docs/design/algotrendy_browser_figma
docker build -t algotrendy-frontend .
docker run -d -p 3000:80 algotrendy-frontend
```
**Time:** 15 minutes
**Cost:** VPS only (~$6/month)
**Docs:** [DOCKER-SETUP.md](./algotrendy_browser_figma/src/DOCKER-SETUP.md)

---

**Option 3: Full Stack (Docker Compose)**
```bash
cd /root/AlgoTrendy_v2.6/docs/design/algotrendy_browser_figma
docker-compose up -d

# Frontend: http://localhost:3000
# Backend: http://localhost:5002
# QuestDB: http://localhost:9000
```
**Best for:** Local development/testing
**Docs:** [DOCKER-SETUP.md](./algotrendy_browser_figma/src/DOCKER-SETUP.md#integration-with-your-existing-docker-setup)

---

## 📖 Documentation Guide

### Primary Documentation

| File | Purpose | Read Time | Audience |
|------|---------|-----------|----------|
| **[README.md](./algotrendy_browser_figma/README.md)** | Comprehensive overview | 10 min | Everyone |
| **[DEPLOYMENT.md](./algotrendy_browser_figma/src/DEPLOYMENT.md)** | Cloud deployment guide | 15 min | DevOps |
| **[DOCKER-SETUP.md](./algotrendy_browser_figma/src/DOCKER-SETUP.md)** | Docker integration | 15 min | DevOps |

### Quick Reference

**Question** → **Answer Location**

| Question | Document |
|----------|----------|
| What is this frontend? | [README.md](./algotrendy_browser_figma/README.md#overview) |
| How do I run it locally? | [README.md](./algotrendy_browser_figma/README.md#quick-start) |
| How do I deploy to Vercel? | [DEPLOYMENT.md](./algotrendy_browser_figma/src/DEPLOYMENT.md#option-1-vercel-recommended---easiest) |
| How do I deploy with Docker? | [DOCKER-SETUP.md](./algotrendy_browser_figma/src/DOCKER-SETUP.md) |
| How does it connect to backend? | [README.md](./algotrendy_browser_figma/README.md#backend-integration) |
| What's the tech stack? | [README.md](./algotrendy_browser_figma/README.md#architecture) |
| What's missing/TODO? | [README.md](./algotrendy_browser_figma/README.md#known-issues--todo) |
| How do I add new pages? | [README.md](./algotrendy_browser_figma/README.md#development-workflow) |

---

## 🔗 Integration with AlgoTrendy v2.6

### How This Fits In

```
AlgoTrendy v2.6 Architecture:

┌─────────────────────────────────────────────────────────┐
│  Frontend (This Application)                            │
│  Location: docs/design/algotrendy_browser_figma/       │
│  Tech: React + Vite + TypeScript                        │
│  Status: 🎨 Design Implementation (40% complete)        │
└──────────────────┬──────────────────────────────────────┘
                   │ REST API + SignalR WebSocket
                   ▼
┌─────────────────────────────────────────────────────────┐
│  Backend API                                             │
│  Location: /root/AlgoTrendy_v2.6/backend/              │
│  Tech: C# .NET 8 + ASP.NET Core                         │
│  Status: ✅ Production Ready (306/407 tests passing)    │
└──────────────────┬──────────────────────────────────────┘
                   │ SQL queries
                   ▼
┌─────────────────────────────────────────────────────────┐
│  Database (QuestDB)                                      │
│  Tech: Time-series database                              │
│  Data: Market data, orders, positions, backtest results │
│  Status: ✅ Operational                                  │
└─────────────────────────────────────────────────────────┘
```

### Deployment Strategy

**Current State:**
- Backend: ✅ Production ready
- Frontend: 🎨 Design implementation (40%)

**Phase 7F Timeline:**
1. **Week 1-2:** Complete pages (Dashboard, Orders, Positions)
2. **Week 2-3:** API integration, testing
3. **Week 3-4:** WebSocket real-time updates
4. **Week 4:** Production deployment

**Estimated Effort:** 60-80 hours

---

## 📊 Current Status

### What's Complete ✅

- [x] 40+ shadcn/ui components (buttons, forms, tables, dialogs, etc.)
- [x] QueryBuilder component for strategy creation
- [x] Dark mode theme optimized for trading
- [x] Tailwind CSS design system
- [x] Docker deployment configuration
- [x] Nginx production config
- [x] Comprehensive deployment documentation

### What's Pending ⏳

- [ ] React Router setup (navigation)
- [ ] API client implementation (fetch/axios wrapper)
- [ ] Main pages (Dashboard, Orders, Positions, Strategies)
- [ ] SignalR WebSocket integration (real-time updates)
- [ ] Authentication flow (JWT tokens)
- [ ] Error boundaries & loading states
- [ ] Unit tests (Vitest/React Testing Library)
- [ ] E2E tests (Playwright)
- [ ] TradingView chart widget integration

---

## 🎯 Next Steps

### For Project Manager
1. Review [README.md](./algotrendy_browser_figma/README.md) - Overview & status
2. Check Phase 7F roadmap in [CONTINUATION_STATUS.md](/root/AlgoTrendy_v2.6/ai_context/CONTINUATION_STATUS.md)
3. Allocate 60-80 hours for completion

### For Developers
1. Read [README.md](./algotrendy_browser_figma/README.md) - Full guide
2. Set up local development environment
3. Review [Known Issues/TODO](./algotrendy_browser_figma/README.md#known-issues--todo)
4. Start implementing pages + API integration

### For DevOps
1. Review [DEPLOYMENT.md](./algotrendy_browser_figma/src/DEPLOYMENT.md) - Cloud options
2. Review [DOCKER-SETUP.md](./algotrendy_browser_figma/src/DOCKER-SETUP.md) - Self-hosting
3. Plan production deployment strategy
4. Set up CI/CD pipeline (GitHub Actions)

---

## 🔐 Security Notes

**Important:** This frontend will handle financial data and trading operations.

**Pre-Deployment Security Checklist:**
- [ ] HTTPS enabled for all API calls
- [ ] CORS configured properly in backend
- [ ] JWT token refresh flow implemented
- [ ] Input sanitization (especially QueryBuilder)
- [ ] CSP headers configured
- [ ] No secrets in .env committed to git
- [ ] Rate limiting enabled
- [ ] API key rotation implemented

See: [DEPLOYMENT.md Security Checklist](./algotrendy_browser_figma/src/DEPLOYMENT.md#security-checklist)

---

## 📚 Related Documentation

### AlgoTrendy v2.6 Project Docs
- **Project Overview:** `/root/AlgoTrendy_v2.6/README.md`
- **Quick Start Guide:** `/root/AlgoTrendy_v2.6/START_HERE.md`
- **Backend API:** `/root/AlgoTrendy_v2.6/backend/README.md`
- **Implementation Roadmap:** `/root/AlgoTrendy_v2.6/ai_context/CONTINUATION_STATUS.md`

### Design Resources
- **Figma Design:** [Browser Interface for Trading](https://www.figma.com/design/T2k3LiEtmOERY3Rjqyb4VW/Browser-Interface-for-Trading)
- **shadcn/ui Docs:** https://ui.shadcn.com
- **Radix UI Docs:** https://www.radix-ui.com
- **Tailwind CSS:** https://tailwindcss.com

---

## 💡 Development Tips

### Setting Up IDE

**VS Code Extensions (Recommended):**
- ESLint
- Prettier
- Tailwind CSS IntelliSense
- TypeScript Import Sorter
- Error Lens

**Configuration:**
```bash
cd /root/AlgoTrendy_v2.6/docs/design/algotrendy_browser_figma

# Install dependencies
npm install

# Open in VS Code
code .
```

### Running Backend Locally

The frontend needs the backend API running:

```bash
# In another terminal
cd /root/AlgoTrendy_v2.6/backend/AlgoTrendy.API
dotnet run

# API will start at http://localhost:5002
```

### Hot Reload Development

```bash
# Terminal 1: Backend
cd /root/AlgoTrendy_v2.6/backend/AlgoTrendy.API
dotnet watch run

# Terminal 2: Frontend
cd /root/AlgoTrendy_v2.6/docs/design/algotrendy_browser_figma
npm run dev

# Both will hot reload on file changes
```

---

## 🐛 Common Issues

**Build fails:**
```bash
# Clear node_modules and reinstall
rm -rf node_modules package-lock.json
npm install
```

**CORS errors:**
```bash
# Backend CORS policy needs to allow frontend origin
# See: /root/AlgoTrendy_v2.6/backend/AlgoTrendy.API/Program.cs
# Add: WithOrigins("http://localhost:5173")
```

**Styles not loading:**
```bash
# Rebuild Tailwind
npm run build
```

**API not connecting:**
```bash
# Check .env file
cat .env
# Should have: VITE_API_BASE_URL=http://localhost:5002/api
```

---

## 📞 Support

**Questions?**
- Frontend issues → See [README.md](./algotrendy_browser_figma/README.md)
- Deployment → See [DEPLOYMENT.md](./algotrendy_browser_figma/src/DEPLOYMENT.md)
- Docker → See [DOCKER-SETUP.md](./algotrendy_browser_figma/src/DOCKER-SETUP.md)
- Backend integration → See `/root/AlgoTrendy_v2.6/backend/README.md`
- Project overview → See `/root/AlgoTrendy_v2.6/START_HERE.md`

---

**Last Updated:** October 20, 2025
**Status:** 🎨 Design Implementation (Phase 7F)
**Completion:** ~40% (UI components done, pages & API integration pending)
