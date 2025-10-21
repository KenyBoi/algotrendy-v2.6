# AlgoTrendy Monitoring & Scaling Decision Guide

**Last Updated:** October 21, 2025
**Version:** 3.0.0
**For:** System Administrators and DevOps Teams

---

## 📋 Table of Contents

1. [Overview](#overview)
2. [Quick Start](#quick-start)
3. [System Health Dashboard](#system-health-dashboard)
4. [Understanding Metrics](#understanding-metrics)
5. [When to Switch Architectures](#when-to-switch-architectures)
6. [Setting Up Alerts](#setting-up-alerts)
7. [Prometheus + Grafana (Optional)](#prometheus--grafana-optional)
8. [Troubleshooting](#troubleshooting)

---

## Overview

AlgoTrendy v3.0 includes a **comprehensive monitoring system** designed to help you make data-driven decisions about when to switch from Monolith to Microservices architecture.

### What's Included

✅ **System Health Dashboard** - Real-time metrics visualization (frontend)
✅ **System Metrics API** - Programmatic access to health data
✅ **Scaling Decision Engine** - Automated recommendations
✅ **Prometheus + Grafana** - Optional advanced monitoring stack

### Key Metrics Tracked

| Metric | Threshold | Why It Matters |
|--------|-----------|----------------|
| **CPU Usage** | 70% warning, 85% critical | High CPU = need more compute power |
| **Memory Usage** | 80% warning, 90% critical | High memory = risk of out-of-memory errors |
| **API Latency (P95)** | 200ms warning, 500ms critical | High latency = poor user experience |
| **Request Rate** | 10,000 req/min | Above this, microservices recommended |
| **Error Rate** | 1% warning, 5% critical | High errors = application issues |

---

## Quick Start

### Step 1: Access the Dashboard

**Frontend Dashboard (Recommended):**
```bash
# Start AlgoTrendy
docker-compose up -d

# Start frontend
cd frontend
npm run dev

# Access dashboard
http://localhost:5173/system-health
```

**API Endpoints (Programmatic):**
```bash
# Get all metrics
curl http://localhost:5000/api/system-metrics

# Get health score (0-100)
curl http://localhost:5000/api/system-metrics/health-score

# Get scaling recommendation
curl http://localhost:5000/api/system-metrics/scaling-decision
```

### Step 2: Interpret the Recommendation

The dashboard shows a clear recommendation:

```
✅ STAY WITH MONOLITH
- All metrics within healthy ranges
- No action required
- Next review: 24 hours

OR

🚨 SWITCH TO HYBRID
- CPU at 78% (critical threshold: 70%)
- Data service under heavy load
- Action required: Extract data service
- Cost: +$24/mo
```

### Step 3: Act on Recommendations

Follow the steps in the recommendation card or refer to [DUAL_DEPLOYMENT_GUIDE.md](../../DUAL_DEPLOYMENT_GUIDE.md) for detailed migration instructions.

---

## System Health Dashboard

### Dashboard Sections

#### 1. Scaling Recommendation Card

**Location:** Top of dashboard
**What It Shows:** The most important information - should you switch?

**Example:**
```
Current: Monolith
Recommended: HYBRID
Confidence: 87%

Why: Data service CPU at 78% (sustained for 3 days)
Cost: +$24/mo (1 additional service)
Performance: -60% data service latency
Migration Time: 2 hours
```

**Colors:**
- 🟢 Green = Stay with monolith (healthy)
- 🟡 Yellow = Consider hybrid (some pressure)
- 🔴 Red = Switch to microservices (critical)

#### 2. Health Score

**Location:** Second section
**What It Shows:** Overall system health (0-100)

**Score Interpretation:**
- **80-100:** Excellent - System healthy
- **60-79:** Good - Minor issues, monitor
- **40-59:** Fair - Action needed soon
- **0-39:** Poor - Immediate action required

**Component Scores:**
- CPU, Memory, Latency, Error Rate
- Each scored independently

#### 3. Metric Cards

**Location:** Middle section
**What They Show:** Real-time metrics with visual indicators

**For Each Metric:**
- Current value
- Warning threshold (🟡)
- Critical threshold (🔴)
- Progress bar showing proximity to thresholds
- Status badge (OK/Warning/Critical)

#### 4. Additional Stats

**Location:** Bottom section
**What They Show:** Supporting metrics

- **Traffic:** Requests per minute
- **Error Rate:** Percentage of failed requests
- **Uptime:** How long the system has been running

---

## Understanding Metrics

### CPU Usage

**What:** Percentage of CPU capacity being used
**Warning:** 70% | **Critical:** 85%

**What It Means:**
- <50%: Plenty of headroom
- 50-70%: Normal under load
- 70-85%: Approaching limit, prepare to scale
- >85%: At capacity, scale now

**Actions:**
- <70%: No action
- 70-85%: Monitor closely, plan scaling
- >85%: Extract CPU-intensive service or add capacity

**Example:**
```
CPU: 45% → ✅ OK
CPU: 72% → 🟡 WARNING: Consider scaling soon
CPU: 88% → 🔴 CRITICAL: Scale immediately
```

### Memory Usage

**What:** Percentage of RAM being used
**Warning:** 80% | **Critical:** 90%

**What It Means:**
- <70%: Healthy
- 70-80%: Normal usage
- 80-90%: Nearing limit
- >90%: Risk of out-of-memory errors

**Actions:**
- <80%: No action
- 80-90%: Increase memory allocation or optimize
- >90%: Immediate action - add memory or scale

**Note:** Unlike CPU, you can't "borrow" memory. Once it's gone, the app crashes.

### API Latency (P95)

**What:** 95th percentile response time (95% of requests faster than this)
**Warning:** 200ms | **Critical:** 500ms

**What It Means:**
- <100ms: Excellent user experience
- 100-200ms: Good, acceptable for most uses
- 200-500ms: Degraded, users will notice
- >500ms: Poor, unacceptable

**Actions:**
- <200ms: No action
- 200-500ms: Optimize slow endpoints or scale
- >500ms: Immediate optimization or architectural change

**Why P95 vs Average?**
- Average hides outliers
- P95 shows what 95% of users experience
- Better indicator of actual performance

### Request Rate

**What:** Requests per minute
**Microservices Threshold:** 10,000 req/min

**What It Means:**
- <1,000: Monolith easily handles
- 1,000-10,000: Monolith sufficient with optimization
- >10,000: Microservices recommended for scaling

**Actions:**
- <10,000: Stay monolith
- >10,000: Consider microservices or hybrid

### Error Rate

**What:** Percentage of requests that fail
**Warning:** 1% | **Critical:** 5%

**What It Means:**
- <0.5%: Normal (some errors always occur)
- 0.5-1%: Slightly elevated, investigate
- 1-5%: Significant issues
- >5%: Critical application problems

**Actions:**
- <1%: Monitor error logs
- 1-5%: Investigate and fix errors
- >5%: Emergency - find and fix root cause

---

## When to Switch Architectures

### Decision Matrix

Use this table to quickly decide:

| Scenario | Recommendation | Action |
|----------|----------------|--------|
| All metrics green, <500 users | **Monolith** | No action, continue monitoring |
| 1 metric yellow, sustained | **Monitor** | Check daily, prepare scaling plan |
| 2+ metrics yellow | **Hybrid** | Extract bottleneck service |
| 1 metric red | **Hybrid** | Extract affected service immediately |
| 2+ metrics red | **Microservices** | Full migration, urgent |
| >10K req/min | **Microservices** | Need independent scaling |

### Real Examples

#### Example 1: All Green
```
CPU: 45% ✅
Memory: 62% ✅
Latency: 120ms ✅
Error Rate: 0.3% ✅

Decision: STAY WITH MONOLITH
Action: None required
Next review: 24 hours
```

#### Example 2: CPU Warning
```
CPU: 73% 🟡
Memory: 58% ✅
Latency: 150ms ✅
Error Rate: 0.5% ✅

Decision: MONITOR CLOSELY
Action: Check CPU usage trends
Plan: Prepare to extract CPU-intensive service
Next review: 12 hours
```

#### Example 3: Multiple Warnings
```
CPU: 75% 🟡
Memory: 82% 🟡
Latency: 210ms 🟡
Error Rate: 0.4% ✅

Decision: SWITCH TO HYBRID
Action: Extract data service (likely bottleneck)
Cost: +$24/mo
Performance gain: 30-40%
Migration time: 2-4 hours
```

#### Example 4: Critical State
```
CPU: 88% 🔴
Memory: 91% 🔴
Latency: 520ms 🔴
Error Rate: 3.2% 🟡

Decision: SWITCH TO MICROSERVICES
Action: Full migration ASAP
Cost: +$150/mo
Performance gain: 60-80%
Migration time: 4-8 hours
```

---

## Setting Up Alerts

### Option 1: Email Alerts (Simple)

Create a cron job to check metrics and send email:

```bash
# /etc/cron.d/algotrendy-alerts
*/15 * * * * root /opt/algotrendy/scripts/check-health.sh

# /opt/algotrendy/scripts/check-health.sh
#!/bin/bash
RESPONSE=$(curl -s http://localhost:5000/api/system-metrics/scaling-decision)
ACTION_REQUIRED=$(echo $RESPONSE | jq -r '.actionRequired')

if [ "$ACTION_REQUIRED" == "true" ]; then
  RECOMMENDATION=$(echo $RESPONSE | jq -r '.recommendation')
  REASONS=$(echo $RESPONSE | jq -r '.reasons | join(", ")')

  echo "AlgoTrendy Alert: Action Required

Recommendation: $RECOMMENDATION
Reasons: $REASONS

Dashboard: http://localhost:5173/system-health
  " | mail -s "AlgoTrendy: Scaling Action Required" admin@yourcompany.com
fi
```

### Option 2: Slack Alerts

Use webhook integration:

```bash
#!/bin/bash
WEBHOOK_URL="https://hooks.slack.com/services/YOUR/WEBHOOK/URL"
RESPONSE=$(curl -s http://localhost:5000/api/system-metrics/scaling-decision)
ACTION_REQUIRED=$(echo $RESPONSE | jq -r '.actionRequired')

if [ "$ACTION_REQUIRED" == "true" ]; then
  RECOMMENDATION=$(echo $RESPONSE | jq -r '.recommendation')
  CONFIDENCE=$(echo $RESPONSE | jq -r '.confidence')
  COST=$(echo $RESPONSE | jq -r '.estimatedCostImpact')

  curl -X POST $WEBHOOK_URL -H 'Content-Type: application/json' -d "{
    \"text\": \"🚨 AlgoTrendy Scaling Alert\",
    \"attachments\": [{
      \"color\": \"warning\",
      \"fields\": [
        {\"title\": \"Recommendation\", \"value\": \"$RECOMMENDATION\", \"short\": true},
        {\"title\": \"Confidence\", \"value\": \"${CONFIDENCE}%\", \"short\": true},
        {\"title\": \"Cost Impact\", \"value\": \"$COST\", \"short\": false}
      ]
    }]
  }"
fi
```

### Option 3: Grafana Alerts

See [Prometheus + Grafana](#prometheus--grafana-optional) section below.

---

## Prometheus + Grafana (Optional)

For advanced monitoring with historical trends and custom dashboards.

### Why Use Prometheus + Grafana?

**Prometheus:**
- Time-series metrics storage
- Query historical data
- Trend analysis over days/weeks

**Grafana:**
- Beautiful visual dashboards
- Built-in alerting
- Pre-built dashboards

### Setup

```bash
# Start monitoring stack
docker-compose -f docker-compose.yml -f docker-compose.monitoring.yml up -d

# Access:
# Grafana: http://localhost:3000 (admin/admin)
# Prometheus: http://localhost:9090
```

### Pre-configured Dashboards

1. **When to Switch to Microservices**
   - Shows all key metrics with thresholds
   - Scaling recommendation over time
   - Cost comparison calculator

2. **System Resources**
   - CPU, memory, disk usage trends
   - 24-hour, 7-day, 30-day views
   - Anomaly detection

3. **API Performance**
   - Request rate over time
   - Latency by endpoint
   - Error rate trends

### Creating Custom Alerts

In Grafana:
1. Go to Alerting → Alert rules
2. Create new alert rule
3. Query: `node_cpu_seconds_total > 70`
4. Set notification channel (email/Slack)
5. Save

---

## Troubleshooting

### Dashboard Not Loading

**Problem:** Frontend dashboard shows error
**Check:**
```bash
# Is API running?
curl http://localhost:5000/health

# Check API logs
docker-compose logs api
```

**Solution:**
```bash
# Restart API
docker-compose restart api
```

### Metrics Showing 0 or N/A

**Problem:** Metrics not being collected
**Check:**
```bash
# Check if metrics middleware is enabled
curl http://localhost:5000/api/metrics
```

**Solution:** Metrics should populate after a few requests. Make some API calls to generate data.

### Scaling Recommendation Not Updating

**Problem:** Recommendation stays same despite changing metrics
**Cause:** API caches results for 5 minutes
**Solution:** Wait 5 minutes or restart API to force refresh

### Prometheus Not Scraping Metrics

**Problem:** No data in Grafana
**Check:**
```bash
# Check Prometheus targets
http://localhost:9090/targets

# Should show:
# algotrendy-api (UP)
# node-exporter (UP)
# cadvisor (UP)
```

**Solution:**
```bash
# Check network connectivity
docker-compose -f docker-compose.monitoring.yml exec prometheus ping api

# Restart Prometheus
docker-compose -f docker-compose.monitoring.yml restart prometheus
```

---

## Best Practices

### Daily Monitoring

✅ Check dashboard once daily
✅ Review trends in Grafana weekly
✅ Document any scaling decisions
✅ Test alerts monthly

### Before Scaling

✅ Confirm sustained metrics (not just spike)
✅ Review cost impact
✅ Read migration guide
✅ Plan maintenance window
✅ Backup data

### After Scaling

✅ Monitor for 24 hours
✅ Compare performance before/after
✅ Document lessons learned
✅ Update runbooks

---

## Quick Reference

### Endpoints

```
GET /api/system-metrics          → All metrics
GET /api/system-metrics/health-score  → 0-100 score
GET /api/system-metrics/scaling-decision  → Recommendation
GET /api/system-metrics/trend    → 24h trends
```

### Thresholds

```
CPU:     70% ⚠️   85% 🔴
Memory:  80% ⚠️   90% 🔴
Latency: 200ms ⚠️  500ms 🔴
Errors:  1% ⚠️    5% 🔴
```

### Actions

```
All Green → No action
1 Yellow → Monitor closely
2+ Yellow → Plan migration
1 Red → Extract service
2+ Red → Urgent migration
```

---

## Support

**Documentation:**
- [DUAL_DEPLOYMENT_GUIDE.md](../../DUAL_DEPLOYMENT_GUIDE.md) - Complete deployment guide
- [MODULAR_VS_MONOLITH.md](../../MODULAR_VS_MONOLITH.md) - Architecture comparison

**Need Help?**
- Check logs: `docker-compose logs api`
- Review errors: `curl http://localhost:5000/api/metrics`
- Open issue on GitHub

---

**Version:** 1.0.0
**Last Updated:** October 21, 2025
**Maintained By:** AlgoTrendy Team
