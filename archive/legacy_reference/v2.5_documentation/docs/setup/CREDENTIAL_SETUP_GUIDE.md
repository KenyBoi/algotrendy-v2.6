# 🔐 COMPOSER.TRADE CREDENTIAL SETUP GUIDE

## Quick Start (2 minutes)

```bash
# 1. Run the credential setup script
python3 setup_credentials.py

# 2. Answer the prompts:
#    - Paste your Composer API Key (hidden input)
#    - Enter your wallet address (0x...)
#    - Select your network (1-6)

# 3. Verify setup
python3 -c "from dotenv import load_dotenv; import os; load_dotenv(); print('✅ Credentials configured' if os.getenv('COMPOSER_API_KEY') else '❌ Not configured')"
```

---

## Where to Get Your Credentials

### 1. **Composer API Key**

```
From Composer Dashboard:
1. Log in to https://dashboard.composer.trade
2. Go to Settings → API Keys
3. Click "Create New API Key"
4. Copy the key (starts with "comp_sk_..." or similar)
5. Paste into setup script
```

**Important**: Store this securely. Never share it.

### 2. **Wallet Address**

```
Your wallet address:
- Starts with "0x"
- Is exactly 42 characters long
- Example: 0x1234567890123456789012345678901234567890

Use any of your Ethereum-compatible wallets:
- MetaMask
- Ledger
- Trezor
- Coinbase Wallet
- Any EVM wallet
```

### 3. **Network Selection**

```
Choose which network to use:
1. Ethereum (eth-mainnet) - Most liquid, highest fees
2. Polygon (polygon-mainnet) - Fast, low fees
3. Arbitrum (arb-mainnet) - Low fees, high speed
4. Optimism (opt-mainnet) - Low fees, good liquidity
5. Base (base-mainnet) - Newer, fast, low fees
6. Avalanche (avax-mainnet) - Fast, good ecosystem

Recommendation for testing: Start with Arbitrum (option 3)
```

---

## Running the Credential Setup

### Option 1: Interactive Setup (Recommended)

```bash
python3 setup_credentials.py
```

**What happens:**
- Prompts for API key (hidden for security)
- Prompts for wallet address
- Prompts for network selection
- Validates credentials format
- Tests API connection
- Saves to `.env` file securely
- Verifies all components

**Output:**
```
======================================================================
           🔐 COMPOSER.TRADE CREDENTIAL SETUP
======================================================================

ℹ️  Enter your Composer.Trade API credentials
ℹ️  (These will be securely stored in .env)

Enter Composer API Key (hidden): [hidden input]
Enter Wallet Address (0x...): 0x1234567890123456789012345678901234567890

Select network:
  1. ethereum
  2. polygon
  3. arbitrum
  4. optimism
  5. base
  6. avalanche

Enter network choice (1-6): 3

ℹ️  Testing API credentials...
✅ Credentials format validated
ℹ️  Attempting to connect to Composer API...
✅ API connection successful!

======================================================================
                    🔍 VERIFYING SETUP
======================================================================

✅ .env file found
✅ All required environment variables present
✅ All Composer integration files found
✅ Required Python packages installed

ℹ️  Setup verification: 4/4 checks passed
✅ Setup complete and verified!
```

### Option 2: Manual Setup

If you prefer to manually create the `.env` file:

```bash
# Create .env file
cat > .env << 'EOF'
# Composer.Trade API Configuration
COMPOSER_API_KEY=your_api_key_here
COMPOSER_WALLET_ADDRESS=0xYourWalletAddressHere
COMPOSER_NETWORK=arbitrum

# Optional settings
PAPER_TRADING=true
LOG_LEVEL=INFO
EOF

# Set secure permissions
chmod 600 .env
```

### Option 3: Environment Variables Only

If you don't want to use a `.env` file:

```bash
export COMPOSER_API_KEY="your_api_key"
export COMPOSER_WALLET_ADDRESS="0xYourWalletAddress"
export COMPOSER_NETWORK="arbitrum"

python3 -c "from composer_trade_integration import ComposerTradeHTTP; import asyncio; asyncio.run(ComposerTradeHTTP().connect())"
```

---

## Verifying Your Setup

### 1. **Check .env File**

```bash
# View variables (without showing secrets)
grep -E "^COMPOSER_" .env

# Output should show:
# COMPOSER_API_KEY=comp_sk_***
# COMPOSER_WALLET_ADDRESS=0x...
# COMPOSER_NETWORK=arbitrum
```

### 2. **Test API Connection**

```bash
# Quick Python test
python3 << 'EOF'
import os
from dotenv import load_dotenv
from composer_trade_integration import ComposerTradeHTTP
import asyncio

load_dotenv()

async def test():
    client = ComposerTradeHTTP()
    await client.connect()
    portfolio = await client.get_portfolio()
    print(f"✅ Connected! Portfolio: {portfolio}")
    await client.disconnect()

asyncio.run(test())
EOF
```

### 3. **Use the Launcher Script**

```bash
bash launch_composer_integration.sh

# Select option 1: Test API Connection
# This will verify credentials and show your portfolio
```

### 4. **Run Full Test Suite**

```bash
pytest test_composer_integration.py -v

# All 20+ tests should pass
```

---

## Troubleshooting

### Problem: "API key is invalid or expired"

**Solution:**
1. Verify API key in Composer dashboard
2. Regenerate key if needed
3. Update .env file
4. Re-run setup script

```bash
python3 setup_credentials.py
# Select "yes" to update .env
```

### Problem: "Invalid wallet address format"

**Solution:**
1. Check wallet address starts with `0x`
2. Verify it's exactly 42 characters
3. Ensure you didn't copy extra spaces

