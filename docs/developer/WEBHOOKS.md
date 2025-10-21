# Webhooks Documentation

## Overview

AlgoTrendy supports webhooks for real-time notifications and integrations with external systems.

## Webhook Types

### 1. Outgoing Webhooks (Notifications)

Send notifications when events occur in AlgoTrendy:

- Order status changes
- Price alerts triggered
- System events
- Custom events

### 2. Incoming Webhooks (Integrations)

Receive signals from external systems:

- TradingView alerts
- Custom trading signals
- Third-party integrations

## Configuration

### appsettings.json

```json
{
  "Webhooks": {
    "OrderNotifications": "https://your-webhook-url.com/orders",
    "PriceAlerts": "https://your-webhook-url.com/alerts",
    "SystemEvents": "https://your-webhook-url.com/system",
    "Enabled": true
  }
}
```

### Environment Variables

```bash
export WEBHOOK_ORDER_NOTIFICATIONS="https://your-webhook-url.com/orders"
export WEBHOOK_PRICE_ALERTS="https://your-webhook-url.com/alerts"
export WEBHOOK_SYSTEM_EVENTS="https://your-webhook-url.com/system"
```

## Outgoing Webhooks

### Order Notifications

Sent when order status changes:

```json
{
  "eventType": "order_status_changed",
  "timestamp": "2025-10-21T12:00:00Z",
  "data": {
    "orderId": "550e8400-e29b-41d4-a716-446655440000",
    "status": "Filled",
    "source": "AlgoTrendy.API"
  }
}
```

### Price Alerts

Sent when price conditions are met:

```json
{
  "eventType": "price_alert",
  "timestamp": "2025-10-21T12:00:00Z",
  "data": {
    "symbol": "BTCUSDT",
    "price": 43500.00,
    "condition": "above_target",
    "source": "AlgoTrendy.API"
  }
}
```

### System Events

Sent for system-level events:

```json
{
  "eventType": "system_event",
  "timestamp": "2025-10-21T12:00:00Z",
  "data": {
    "type": "high_error_rate",
    "message": "Error rate exceeded threshold",
    "source": "AlgoTrendy.API"
  }
}
```

## Incoming Webhooks

### TradingView Integration

#### 1. Create Alert in TradingView

1. Open a chart on TradingView
2. Right-click and select "Add Alert"
3. Configure alert conditions
4. Set webhook URL: `https://api.algotrendy.com/api/webhook/tradingview`

#### 2. Alert Message Template

```json
{
  "symbol": "{{ticker}}",
  "action": "{{strategy.order.action}}",
  "price": {{close}},
  "strategy": "My Strategy",
  "interval": "{{interval}}",
  "quantity": 0.001,
  "message": "{{strategy.order.comment}}"
}
```

#### 3. Example Alert Payload

```json
{
  "symbol": "BTCUSDT",
  "action": "buy",
  "price": 43250.50,
  "strategy": "EMA Crossover",
  "interval": "1h",
  "quantity": 0.001,
  "message": "EMA cross detected"
}
```

### Custom Webhooks

Send custom signals to AlgoTrendy:

```bash
curl -X POST "https://api.algotrendy.com/api/webhook/receive" \
  -H "Content-Type: application/json" \
  -H "X-API-Key: YOUR_API_KEY" \
  -d '{
    "eventType": "custom_signal",
    "symbol": "ETHUSDT",
    "action": "buy",
    "data": {
      "indicator": "RSI",
      "value": 28.5,
      "threshold": 30
    }
  }'
```

## Testing Webhooks

### Test Outgoing Webhook

```bash
curl -X POST "https://api.algotrendy.com/api/webhook/test" \
  -H "Content-Type: application/json" \
  -H "X-API-Key: YOUR_API_KEY" \
  -d '{
    "url": "https://webhook.site/your-unique-id",
    "message": "Test webhook from AlgoTrendy",
    "data": {
      "test": true,
      "timestamp": "2025-10-21T12:00:00Z"
    }
  }'
```

### Test with webhook.site

1. Go to https://webhook.site
2. Copy your unique URL
3. Use the test endpoint above with your URL
4. View received webhook at webhook.site

## Security

### Request Signing

Outgoing webhooks include a signature for verification:

```http
POST /your-webhook-endpoint
Content-Type: application/json
X-Webhook-Signature: sha256=abc123def456...
X-Webhook-Timestamp: 1634567890

{
  "eventType": "order_status_changed",
  ...
}
```

### Verify Signature (Node.js Example)

```javascript
const crypto = require('crypto');

function verifyWebhook(payload, signature, secret) {
  const hmac = crypto.createHmac('sha256', secret);
  hmac.update(JSON.stringify(payload));
  const computedSignature = 'sha256=' + hmac.digest('hex');

  return crypto.timingSafeEqual(
    Buffer.from(signature),
    Buffer.from(computedSignature)
  );
}
```

