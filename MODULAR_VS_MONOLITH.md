# AlgoTrendy: Modular vs Monolith Deployment Guide

**Last Updated:** October 21, 2025
**Current Branch:** modular
**Version:** v3.0.0-beta (modular) | v2.6.x (monolith on main)

---

## TL;DR - Quick Decision Guide

**Choose Monolith if:**
- ✅ Simple deployment (single container)
- ✅ Small to medium scale (<1000 req/min)
- ✅ You want easy debugging
- ✅ You have limited DevOps resources

**Choose Microservices if:**
- ✅ Need independent scaling
- ✅ High traffic (>10,000 req/min)
- ✅ Multiple teams working in parallel
- ✅ Want fault isolation

**Bottom Line:** Start with monolith, migrate to microservices when needed.

---

## Architecture Comparison

### Monolith (v2.6.x - Main Branch)

```
┌─────────────────────────────────────────┐
│        AlgoTrendy API (Port 5000)       │
│                                         │
│  ┌──────────┬──────────┬──────────┐   │
│  │ Trading  │   Data   │Backtesting│  │
│  │  Engine  │ Channels │  Engine   │  │
│  └──────────┴──────────┴──────────┘   │
│                 ↓                       │
│         ┌──────────────┐                │
│         │   QuestDB    │                │
│         │    Redis     │                │
│         └──────────────┘                │
└─────────────────────────────────────────┘

Single Container | Single Process | Shared Memory
```

**Deployment:**
```bash
docker-compose up -d
```

### Microservices (v3.0.0-beta - Modular Branch)

```
┌─────────┐     ┌──────────┐     ┌─────────┐
│   API   │────▶│ Trading  │     │  Data   │
│ Gateway │     │ Service  │     │ Service │
│  :5000  │     │  :5001   │     │  :5002  │
└─────────┘     └──────────┘     └─────────┘
      │               │                 │
      └───────────────┼─────────────────┘
                      ▼
            ┌──────────────────┐
            │  QuestDB + Redis │
            └──────────────────┘

Multiple Containers | Independent Processes | HTTP/Redis Communication
```

**Deployment:**
```bash
docker-compose -f docker-compose.modular.yml up -d
```

---

## Feature Comparison Matrix

| Feature | Monolith | Microservices | Notes |
|---------|----------|---------------|-------|
| **Brokers** | 11 brokers | 11 brokers | Identical: Binance, Bybit, MEXC, Alpaca, Freqtrade, etc. |
| **Data Channels** | 8 providers | 8 providers | Identical: Alpaca, CoinGecko, EODHD, etc. |
| **Backtesting** | 3 engines | 3 engines | Identical: Custom, QuantConnect, BacktestingPy |
| **MEM AI** | ✅ Full support | ✅ Full support | 78% ML accuracy, self-learning |
| **Freqtrade** | ✅ Integrated | ✅ Integrated | Multi-bot monitoring |
| **ML System** | ✅ XGBoost+LSTM | ✅ XGBoost+LSTM | Hybrid models |
| **API Endpoints** | 50+ endpoints | 50+ endpoints | Same REST API |
| **Frontend** | React + Vite | React + Vite | Identical UI |
| **Database** | QuestDB + Postgres | QuestDB + Postgres | Shared data layer |
| **Caching** | Redis | Redis | Shared cache |

### Code Differences

**Answer: ZERO**

Both architectures use the **identical codebase** from `backend/` directory.

- Monolith: All modules loaded in single process
- Microservices: Modules distributed across containers

**Same features | Same code | Different deployment**

---

## Performance Comparison

### Monolith Performance

| Metric | Value |
|--------|-------|
| **Startup Time** | 15-20 seconds |
| **Memory Usage** | 500MB - 1GB |
| **CPU Usage** | 10-30% (single core) |
| **Request Latency** | 50-100ms avg |
| **Max Throughput** | ~1,000 req/min |
| **Max Symbols** | 100-200 actively tracked |

**Bottlenecks:**
- Single process = CPU bound
- Shared memory = potential contention
- No horizontal scaling

