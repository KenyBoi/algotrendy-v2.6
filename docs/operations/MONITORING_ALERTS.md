# Monitoring and Alerting Configuration

## Overview

AlgoTrendy includes comprehensive monitoring and alerting to ensure system health and rapid incident response.

## Monitoring Stack

### Components

1. **Serilog** - Structured logging
2. **Seq** - Log aggregation and analysis
3. **Metrics API** - Application metrics
4. **Health Checks** - Service health monitoring
5. **Webhooks** - Alert notifications

## Alert Configuration

### appsettings.json

```json
{
  "Alerts": {
    "Enabled": true,
    "Channels": {
      "Discord": "${DISCORD_WEBHOOK_URL}",
      "Slack": "${SLACK_WEBHOOK_URL}",
      "Email": "${ALERT_EMAIL}"
    },
    "Rules": [
      {
        "Name": "HighErrorRate",
        "Condition": "ErrorRate > 5%",
        "Severity": "Critical",
        "CooldownMinutes": 15
      },
      {
        "Name": "SlowRequests",
        "Condition": "P95Latency > 2000ms",
        "Severity": "Warning",
        "CooldownMinutes": 30
      },
      {
        "Name": "OrderFailures",
        "Condition": "OrderFailureRate > 10%",
        "Severity": "Critical",
        "CooldownMinutes": 5
      }
    ]
  }
}
```

### Environment Variables

```bash
# Discord webhook for alerts
export DISCORD_WEBHOOK_URL="https://discord.com/api/webhooks/..."

# Slack webhook for alerts
export SLACK_WEBHOOK_URL="https://hooks.slack.com/services/..."

# Email for critical alerts
export ALERT_EMAIL="ops@algotrendy.com"

# Alert thresholds
export ERROR_RATE_THRESHOLD="5"  # percent
export LATENCY_THRESHOLD="2000"  # milliseconds
```

## Alert Types

### 1. Error Rate Alerts

Triggered when error rate exceeds threshold.

**Condition:** `(Errors / TotalRequests) * 100 > 5%`

**Example Alert:**
```
🚨 CRITICAL: High Error Rate

Error Rate: 8.2%
Time Window: Last 5 minutes
Errors: 82 / 1,000 requests

Affected Endpoints:
- POST /api/trading/order (45 errors)
- GET /api/market/data (37 errors)

Action Required:
- Check broker connections
- Review recent deployments
- Check QuestDB connectivity

View Logs: http://seq:5341
View Metrics: http://api:5002/api/metrics/summary
```

### 2. Latency Alerts

Triggered when request latency is too high.

**Condition:** `P95Latency > 2000ms`

**Example Alert:**
```
⚠️ WARNING: High Latency Detected

P95 Latency: 3,245ms (threshold: 2,000ms)
P99 Latency: 5,120ms
Average: 1,850ms

Slowest Endpoints:
1. POST /api/backtest/run - 4,500ms avg
2. GET /api/portfolio/analysis - 3,200ms avg
3. POST /api/trading/order - 2,100ms avg

Possible Causes:
- Database query performance
- External API slowness
- High system load

View Metrics: http://api:5002/api/metrics
```

### 3. Order Failure Alerts

Triggered when order placement failures spike.

**Condition:** `OrderFailureRate > 10%`

**Example Alert:**
```
🔴 CRITICAL: Order Failure Spike

Failure Rate: 15.3% (threshold: 10%)
Failed Orders: 23 / 150

Failure Reasons:
- Insufficient balance: 12
- Invalid price: 6
- Connection timeout: 5

Affected Brokers:
- Bybit: 15 failures
- Binance: 8 failures

Action Required:
- Check broker API status
- Verify account balances
- Review order validation

Broker Status: http://api:5002/api/health/brokers
```

### 4. Service Health Alerts

Triggered when services fail health checks.

**Condition:** `HealthCheckFailed == true`

**Example Alert:**
```
🔴 SERVICE DOWN: QuestDB Unavailable

Service: QuestDB
Status: Unhealthy
Last Successful Check: 2 minutes ago
Failed Checks: 3 consecutive

Impact:
- Cannot store new market data
- Cannot create new orders
- Cannot retrieve historical data

Action Required:
- Check QuestDB container: docker ps
- Check QuestDB logs: docker logs algotrendy-questdb
- Restart if needed: docker-compose restart questdb

Health Status: http://api:5002/health
```

### 5. ML Model Drift Alerts

Triggered when ML model accuracy degrades.

**Condition:** `ModelAccuracy < 70%` or `DataDrift > 0.3`

**Example Alert:**
```
⚠️ ML MODEL DRIFT DETECTED

Model: TrendReversal
Current Accuracy: 65% (baseline: 78%)
Drift Score: 0.42 (threshold: 0.30)

Metrics:
- Precision: 0.58
- Recall: 0.72
- F1 Score: 0.64

Action Required:
- Auto-retraining initiated
- Review recent market conditions
- Check data quality

Model Status: http://api:5002/api/ml/status
```

## Alert Channels

### Discord Setup

1. **Create Webhook in Discord**
   - Open Server Settings
   - Integrations → Webhooks
   - Click "New Webhook"
   - Name: `AlgoTrendy Alerts`
   - Copy webhook URL

2. **Configure in AlgoTrendy**
   ```bash
   export DISCORD_WEBHOOK_URL="https://discord.com/api/webhooks/123456/abc..."
   ```

3. **Test Alert**
   ```bash
   curl -X POST "${DISCORD_WEBHOOK_URL}" \
     -H "Content-Type: application/json" \
     -d '{
       "content": "🟢 AlgoTrendy monitoring active"
     }'
   ```

### Slack Setup

