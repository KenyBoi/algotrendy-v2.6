# AlgoTrendy v2.6 - Secrets Configuration Guide

## Overview
AlgoTrendy v2.6 uses **User Secrets** for development and **Environment Variables** for production to keep API credentials secure.

## Development Setup (User Secrets)

### Step 1: Initialize User Secrets

From the AlgoTrendy.API directory:

```bash
cd /root/AlgoTrendy_v2.6/backend/AlgoTrendy.API
dotnet user-secrets init
```

This creates a secrets.json file stored outside your project directory.

### Step 2: Add Binance Credentials

**For Testnet (Development):**

```bash
dotnet user-secrets set "Binance:ApiKey" "YOUR_BINANCE_TESTNET_API_KEY"
dotnet user-secrets set "Binance:ApiSecret" "YOUR_BINANCE_TESTNET_API_SECRET"
```

**For Production:**

```bash
dotnet user-secrets set "Binance:UseTestnet" "false"
dotnet user-secrets set "Binance:ApiKey" "YOUR_BINANCE_PRODUCTION_API_KEY"
dotnet user-secrets set "Binance:ApiSecret" "YOUR_BINANCE_PRODUCTION_API_SECRET"
```

### Step 3: Verify Secrets

```bash
dotnet user-secrets list
```

Expected output:
```
Binance:ApiKey = YOUR_API_KEY
Binance:ApiSecret = YOUR_API_SECRET
Binance:UseTestnet = true
```

## Production Setup (Environment Variables)

### Option 1: systemd service file

```ini
[Service]
Environment="Binance__UseTestnet=false"
Environment="Binance__ApiKey=YOUR_API_KEY"
Environment="Binance__ApiSecret=YOUR_API_SECRET"
```

### Option 2: Docker Compose

```yaml
environment:
  - Binance__UseTestnet=false
  - Binance__ApiKey=${BINANCE_API_KEY}
  - Binance__ApiSecret=${BINANCE_API_SECRET}
```

### Option 3: Export in shell

```bash
export Binance__UseTestnet=false
export Binance__ApiKey="YOUR_API_KEY"
export Binance__ApiSecret="YOUR_API_SECRET"
```

## Getting Binance Testnet API Keys

1. Go to [Binance Testnet](https://testnet.binance.vision/)
2. Click "Generate HMAC_SHA256 Key"
3. Save your API Key and Secret Key
4. Add them to User Secrets as shown above

## Getting Binance Production API Keys

1. Log into [Binance.com](https://www.binance.com/)
2. Go to API Management
3. Create a new API key
4. **Important Security Settings:**
   - Enable "Enable Spot & Margin Trading"
   - DO NOT enable "Enable Withdrawals"
   - Set IP whitelist if possible
   - Use read-only permissions for testing

## Configuration Priority

The configuration system loads settings in this order (later overrides earlier):

1. appsettings.json
2. appsettings.Development.json (in Development environment)
3. User Secrets (in Development environment)
4. Environment Variables
5. Command-line arguments

## Security Best Practices

1. **Never commit secrets to git**
   - User secrets are stored outside your project
   - appsettings.json only contains placeholder values

2. **Use testnet for development**
   - Set `UseTestnet: true` in development
   - Use production keys only when ready

3. **Restrict API permissions**
   - Only enable trading permissions
   - Never enable withdrawal permissions
   - Use IP whitelisting when possible

4. **Rotate keys regularly**
   - Change API keys periodically
   - Immediately revoke compromised keys

## Troubleshooting

### "ApiKey not set" error
- Verify user secrets are set: `dotnet user-secrets list`
- Check you're in the correct project directory
- Ensure UserSecretsId is in your .csproj file

### "Unauthorized" error from Binance
- Verify API key and secret are correct
- Check if testnet/production setting matches your keys
- Ensure API key has trading permissions enabled

### User secrets not loading
- Check environment is set to "Development"
- Verify the UserSecretsId in .csproj matches secrets location
- Try clearing and re-adding secrets

## Example: Complete Setup

```bash
# Navigate to API project
cd /root/AlgoTrendy_v2.6/backend/AlgoTrendy.API

# Initialize user secrets
dotnet user-secrets init

# Add testnet credentials (for development)
dotnet user-secrets set "Binance:UseTestnet" "true"
dotnet user-secrets set "Binance:ApiKey" "YOUR_TESTNET_KEY"
dotnet user-secrets set "Binance:ApiSecret" "YOUR_TESTNET_SECRET"

# Verify
dotnet user-secrets list

# Run the application
dotnet run
```

## Testing Configuration

You can test if your configuration is loading correctly:

```bash
cd /root/AlgoTrendy_v2.6/backend/AlgoTrendy.API
dotnet run

# The logs should show:
# [INFO] Binance broker configured for TESTNET
# [INFO] Connected to Binance successfully
```

## Additional Brokers

To add other brokers (OKX, Kraken, Coinbase), follow the same pattern:

```bash
# OKX
dotnet user-secrets set "OKX:ApiKey" "YOUR_KEY"
dotnet user-secrets set "OKX:ApiSecret" "YOUR_SECRET"
dotnet user-secrets set "OKX:Passphrase" "YOUR_PASSPHRASE"

# Kraken
dotnet user-secrets set "Kraken:ApiKey" "YOUR_KEY"
dotnet user-secrets set "Kraken:ApiSecret" "YOUR_SECRET"
```

## Support

For issues with configuration, check:
1. Project logs in `logs/algotrendy-*.log`
2. User secrets location: `~/.microsoft/usersecrets/<UserSecretsId>/secrets.json`
3. Configuration binding in `Program.cs`