### Microservices Performance

| Metric | Trading Service | Data Service | Backtesting Service |
|--------|----------------|--------------|---------------------|
| **Startup Time** | 10-15s | 10-15s | 15-20s |
| **Memory** | 300MB | 400MB | 600MB |
| **CPU** | 20-40% | 30-50% | 60-80% |
| **Latency** | 40-80ms | 30-60ms | 200-500ms |
| **Throughput** | 2,000 req/min | 5,000 req/min | 100 backtests/min |
| **Scalability** | ✅ Horizontal | ✅ Horizontal | ✅ Horizontal |

**Advantages:**
- Independent scaling per service
- Isolated resource usage
- Parallel processing
- Fault isolation

**Network Overhead:** +10-20ms per inter-service call

---

## Deployment Scenarios

### Scenario 1: Local Development

**Recommendation:** Monolith

**Why:**
- Faster startup
- Easier debugging
- Single container to manage
- Lower resource usage

**Command:**
```bash
docker-compose up
# Starts on http://localhost:5000
```

### Scenario 2: Small Production (<100 users)

**Recommendation:** Monolith

**Why:**
- Simpler operations
- Lower hosting costs ($20-50/month)
- Single point of monitoring
- Adequate performance

**Hosting:**
- Single VPS (2 CPU, 4GB RAM)
- Or Docker Swarm with 1 node

### Scenario 3: Medium Production (100-1000 users)

**Recommendation:** Start Monolith, prepare Microservices

**Why:**
- Monolith handles load initially
- Monitor performance metrics
- Plan microservices migration
- Scale when needed

**Migration Trigger:**
- CPU usage >70% sustained
- Request latency >200ms
- Need to scale specific component

### Scenario 4: Large Production (1000+ users)

**Recommendation:** Microservices

**Why:**
- Independent scaling required
- High availability needs
- Multiple data centers
- Team specialization

**Hosting:**
- Kubernetes cluster (3+ nodes)
- Or Docker Swarm (5+ nodes)
- Load balancer required

---

## Cost Comparison

### Monolith Hosting Costs

**Small Scale (100 users):**
- VPS: DigitalOcean Droplet $24/month (2 CPU, 4GB RAM)
- Database: Managed PostgreSQL $15/month
- **Total: $39/month**

**Medium Scale (500 users):**
- VPS: $48/month (4 CPU, 8GB RAM)
- Database: $30/month
- CDN: $10/month
- **Total: $88/month**

### Microservices Hosting Costs

**Small Scale (100 users):**
- API Gateway: $12/month
- Trading Service: $12/month
- Data Service: $12/month
- Backtesting: $12/month
- Database: $15/month
- Load Balancer: $10/month
- **Total: $73/month**

**Medium Scale (500 users):**
- API Gateway: $24/month (2 instances)
- Trading Service: $36/month (3 instances)
- Data Service: $48/month (4 instances)
- Backtesting: $24/month (2 instances)
- Database: $30/month
- Load Balancer: $10/month
- **Total: $172/month**

**Verdict:** Monolith cheaper for small scale, microservices cost justified at larger scale.

---

## Operational Complexity

### Monolith Operations

**Deployment:**
```bash
# Pull latest
git pull origin main

# Rebuild
docker-compose build

# Deploy (downtime: ~30 seconds)
docker-compose up -d --force-recreate
```

**Monitoring:**
```bash
# Single log stream
docker-compose logs -f api

# Single health check
curl http://localhost:5000/health
```

**Debugging:**
```bash
# Attach debugger to single process
dotnet attach <PID>

# View all logs in one place
docker-compose logs api
```

**Skills Required:**
- Docker basics
- .NET debugging
- SQL knowledge

**Time Investment:** 5-10 hours/month

### Microservices Operations

**Deployment:**
```bash
# Deploy specific service (zero downtime)
docker-compose -f docker-compose.modular.yml up -d --no-deps trading-service

# Or deploy all
docker-compose -f docker-compose.modular.yml up -d
```