### IP Whitelist

For incoming webhooks, whitelist AlgoTrendy IPs:

```
203.0.113.0/24
198.51.100.0/24
```

## Retry Logic

Outgoing webhooks implement automatic retry:

- **Retry Attempts:** 3
- **Backoff:** Exponential (1s, 2s, 4s)
- **Timeout:** 10 seconds per attempt

## Rate Limits

- **Outgoing:** Unlimited (best effort)
- **Incoming:** 100 requests/minute per API key

## Monitoring

### Webhook Logs

All webhook activity is logged:

```bash
# View webhook logs
tail -f logs/algotrendy-*.log | grep "webhook"
```

### Metrics

Track webhook performance via metrics endpoint:

```bash
curl -X GET "https://api.algotrendy.com/api/metrics" \
  -H "X-API-Key: YOUR_API_KEY"
```

## Integration Examples

### Discord

Send notifications to Discord:

```json
{
  "Webhooks": {
    "OrderNotifications": "https://discord.com/api/webhooks/YOUR_WEBHOOK_ID/YOUR_WEBHOOK_TOKEN"
  }
}
```

Transform payload for Discord:

```javascript
function transformToDiscord(payload) {
  return {
    content: `Order ${payload.data.orderId} is now ${payload.data.status}`,
    embeds: [{
      title: 'Order Update',
      description: JSON.stringify(payload.data, null, 2),
      color: 0x00ff00,
      timestamp: payload.timestamp
    }]
  };
}
```

### Slack

Send notifications to Slack:

```json
{
  "Webhooks": {
    "OrderNotifications": "https://hooks.slack.com/services/YOUR/WEBHOOK/URL"
  }
}
```

Transform payload for Slack:

```javascript
function transformToSlack(payload) {
  return {
    text: `Order Update: ${payload.data.status}`,
    blocks: [{
      type: 'section',
      text: {
        type: 'mrkdwn',
        text: `*Order ID:* ${payload.data.orderId}\n*Status:* ${payload.data.status}`
      }
    }]
  };
}
```

### Telegram

Send notifications to Telegram:

```javascript
const telegramBot = require('node-telegram-bot-api');
const bot = new telegramBot(YOUR_BOT_TOKEN);

function sendToTelegram(payload) {
  const chatId = YOUR_CHAT_ID;
  const message = `Order ${payload.data.orderId} status: ${payload.data.status}`;

  bot.sendMessage(chatId, message);
}
```

## Troubleshooting

### Webhook Not Received

1. Check webhook URL is correct
2. Verify endpoint is publicly accessible
3. Check firewall/security group rules
4. Review webhook logs for errors

### TradingView Webhooks Not Working

1. Verify alert message format is valid JSON
2. Check TradingView webhook URL
3. Ensure API key is valid
4. Review rate limiting

### Signature Verification Fails

1. Ensure using correct secret
2. Verify payload hasn't been modified
3. Check timestamp isn't too old (> 5 minutes)

## Best Practices

1. **Use HTTPS** - Always use secure endpoints
2. **Verify Signatures** - Validate incoming webhooks
3. **Handle Retries** - Implement idempotency
4. **Log Everything** - Track all webhook activity
5. **Monitor Failures** - Alert on webhook failures
6. **Rate Limit** - Protect your webhook receivers
7. **Timeout** - Set reasonable timeouts (< 30s)

## API Reference

### POST /api/webhook/test

Test webhook connectivity

**Request:**
```json
{
  "url": "https://your-endpoint.com/webhook",
  "message": "Test message",
  "data": {}
}
```

**Response:**
```json
{
  "message": "Webhook test sent successfully",
  "url": "https://your-endpoint.com/webhook",
  "timestamp": "2025-10-21T12:00:00Z"
}
```

### POST /api/webhook/tradingview

Receive TradingView alerts

**Request:**
```json
{
  "symbol": "BTCUSDT",
  "action": "buy",
  "price": 43250.50,
  "strategy": "EMA Crossover",
  "interval": "1h",
  "quantity": 0.001
}
```

**Response:**
```json
{
  "message": "Alert received",
  "symbol": "BTCUSDT",
  "action": "buy",
  "timestamp": "2025-10-21T12:00:00Z"
}
```

### POST /api/webhook/receive

Generic webhook receiver

**Request:**
```json
{
  "eventType": "custom_event",
  "data": {}
}
```

**Response:**
```json
{
  "message": "Webhook received",
  "timestamp": "2025-10-21T12:00:00Z"
}
```

## Support

For webhook issues:
- GitHub Issues: https://github.com/KenyBoi/algotrendy-v2.6/issues
- Documentation: `/docs/developer/WEBHOOKS.md`