1. **Create Slack App**
   - Go to https://api.slack.com/apps
   - Click "Create New App"
   - Choose "From scratch"
   - Name: `AlgoTrendy Alerts`

2. **Enable Incoming Webhooks**
   - Navigate to "Incoming Webhooks"
   - Toggle "Activate Incoming Webhooks"
   - Click "Add New Webhook to Workspace"
   - Select channel: `#algotrendy-alerts`
   - Copy webhook URL

3. **Configure in AlgoTrendy**
   ```bash
   export SLACK_WEBHOOK_URL="https://hooks.slack.com/services/T00/B00/xxx"
   ```

4. **Test Alert**
   ```bash
   curl -X POST "${SLACK_WEBHOOK_URL}" \
     -H "Content-Type: application/json" \
     -d '{
       "text": ":white_check_mark: AlgoTrendy monitoring active"
     }'
   ```

### Email Alerts

Configure SMTP settings:

```json
{
  "Email": {
    "Enabled": true,
    "SmtpHost": "smtp.gmail.com",
    "SmtpPort": 587,
    "UseSsl": true,
    "From": "alerts@algotrendy.com",
    "To": ["ops@algotrendy.com"],
    "Username": "${EMAIL_USERNAME}",
    "Password": "${EMAIL_PASSWORD}"
  }
}
```

## Monitoring Dashboards

### Metrics API Endpoints

```bash
# Overall summary
GET /api/metrics/summary

# All metrics
GET /api/metrics

# Prometheus format
GET /api/metrics/prometheus
```

### Example Dashboard Queries

**Error Rate:**
```
(request_error_total / request_total_total) * 100
```

**P95 Latency:**
```
histogram_quantile(0.95, request_duration_milliseconds)
```

**Active Positions:**
```
trading_positions_active
```

## Alert Rules Configuration

### Custom Alert Rules

Create custom alert rules in code:

```csharp
public class CustomAlertRule : IAlertRule
{
    public string Name => "HighVolumeSpike";
    public AlertSeverity Severity => AlertSeverity.Warning;

    public bool ShouldAlert(MetricsSnapshot metrics)
    {
        var currentVolume = metrics.GetMetric("trading_volume");
        var avgVolume = metrics.GetAverage("trading_volume", TimeSpan.FromHours(24));

        return currentVolume > avgVolume * 3; // 3x spike
    }

    public AlertMessage CreateAlert(MetricsSnapshot metrics)
    {
        return new AlertMessage
        {
            Title = "High Trading Volume Spike",
            Message = $"Volume is 3x higher than 24h average",
            Severity = Severity,
            Timestamp = DateTime.UtcNow
        };
    }
}
```

## Grafana Integration (Optional)

### Setup Prometheus Exporter

1. **Install Prometheus**
   ```bash
   docker run -d \
     --name prometheus \
     -p 9090:9090 \
     -v $(pwd)/prometheus.yml:/etc/prometheus/prometheus.yml \
     prom/prometheus
   ```

2. **Configure Scraping**
   ```yaml
   # prometheus.yml
   scrape_configs:
     - job_name: 'algotrendy'
       static_configs:
         - targets: ['api:5002']
       metrics_path: '/api/metrics/prometheus'
       scrape_interval: 15s
   ```

3. **Install Grafana**
   ```bash
   docker run -d \
     --name grafana \
     -p 3001:3000 \
     grafana/grafana
   ```

4. **Add Prometheus Data Source**
   - Open Grafana: http://localhost:3001
   - Add Data Source → Prometheus
   - URL: http://prometheus:9090

5. **Import Dashboard**
   - Download: `/docs/monitoring/grafana-dashboard.json`
   - Import in Grafana

## Best Practices

### Alert Fatigue Prevention

1. **Use Cooldown Periods**
   - Prevent repeated alerts
   - Default: 15 minutes between same alert

2. **Severity Levels**
   - Critical: Immediate action required
   - Warning: Monitor situation
   - Info: Informational only

3. **Smart Grouping**
   - Group related alerts
   - Aggregate similar errors

### On-Call Procedures

**Critical Alerts:**
- Page on-call engineer immediately
- Create incident ticket
- Begin remediation within 5 minutes

**Warning Alerts:**
- Notify team channel
- Investigate within 1 hour
- Update status

**Info Alerts:**
- Log only
- Review during business hours

## Troubleshooting

### Alerts Not Firing

```bash
# Check alert configuration
cat appsettings.json | grep -A 20 "Alerts"

# Verify webhook URLs
echo $DISCORD_WEBHOOK_URL
echo $SLACK_WEBHOOK_URL

# Test webhook manually
curl -X POST "$DISCORD_WEBHOOK_URL" -d '{"content":"Test"}'

# Check logs
docker logs algotrendy-api | grep "Alert"
```

### Too Many Alerts

```bash
# Increase thresholds in appsettings.json
# Increase cooldown periods
# Add alert grouping
```

### Missing Metrics

```bash
# Verify metrics middleware is enabled
# Check /api/metrics endpoint
curl http://localhost:5002/api/metrics

# Restart API if needed
docker-compose restart api
```

## Monitoring Checklist

- [ ] Seq log viewer accessible
- [ ] Metrics API endpoint working
- [ ] Health checks passing
- [ ] Discord/Slack webhooks configured
- [ ] Alert rules defined
- [ ] On-call rotation set
- [ ] Incident response plan documented
- [ ] Grafana dashboard created (optional)

## Support

For monitoring issues:
- **Logs:** http://seq:5341
- **Metrics:** http://api:5002/api/metrics
- **Health:** http://api:5002/health
- **Documentation:** `/docs/operations/`

---

**Last Updated:** October 21, 2025
**Version:** 2.6.0