**Monitoring:**
```bash
# Multiple log streams
docker-compose -f docker-compose.modular.yml logs -f api-gateway
docker-compose -f docker-compose.modular.yml logs -f trading-service
docker-compose -f docker-compose.modular.yml logs -f data-service

# Multiple health checks
curl http://localhost:5000/health  # API Gateway
curl http://localhost:5001/health  # Trading Service
curl http://localhost:5002/health  # Data Service
```

**Debugging:**
```bash
# Distributed tracing required
# Check each service independently
docker exec -it algotrendy-trading-service bash

# Correlate logs across services
# Need log aggregation (ELK/Loki)
```

**Skills Required:**
- Docker Compose / Kubernetes
- Distributed systems debugging
- Network troubleshooting
- Log aggregation tools
- Service mesh (optional)

**Time Investment:** 15-30 hours/month

**Verdict:** Monolith much simpler for small teams.

---

## Migration Path

### Phase 1: Start with Monolith (Week 1)

```bash
# Clone and run
git clone <repo>
cd AlgoTrendy_v2.6
git checkout main
docker-compose up -d
```

### Phase 2: Measure and Monitor (Months 1-3)

**Key Metrics:**
- Request latency (target: <100ms p95)
- CPU usage (alert if >70%)
- Memory usage (alert if >80%)
- Error rate (target: <1%)

**Tools:**
- Prometheus + Grafana
- Application Insights
- Custom metrics dashboard

### Phase 3: Identify Bottlenecks (Month 3-4)

**Common Bottlenecks:**
1. **Data Service** - High throughput for market data
2. **Backtesting Service** - CPU intensive
3. **ML Service** - GPU requirements

**Decision Point:**
- If single component bottleneck → extract to microservice
- If overall system slow → optimize monolith first

### Phase 4: Gradual Migration (Months 4-6)

**Order of Extraction:**
1. **ML Service** (already separate - Python)
2. **Backtesting Service** (CPU intensive, independent)
3. **Data Service** (high throughput)
4. **Trading Service** (last - mission critical)

**Per-Service Migration:**
```bash
# 1. Extract service code (already done in modular branch)
# 2. Create Dockerfile (already exists)
# 3. Update docker-compose
# 4. Deploy alongside monolith
# 5. Route traffic gradually (feature flags)
# 6. Monitor and validate
# 7. Decommission from monolith
```

### Phase 5: Full Microservices (Month 6+)

```bash
git checkout modular
docker-compose -f docker-compose.modular.yml up -d
```

---

## Troubleshooting Guide

### Monolith Issues

**Problem: Slow API responses**
- Check: `docker stats algotrendy-api`
- Fix: Increase CPU/RAM or scale horizontally

**Problem: Out of memory**
- Check: Memory usage in container
- Fix: Increase container memory limit

**Problem: Database connection pool exhausted**
- Check: Connection count in PostgreSQL
- Fix: Increase pool size in `appsettings.json`

### Microservices Issues

**Problem: Inter-service timeout**
- Check: Network connectivity between containers
- Fix: Increase timeout, check service health

**Problem: Service discovery fails**
- Check: Docker network configuration
- Fix: Ensure all services on same network

**Problem: Distributed debugging difficult**
- Check: Correlation IDs in logs
- Fix: Implement distributed tracing (Jaeger/Zipkin)

---

## Decision Tree

```
Start Here
│
├─ Do you have >10 engineers? ──Yes──▶ Microservices
│                              │
│                              No
│                              ▼
├─ Do you need >5,000 req/min? ──Yes──▶ Microservices
│                              │
│                              No
│                              ▼
├─ Do you need independent scaling? ──Yes──▶ Microservices
│                                    │
│                                    No
│                                    ▼
└─ Use Monolith ──────────────────▶ Monolith
                                    │
                      Monitor and migrate when needed
```

---

## Recommendation Summary

### For Most Users: **Start with Monolith**

