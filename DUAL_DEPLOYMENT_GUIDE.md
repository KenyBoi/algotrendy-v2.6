# AlgoTrendy v3.0 Dual-Deployment Architecture Guide

**Last Updated:** October 21, 2025
**Version:** 3.0.0
**Read Time:** 20 minutes

---

## 📋 Table of Contents

1. [Understanding Dual Architecture](#1-understanding-dual-architecture)
2. [When to Use Each Mode](#2-when-to-use-each-mode)
3. [Getting Started Fast](#3-getting-started-fast)
4. [Performance Optimization](#4-performance-optimization)
5. [Real-World Use Cases](#5-real-world-use-cases)
6. [Migration Strategies](#6-migration-strategies)
7. [Monitoring & Debugging](#7-monitoring--debugging)
8. [Cost Analysis](#8-cost-analysis)
9. [Advanced Patterns](#9-advanced-patterns)
10. [Quick Reference](#10-quick-reference)

---

## 1. Understanding Dual Architecture

### What Is Dual Architecture?

AlgoTrendy v3.0 introduces a **revolutionary approach**: **same codebase, two deployment options**.

```
┌─────────────────────────────────────┐
│   AlgoTrendy v3.0 Codebase          │
│   (backend/ directory)              │
└──────────┬──────────────┬───────────┘
           │              │
           ▼              ▼
    ┌──────────┐   ┌──────────────┐
    │ Monolith │   │ Microservices│
    │ Mode     │   │ Mode         │
    └──────────┘   └──────────────┘
```

**Key Insight:** You don't have to choose one architecture forever. You can:
- Start simple (monolith)
- Scale when needed (microservices)
- Switch between them anytime
- Run both simultaneously for A/B testing

### How It Works Technically

**Same Code, Different Packaging:**

| Component | Monolith | Microservices |
|-----------|----------|---------------|
| **Source Code** | `backend/AlgoTrendy.API` | `backend/AlgoTrendy.API` |
| **Trading Engine** | `backend/AlgoTrendy.TradingEngine` | `backend/AlgoTrendy.TradingEngine` |
| **Data Channels** | `backend/AlgoTrendy.DataChannels` | `backend/AlgoTrendy.DataChannels` |
| **Backtesting** | `backend/AlgoTrendy.Backtesting` | `backend/AlgoTrendy.Backtesting` |

**The Magic:**
- **Monolith**: All modules loaded in single process (1 container)
- **Microservices**: Modules distributed across services (4+ containers)
- **Code**: IDENTICAL (you develop in one place)

### Key Benefits

✅ **Flexibility** - Change deployment without code changes
✅ **Safety** - Start simple, scale when proven
✅ **Cost-Effective** - Pay for complexity only when needed
✅ **Future-Proof** - Architecture choice is deployment-time, not design-time
✅ **Risk Reduction** - Test microservices without commitment

---

## 2. When to Use Each Mode

### Decision Matrix

| Factor | Monolith | Microservices |
|--------|----------|---------------|
| **Team Size** | 1-5 developers | 5+ developers |
| **Traffic** | <1,000 req/min | >10,000 req/min |
| **Active Users** | <500 | >1,000 |
| **DevOps Team** | None/Small | Dedicated team |
| **Budget** | $40-100/month | $150-500/month |
| **Deployment Frequency** | Weekly/Monthly | Daily/Continuous |
| **Geographic Distribution** | Single region | Multi-region |
| **Development Speed** | 🟢 Fast | 🟡 Moderate |
| **Operational Complexity** | 🟢 Low | 🔴 High |
| **Scaling Needs** | 🟡 Vertical only | 🟢 Horizontal |

### Choose Monolith If:

✅ You're **starting out** or in **MVP stage**
✅ Your team is **small** (1-5 people)
✅ You need **fast development cycles**
✅ You want **simple deployment** (one command)
✅ Your traffic is **predictable and moderate**
✅ You have **limited DevOps experience**
✅ Cost optimization is a **priority**

**Example:** Solo developer building and testing strategies, small hedge fund with <100 users

### Choose Microservices If:

✅ You have **high traffic** (>10K requests/min)
✅ You need **independent scaling** (e.g., data service under heavy load)
✅ You have **multiple teams** working in parallel
✅ You require **geographic distribution** (latency optimization)
✅ Different services have **different resource needs** (GPU for ML, high CPU for backtesting)
✅ You want **fault isolation** (one service failure doesn't crash entire system)
✅ You're deploying to **Kubernetes/cloud-native infrastructure**

**Example:** Trading firm with global users, high-frequency trading platform, multi-asset hedge fund

### The Hybrid Approach (Recommended for Many)

**Best of Both Worlds:**

```
┌──────────────────────────────────────────┐
│  Core Trading (Monolith)                 │  ← Mission-critical, proven
│  - Order execution                       │
│  - Risk management                       │
│  - Portfolio tracking                    │
└──────────────────────────────────────────┘

┌──────────────┐  ┌──────────────┐
│ Data Service │  │ ML Service   │  ← High-load, independent
│ (Microservice│  │ (Microservice│
└──────────────┘  └──────────────┘
```

**When to Use Hybrid:**
- You're **transitioning** from monolith to microservices
- **One component** is a bottleneck (extract just that one)
- You want to **test** microservices without full commitment
- You need **specialized resources** (GPU for ML only)

---

## 3. Getting Started Fast

### Monolith Deployment (5 Minutes)

**Step 1: Clone and prepare**
```bash
git clone <repo-url>
cd AlgoTrendy_v2.6
git checkout main
```

**Step 2: Configure environment**
```bash
cp .env.example .env
# Edit .env with your credentials
nano .env
```

**Step 3: Start services**
```bash
docker-compose up -d
```

**Step 4: Verify**
```bash
# Check services
docker-compose ps

# View logs
docker-compose logs -f api

# Test API
curl http://localhost:5000/health
```

**Access:**
- API: http://localhost:5000
- Frontend: http://localhost:5173 (after `cd frontend && npm run dev`)
- QuestDB Console: http://localhost:9000

### Microservices Deployment (10 Minutes)

**Step 1: Same preparation**
```bash
git clone <repo-url>
cd AlgoTrendy_v2.6
git checkout main
cp .env.example .env
nano .env
```

**Step 2: Start microservices**
```bash
docker-compose -f docker-compose.modular.yml up -d
```

**Step 3: Verify all services**
```bash
# Check all services
docker-compose -f docker-compose.modular.yml ps

# Health checks
curl http://localhost:5000/health  # API Gateway
curl http://localhost:5001/health  # Trading Service
curl http://localhost:5002/health  # Data Service
curl http://localhost:5003/health  # Backtesting Service

# View specific service logs
docker-compose -f docker-compose.modular.yml logs -f trading-service
```

**Service Ports:**
- API Gateway: 5000
- Trading Service: 5001 (internal)
- Data Service: 5002 (internal)
- Backtesting Service: 5003 (internal)

### Switching Between Modes

**Monolith → Microservices:**
```bash
# Stop monolith
docker-compose down

# Start microservices
docker-compose -f docker-compose.modular.yml up -d
```

**Microservices → Monolith:**
```bash
# Stop microservices
docker-compose -f docker-compose.modular.yml down

# Start monolith
docker-compose up -d
```

**Your data is safe!** Both modes use the same QuestDB and PostgreSQL databases. No data loss when switching.

---

## 4. Performance Optimization

### Monolith Performance Tuning

#### 1. Memory Optimization

**File: `docker-compose.yml`**
```yaml
services:
  api:
    deploy:
      resources:
        limits:
          memory: 2G  # Adjust based on load
        reservations:
          memory: 1G
```

**Application Settings (`appsettings.json`):**
```json
{
  "Kestrel": {
    "Limits": {
      "MaxConcurrentConnections": 100,
      "MaxConcurrentUpgradedConnections": 100,
      "MaxRequestBodySize": 30000000
    }
  }
}
```

#### 2. Database Connection Pooling

```json
{
  "ConnectionStrings": {
    "QuestDB": "host=questdb;port=8812;database=algotrendy;Pooling=true;MinPoolSize=5;MaxPoolSize=50"
  }
}
```

#### 3. Caching Strategy

Redis caching for market data:
```csharp
// Enabled by default in v3.0
// Market data cached for 5 seconds
// Strategy results cached for 1 minute
```

#### 4. Horizontal Scaling (Multiple Instances)

```bash
# Run 3 instances behind nginx load balancer
docker-compose up -d --scale api=3
```

**Note:** Requires session affinity or stateless design.

### Microservices Performance Tuning

#### 1. Service-Specific Scaling

```bash
# Scale only data service (high throughput needed)
docker-compose -f docker-compose.modular.yml up -d --scale data-service=5

# Scale trading service moderately
docker-compose -f docker-compose.modular.yml up -d --scale trading-service=3

# Keep others at 1 instance
```

#### 2. Resource Allocation Per Service

**File: `docker-compose.modular.yml`**
```yaml
services:
  data-service:
    deploy:
      resources:
        limits:
          cpus: '2.0'
          memory: 2G
        reservations:
          cpus: '1.0'
          memory: 1G

  backtesting-service:
    deploy:
      resources:
        limits:
          cpus: '4.0'  # CPU-intensive
          memory: 4G
        reservations:
          cpus: '2.0'
          memory: 2G
```

#### 3. Load Balancing

Using nginx or Traefik for intelligent routing:

```nginx
upstream data-service {
    least_conn;  # Route to least-loaded instance
    server data-service-1:5002;
    server data-service-2:5002;
    server data-service-3:5002;
}
```

#### 4. Circuit Breakers (Fault Tolerance)

Implemented using Polly in .NET:
```csharp
// Already configured in v3.0
// Retry 3 times with exponential backoff
// Circuit breaker opens after 5 failures
```

---

## 5. Real-World Use Cases

### Use Case 1: Solo Developer / Small Team

**Scenario:** You're building AlgoTrendy strategies, testing on your laptop.

**Recommendation:** **Monolith**

**Setup:**
```bash
docker-compose up -d
cd frontend && npm run dev
```

**Why:**
- Single container uses minimal resources (1GB RAM)
- Fast startup (<30 seconds)
- Easy debugging (single log stream)
- Low complexity

**Performance:** Handles 50-100 req/min easily.

---

### Use Case 2: Small Hedge Fund (10-50 Users)

**Scenario:** Small fund managing $1M-$10M, 10-50 active traders.

**Recommendation:** **Monolith with scaling**

**Setup:**
```bash
# Production VPS (4 CPU, 8GB RAM)
docker-compose -f docker-compose.prod.yml up -d
```

**Optimizations:**
- Enable Redis caching
- Connection pooling (50 connections)
- Daily backups
- Monitoring (Prometheus + Grafana)

**Cost:** ~$80/month (VPS + managed DB)

**Performance:** Handles 500-1000 req/min.

---

### Use Case 3: Medium Hedge Fund (100-500 Users)

**Scenario:** Fund managing $50M+, multiple strategies, global users.

**Recommendation:** **Hybrid (Monolith + Selected Microservices)**

**Setup:**
```yaml
# Monolith for core trading
services:
  trading-core:
    image: algotrendy/monolith:3.0

# Microservices for high-load components
  data-service:
    image: algotrendy/data-service:3.0
    replicas: 3

  ml-service:
    image: algotrendy/ml-service:3.0
    resources:
      limits:
        nvidia.com/gpu: 1  # GPU for ML
```

**Why Hybrid:**
- Trading engine stays monolithic (proven, simple)
- Data service scales independently (high throughput)
- ML service gets GPU resources

**Cost:** ~$300-400/month

**Performance:** Handles 5,000-10,000 req/min.

---

### Use Case 4: Large Firm / High-Frequency Trading

**Scenario:** 1000+ users, multiple regions, latency-critical.

**Recommendation:** **Full Microservices with Multi-Region**

**Architecture:**
```
Region: US-East (New Jersey)
├── api-gateway (3 replicas)
├── trading-service (5 replicas)
├── data-service (10 replicas)
└── backtesting-service (2 replicas)

Region: US-West (Chicago - Futures)
├── trading-service (5 replicas)
└── data-service (5 replicas)

Region: CDMX (Mexico - Crypto)
├── trading-service (3 replicas)
└── data-service (3 replicas)
```

**Deployment:** Kubernetes cluster

**Cost:** ~$1,500-3,000/month

**Performance:** Handles 50,000+ req/min globally.

**Latency:**
- NJ → NYSE: 10-15ms
- Chicago → CME: 8-12ms
- CDMX → Crypto exchanges: 30-50ms

---

## 6. Migration Strategies

### Strategy 1: Start Simple, Scale When Needed

**Phase 1: Monolith (Month 1-6)**
```bash
docker-compose up -d
```

**Monitor these metrics:**
- CPU usage (alert at >70%)
- Memory usage (alert at >80%)
- Request latency (p95 >200ms = problem)
- Error rate (>1% = problem)

**Phase 2: Identify Bottleneck (Month 6-7)**

Run profiling:
```bash
# Check which component uses most resources
docker stats algotrendy-api

# Analyze logs for slow endpoints
docker-compose logs api | grep "took.*ms" | sort -n
```

**Common bottlenecks:**
1. **Data Service** - Market data streaming (high I/O)
2. **Backtesting Service** - CPU-intensive calculations
3. **ML Service** - GPU requirements

**Phase 3: Extract First Microservice (Month 7-9)**

Extract the bottleneck component:

```bash
# 1. Deploy data service as microservice
docker-compose -f docker-compose.hybrid.yml up -d

# hybrid.yml example:
services:
  monolith:
    # All components except data

  data-service:
    # Standalone data service
    replicas: 3
```

**Phase 4: Measure Impact (Month 9-10)**

Compare metrics:
- Throughput increased?
- Latency reduced?
- Costs acceptable?

**Phase 5: Iterate or Complete (Month 10+)**

- If successful: Extract next bottleneck
- If not: Revert to monolith, optimize differently

### Strategy 2: Big Bang Migration

**When to use:** You have downtime window, small user base, or staging environment.

**Steps:**
```bash
# 1. Test microservices in staging
docker-compose -f docker-compose.modular.yml up -d

# 2. Run full test suite
cd backend && dotnet test

# 3. Load test
k6 run loadtest.js

# 4. If all pass, schedule migration
# 5. Maintenance window: switch docker-compose files
# 6. Monitor closely for 24 hours
```

**Risk:** Higher (all-or-nothing)
**Speed:** Faster (1-2 weeks)

---

## 7. Monitoring & Debugging

### Monolith Monitoring

#### Basic Health Check
```bash
curl http://localhost:5000/health
```

#### View Logs
```bash
# All logs
docker-compose logs -f api

# Last 100 lines
docker-compose logs --tail=100 api

# Filter by error
docker-compose logs api | grep ERROR
```

#### Resource Usage
```bash
docker stats algotrendy-api
```

#### Debugging
```bash
# Attach to container
docker exec -it algotrendy-api bash

# Check running processes
ps aux

# View environment
env | grep ALGO
```

### Microservices Monitoring

#### Distributed Tracing (Recommended: Jaeger)

**Setup:**
```yaml
# Add to docker-compose.modular.yml
services:
  jaeger:
    image: jaegertracing/all-in-one:latest
    ports:
      - "16686:16686"  # UI
      - "14268:14268"  # Collector
```

**Access:** http://localhost:16686

**Trace a request:**
```
Client → API Gateway → Trading Service → Broker
   |         |              |
   └─────────┴──────────────┴───→ Jaeger (Trace ID: abc123)
```

#### Centralized Logging (Recommended: ELK Stack)

**Setup:**
```bash
# Quick setup with Docker
docker run -d --name elasticsearch -p 9200:9200 -e "discovery.type=single-node" elasticsearch:8.11.0
docker run -d --name kibana -p 5601:5601 --link elasticsearch kibana:8.11.0
docker run -d --name logstash -p 5044:5044 --link elasticsearch logstash:8.11.0
```

**View logs:** http://localhost:5601

#### Per-Service Health Checks
```bash
# Check all services
for service in api-gateway trading-service data-service backtesting-service; do
  echo "Checking $service..."
  docker-compose -f docker-compose.modular.yml exec $service curl -s http://localhost:$(echo $service | grep -oP '\d+' || echo 5000)/health
done
```

#### Service-Specific Logs
```bash
# Trading service only
docker-compose -f docker-compose.modular.yml logs -f trading-service

# Multiple services
docker-compose -f docker-compose.modular.yml logs -f trading-service data-service
```

---

## 8. Cost Analysis

### Monolith Costs

#### Small Scale (100 users, 500 req/min)

| Item | Provider | Cost |
|------|----------|------|
| VPS (2 CPU, 4GB RAM) | DigitalOcean | $24/mo |
| Managed PostgreSQL | DigitalOcean | $15/mo |
| Backups (20GB) | S3/Backblaze | $5/mo |
| Domain + SSL | Namecheap | $2/mo |
| **Total** | | **$46/mo** |

#### Medium Scale (500 users, 2K req/min)

| Item | Provider | Cost |
|------|----------|------|
| VPS (4 CPU, 8GB RAM) | DigitalOcean | $48/mo |
| Managed PostgreSQL | DigitalOcean | $30/mo |
| CDN | Cloudflare | $20/mo |
| Backups | S3 | $10/mo |
| Monitoring | Datadog Free | $0/mo |
| **Total** | | **$108/mo** |

### Microservices Costs

#### Small Scale (100 users, 500 req/min)

| Item | Instances | Unit Cost | Total |
|------|-----------|-----------|-------|
| API Gateway | 1 | $12/mo | $12 |
| Trading Service | 1 | $12/mo | $12 |
| Data Service | 2 | $12/mo | $24 |
| Backtesting Service | 1 | $12/mo | $12 |
| PostgreSQL | 1 | $15/mo | $15 |
| Redis | 1 | $10/mo | $10 |
| Load Balancer | 1 | $10/mo | $10 |
| **Total** | | | **$95/mo** |

**Verdict:** Monolith cheaper at small scale ($46 vs $95)

#### Medium Scale (500 users, 5K req/min)

| Item | Instances | Unit Cost | Total |
|------|-----------|-----------|-------|
| API Gateway | 2 | $24/mo | $48 |
| Trading Service | 3 | $24/mo | $72 |
| Data Service | 5 | $24/mo | $120 |
| Backtesting Service | 2 | $24/mo | $48 |
| PostgreSQL | 1 | $30/mo | $30 |
| Redis | 1 | $20/mo | $20 |
| Load Balancer | 1 | $10/mo | $10 |
| Monitoring | 1 | $50/mo | $50 |
| **Total** | | | **$398/mo** |

**Monolith at same scale:** $108/mo (but likely struggling)

**Verdict:** Microservices more expensive but necessary for performance.

### Cost Breakeven Point

```
         Cost
          │
$400     │                           ╱ Microservices
         │                      ╱
         │                 ╱
$200     │            ╱
         │       ╱
         │   Monolith
$100     │──────────────────────────
         │
         └──────────────────────────▶ Users
         0    100    500    1000   2000

Breakeven: ~800 users
Below 800: Monolith cheaper
Above 800: Microservices more cost-effective (performance/cost ratio)
```

---

## 9. Advanced Patterns

### Pattern 1: Blue-Green Deployment with Architecture Switching

**Scenario:** Zero-downtime migration from monolith to microservices.

**Setup:**
```
Blue (Production): Monolith on port 80
Green (Staging): Microservices on port 8080

Load Balancer:
├── 100% traffic → Blue (Monolith)
└── 0% traffic → Green (Microservices)
```

**Migration:**
```bash
# 1. Deploy green (microservices)
docker-compose -f docker-compose.modular.yml up -d

# 2. Health check green
curl http://localhost:8080/health

# 3. Route 10% traffic to green
# (Update load balancer config)

# 4. Monitor for 1 hour
# If successful, increase to 50%, then 100%

# 5. Decommission blue
docker-compose down
```

### Pattern 2: A/B Testing Architectures

**Scenario:** Test microservices performance with real users.

**Setup:**
```nginx
# nginx.conf
upstream backend {
    # 90% to monolith
    server monolith:5000 weight=9;

    # 10% to microservices
    server api-gateway:5000 weight=1;
}
```

**Measure:**
- Latency (monolith vs microservices)
- Error rates
- User satisfaction

**Decision:** If microservices shows >20% performance improvement, migrate fully.

### Pattern 3: Geographic Distribution (Multi-Region)

**Scenario:** Optimize latency for global users.

**Architecture:**
```
User (New York)
│
├─→ If trading US stocks
│   └─→ New Jersey VPS (Monolith)
│       └─→ 10-15ms to NYSE
│
├─→ If trading futures
│   └─→ Chicago VPS (Microservices - Trading Service only)
│       └─→ 8-12ms to CME
│
└─→ If trading crypto
    └─→ CDMX VPS (Microservices - Data Service + Trading)
        └─→ 30-50ms to Binance
```

**Routing:** GeoDNS or intelligent API gateway.

### Pattern 4: Hybrid Cloud (Cost Optimization)

**Scenario:** Use cheapest infrastructure for each component.

```
On-Premise (Existing Hardware):
└─→ Backtesting Service (CPU-intensive, not latency-critical)

Cloud (AWS/DigitalOcean):
├─→ Trading Service (low latency required)
├─→ Data Service (high bandwidth)
└─→ API Gateway (public-facing)

Edge (Cloudflare Workers):
└─→ Static frontend caching
```

**Cost Savings:** 40-60% vs full cloud.

---

## 10. Quick Reference

### Command Cheat Sheet

#### Monolith
```bash
# Start
docker-compose up -d

# Stop
docker-compose down

# Restart
docker-compose restart api

# Logs
docker-compose logs -f api

# Update
git pull && docker-compose up -d --build

# Scale (multiple instances)
docker-compose up -d --scale api=3
```

#### Microservices
```bash
# Start all
docker-compose -f docker-compose.modular.yml up -d

# Start specific service
docker-compose -f docker-compose.modular.yml up -d trading-service

# Stop all
docker-compose -f docker-compose.modular.yml down

# Restart service (zero downtime)
docker-compose -f docker-compose.modular.yml up -d --no-deps --force-recreate trading-service

# Scale specific service
docker-compose -f docker-compose.modular.yml up -d --scale data-service=5

# Logs (all services)
docker-compose -f docker-compose.modular.yml logs -f

# Logs (specific service)
docker-compose -f docker-compose.modular.yml logs -f trading-service
```

### Decision Tree

```
START: Choose Deployment Architecture
│
├─→ Q: Is this for local development?
│   ├─→ YES → Use Monolith
│   └─→ NO  → Continue
│
├─→ Q: Do you have <500 users?
│   ├─→ YES → Use Monolith
│   └─→ NO  → Continue
│
├─→ Q: Do you have dedicated DevOps team?
│   ├─→ NO  → Use Monolith
│   └─→ YES → Continue
│
├─→ Q: Do you need >10K requests/min?
│   ├─→ NO  → Use Monolith (for now)
│   └─→ YES → Continue
│
├─→ Q: Do you need geographic distribution?
│   ├─→ NO  → Use Monolith or Hybrid
│   └─→ YES → Use Microservices
│
└─→ Q: Is one specific component a bottleneck?
    ├─→ NO  → Use Monolith
    └─→ YES → Use Hybrid (extract that component)
```

### Troubleshooting Common Issues

#### Monolith

**Problem:** High memory usage
```bash
# Check memory
docker stats algotrendy-api

# Solution: Increase memory limit
# In docker-compose.yml:
# mem_limit: 2g
```

**Problem:** Slow API responses
```bash
# Check CPU
docker stats algotrendy-api

# Solution: Scale horizontally or upgrade VPS
```

#### Microservices

**Problem:** Service discovery fails
```bash
# Check network
docker network inspect algotrendy-network

# Check service names resolve
docker-compose -f docker-compose.modular.yml exec api-gateway ping trading-service
```

**Problem:** Inter-service timeout
```bash
# Check health of target service
curl http://localhost:5001/health

# Increase timeout in code or env vars
# TRADING_SERVICE_TIMEOUT=60000
```

### Monitoring URLs

**Monolith:**
- API: http://localhost:5000
- Health: http://localhost:5000/health
- Swagger: http://localhost:5000/swagger

**Microservices:**
- API Gateway: http://localhost:5000
- Gateway Health: http://localhost:5000/health
- Trading Health: http://localhost:5001/health (container network only)
- Data Health: http://localhost:5002/health (container network only)

**Shared:**
- QuestDB Console: http://localhost:9000
- Frontend: http://localhost:5173

---

## 🎯 Recommendations Summary

| Your Situation | Recommended Architecture |
|----------------|-------------------------|
| Solo developer, learning | **Monolith** |
| Small team (<5), MVP | **Monolith** |
| Small hedge fund (<100 users) | **Monolith** |
| Medium fund (100-500 users) | **Hybrid** (Monolith + Data/ML microservices) |
| Large firm (500-1000 users) | **Microservices** |
| Enterprise (1000+ users) | **Microservices + Multi-region** |
| High-frequency trading | **Microservices + Edge deployment** |

**The Golden Rule:** Start with monolith, migrate to microservices when you measure the need (not when you assume it).

---

## 📚 Additional Resources

- **Architecture Comparison:** `MODULAR_VS_MONOLITH.md`
- **Migration Plan:** `MIGRATION_TO_V3_PLAN.md`
- **Microservices Details:** `services/README.md`
- **Deployment Guide:** `docs/deployment/DEPLOYMENT_GUIDE.md`

---

**Questions?** Check `docs/ai-context/KNOWN_ISSUES_DATABASE.md` or open an issue.

**Last Updated:** October 21, 2025
**Maintainer:** AlgoTrendy Team
**Version:** 1.0.0