```bash
# Verify wallet format
echo "0x1234567890123456789012345678901234567890" | wc -c
# Should output: 43 (42 chars + newline)
```

### Problem: "Connection timeout"

**Solution:**
1. Check internet connection
2. Verify Composer API is online
3. Check firewall/proxy settings
4. Try different network

```bash
# Test connectivity
curl -s https://api.composer.trade/v1/health | head -20
```

### Problem: ".env file not found"

**Solution:**
1. Run setup script again
2. Or manually create with Option 2 above
3. Verify file permissions

```bash
# Check if file exists
ls -la .env

# If not, create it
python3 setup_credentials.py
```

### Problem: "Python packages not found"

**Solution:**
1. Install required packages
2. Use the launcher script

```bash
pip install aiohttp websockets python-dotenv pytest
```

---

## Security Best Practices

### 1. **Protect Your .env File**

```bash
# Ensure only you can read it
chmod 600 .env

# Verify permissions
ls -la .env
# Should show: -rw------- (600)

# Add to .gitignore if using git
echo ".env" >> .gitignore
```

### 2. **Never Commit Credentials**

```bash
# Check if .env is tracked
git status | grep .env

# If it is, remove it
git rm --cached .env

# Verify it's not in history
git log --all -- .env
```

### 3. **Rotate Keys Regularly**

```bash
# Best practice: rotate every 30-90 days
# 1. Generate new key in Composer dashboard
# 2. Update .env file
# 3. Run verification
# 4. Delete old key from dashboard
```

### 4. **Use Environment Variables**

For production, use environment variables instead of .env:

```bash
# In your deployment system (not in code):
export COMPOSER_API_KEY="your_key"
export COMPOSER_WALLET_ADDRESS="0x..."
export COMPOSER_NETWORK="arbitrum"

# Code automatically picks these up
python3 run_trader.py
```

### 5. **Backup Safely**

```bash
# Create encrypted backup (if using GPG)
gpg -c .env
# Creates .env.gpg

# Store in secure location (not cloud)
# Or use password manager:
# - 1Password
# - Bitwarden
# - LastPass
# - KeePass
```

---

## Integration with MEM

### After Setup, Integrate with MEM:

#### Option A: Automatic (Using Launcher)

```bash
bash launch_composer_integration.sh
# Select Option 4: "Launch MEM with Composer"
```

#### Option B: Manual Integration

```python
# In your MEM trader code
from dotenv import load_dotenv
import os
from composer_trade_integration import ComposerTradeHTTP

load_dotenv()  # Load .env automatically

async def initialize_trader():
    # Credentials loaded from environment
    client = ComposerTradeHTTP()
    await client.connect()
    
    # Use in trading logic
    portfolio = await client.get_portfolio()
    return client

# Run
import asyncio
trader = asyncio.run(initialize_trader())
```

#### Option C: Via Configuration

```json
{
  "composer": {
    "enabled": true,
    "type": "composer_trade",
    "api_key": "${COMPOSER_API_KEY}",
    "wallet_address": "${COMPOSER_WALLET_ADDRESS}",
    "network": "${COMPOSER_NETWORK}"
  }
}
```

---

## Testing Credentials

### 1. **Syntax Validation Only**

```python
# No API call, just format check
from setup_credentials import test_credentials
result = test_credentials("your_key", "0xYourAddress")
print("✅ Format valid" if result else "❌ Format invalid")
```

### 2. **Full Connection Test**

```python
# Makes actual API call
from setup_credentials import test_api_connection
import asyncio
result = asyncio.run(test_api_connection("your_key", "0xYourAddress"))
print("✅ Connected" if result else "❌ Failed")
```

### 3. **Portfolio Query Test**

```bash
# Full end-to-end test
python3 << 'EOF'
import asyncio
import os
from dotenv import load_dotenv
from composer_trade_integration import ComposerTradeHTTP

load_dotenv()

async def test():
    client = ComposerTradeHTTP()
    await client.connect()
    
    # Get portfolio
    portfolio = await client.get_portfolio()
    print(f"Portfolio positions: {len(portfolio)}")
    
    # Get token price
    eth_price = await client.get_token_price("ETH")
    print(f"ETH price: ${eth_price}")
    
    await client.disconnect()
    print("✅ All tests passed!")

asyncio.run(test())
EOF
```

---

## Next Steps

After credentials are configured:

1. **Run launcher**: `bash launch_composer_integration.sh`
2. **Test connection**: Option 1
3. **Query portfolio**: Option 2
4. **Run tests**: Option 3
5. **Integrate with MEM**: Follow Option A, B, or C above
6. **Start trading**: Begin with paper trading

---

## Documentation Reference

| Document | Purpose | Time |
|----------|---------|------|
| `COMPOSER_QUICK_REFERENCE.md` | One-page cheat sheet | 2 min |
| `COMPOSER_INTEGRATION_GUIDE.md` | Detailed setup & API | 20 min |
| `composer_config.json` | Configuration reference | 5 min |
| `setup_credentials.py` | This credential setup | 5 min |

---

## Support

If you encounter issues:

1. **Check logs**: Look in `composer_*.log` files
2. **Review errors**: Run `pytest test_composer_integration.py -v`
3. **Test connectivity**: `curl https://api.composer.trade/v1/health`
4. **Verify .env**: `grep COMPOSER_ .env` (without showing secrets)
5. **Reinstall deps**: `pip install -r requirements.txt --upgrade`

---

**Status**: ✅ Production Ready | **Version**: 1.0 | **Last Updated**: 2025-10-16