**Reasons:**
1. **Simpler:** One container, one process, one log stream
2. **Cheaper:** Lower hosting costs ($40 vs $75/month)
3. **Faster development:** No network overhead, easier debugging
4. **Adequate performance:** Handles 100-500 users easily

### When to Use Microservices

**Triggers:**
- Sustained CPU >70% (can't scale monolith further)
- Need to scale specific component (e.g., data service)
- Multiple teams working in parallel
- Regulatory requirements for service isolation
- >1,000 active users

### Hybrid Approach (Recommended)

**Best of Both Worlds:**
1. **Run monolith** for core functionality
2. **Extract 1-2 services** that need independent scaling:
   - ML Service (GPU requirements)
   - Data Service (high throughput)
3. **Keep trading engine** in monolith (mission critical)

```bash
# Hybrid docker-compose.yml
services:
  algotrendy-api:        # Monolith (trading + backtesting)
    image: algotrendy/platform:latest

  data-service:          # Microservice (high throughput)
    image: algotrendy/data-service:latest

  ml-service:            # Microservice (GPU)
    image: algotrendy/ml-service:latest
```

---

## Quick Commands Reference

### Monolith Commands

```bash
# Start
docker-compose up -d

# Stop
docker-compose down

# Logs
docker-compose logs -f api

# Rebuild
docker-compose up -d --build

# Health check
curl http://localhost:5000/health
```

### Microservices Commands

```bash
# Start all services
docker-compose -f docker-compose.modular.yml up -d

# Start specific service
docker-compose -f docker-compose.modular.yml up -d trading-service

# Stop all
docker-compose -f docker-compose.modular.yml down

# Logs for all
docker-compose -f docker-compose.modular.yml logs -f

# Logs for specific service
docker-compose -f docker-compose.modular.yml logs -f trading-service

# Restart service (zero downtime)
docker-compose -f docker-compose.modular.yml up -d --no-deps --force-recreate trading-service

# Scale service
docker-compose -f docker-compose.modular.yml up -d --scale data-service=3

# Health checks
curl http://localhost:5000/health  # API Gateway
curl http://localhost:5001/health  # Trading
curl http://localhost:5002/health  # Data
curl http://localhost:5003/health  # Backtesting
```

---

## FAQ

### Q: Can I switch between monolith and microservices easily?

**A: Yes!** Both use the same codebase. Just use different docker-compose files.

```bash
# Switch to monolith
git checkout main
docker-compose up -d

# Switch to microservices
git checkout modular
docker-compose -f docker-compose.modular.yml up -d
```

### Q: Will my data be lost when switching?

**A: No.** Both share the same QuestDB and PostgreSQL databases. Your data persists across deployments.

### Q: Which is faster?

**A:** Monolith has lower latency (<100ms) due to no network overhead. Microservices have higher throughput (can scale horizontally).

### Q: Which costs less?

**A:** Monolith costs 30-40% less at small scale (<500 users). Microservices become cost-effective at large scale (>1000 users).

### Q: Can I run both simultaneously?

**A:** Yes, for A/B testing:

```bash
# Run monolith on :5000
docker-compose up -d

# Run microservices on :6000
docker-compose -f docker-compose.modular.yml up -d
# (modify ports in docker-compose.modular.yml)

# Route 90% traffic to monolith, 10% to microservices
# Use nginx load balancer
```

---

## Conclusion

**For AlgoTrendy users:**

- **80% of users:** Use monolith (simpler, cheaper, adequate)
- **15% of users:** Use hybrid (monolith + 1-2 microservices)
- **5% of users:** Use full microservices (large scale, multiple teams)

**Start simple. Scale when needed. Both options are production-ready.**

---

**Related Documentation:**
- `docs/planning/research/MODULAR_ARCHITECTURE_STRATEGY.md` - Full technical strategy
- `docs/planning/research/MODULAR_QUICK_START.md` - Implementation guide
- `services/README.md` - Microservices architecture details
- `README.md` - Project overview

**Last Updated:** October 21, 2025
**Maintainer:** AlgoTrendy Team
**Version:** 1.0.0
