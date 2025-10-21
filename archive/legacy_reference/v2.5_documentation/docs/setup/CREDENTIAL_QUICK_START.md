# 🚀 COMPOSER.TRADE - CREDENTIAL QUICK START

## You Have Your API Key Ready? Perfect! ⚡

Here's the fastest path to get Composer.Trade integrated with MEM:

---

## 3-STEP SETUP (5 minutes)

### Step 1: Run Credential Setup (2 min)

```bash
cd /root/algotrendy_v2.5
python3 setup_credentials.py
```

**What to provide:**
1. **Composer API Key** - Paste from Composer dashboard (hidden input)
2. **Wallet Address** - Your 0x... address (42 chars)
3. **Network** - Choose 1-6 (Arbitrum is recommended for testing)

✅ Script will auto-save to `.env` file

### Step 2: Verify Setup (1 min)

```bash
python3 verify_credentials.py
```

**You'll see:**
- ✅ Environment variables loaded
- ✅ All files present
- ✅ Dependencies installed
- ✅ API connection test
- ✅ Modules importable

### Step 3: Test Connection (2 min)

```bash
bash launch_composer_integration.sh
```

**Select Option 1**: Test API Connection
- Shows your portfolio
- Verifies credentials work
- Tests all blockchains

---

## What Happens Behind the Scenes

```
Your Input
    ↓
[setup_credentials.py]  ← Interactive setup with validation
    ↓
.env file created (secure, permissions: 0600)
    ↓
[verify_credentials.py]  ← Automatic verification
    ↓
All tests pass → Ready to trade! ✅
```

---

## Files Created During Setup

| File | Purpose | Size |
|------|---------|------|
| `.env` | Your credentials (secure) | ~200 bytes |
| `.env.backup` | Auto-backup (optional) | ~200 bytes |

**Security**: `.env` is automatically set to `0600` (read/write owner only)

---

## Next: Integrate with MEM

After credentials are verified, you have 3 options:

### Option A: Automatic (Easiest)
```bash
bash launch_composer_integration.sh
# Select "Option 4: Launch MEM with Composer"
```

### Option B: Manual Integration
```python
from composer_trade_integration import ComposerTradeHTTP
from dotenv import load_dotenv

load_dotenv()  # Loads .env automatically
client = ComposerTradeHTTP()
await client.connect()
```

### Option C: Via Configuration
```bash
# Edit composer_config.json
# Set "enabled": true
# API key will be read from environment
```

---

## Testing Your Credentials

All three verification levels:

```bash
# 1. Quick format check
python3 << 'EOF'
import os
from dotenv import load_dotenv
load_dotenv()
key = os.getenv('COMPOSER_API_KEY')
print(f"✅ Key configured" if key else "❌ Key missing")
EOF

# 2. Full verification
python3 verify_credentials.py

# 3. Connection test via launcher
bash launch_composer_integration.sh  # Option 1
```

---

## Common Issues & Fixes

### ❌ "API key is invalid"
- **Check**: Composer dashboard API Keys section
- **Fix**: Generate new key, run `setup_credentials.py` again

### ❌ "Invalid wallet address"
- **Check**: Address starts with `0x` and is 42 chars
- **Fix**: Verify in MetaMask/wallet, update via setup script

### ❌ ".env file not found"
- **Check**: Are you in `/root/algotrendy_v2.5`?
- **Fix**: Run `python3 setup_credentials.py` again

### ❌ "Python packages not found"
- **Fix**: `pip install aiohttp websockets python-dotenv`

### ❌ "Connection timeout"
- **Check**: Internet connection, firewall
- **Fix**: Try different network (Option 3 in setup: base or optimism)

---

## Security Reminders ⚠️

✅ **DO:**
- Keep `.env` file safe (permissions are 0600)
- Rotate API keys every 30-90 days
- Use environment variables for secrets
- Store backups in password manager

❌ **DON'T:**
- Commit `.env` to git (use `.gitignore`)
- Share API keys with anyone
- Store credentials in code
- Change `.env` file permissions

---

## Quick Commands Reference

```bash
# Setup
python3 setup_credentials.py

# Verify
python3 verify_credentials.py

# Test via launcher
bash launch_composer_integration.sh

# Check credentials are loaded
grep COMPOSER_ .env

# See what's configured (without secrets)
grep -E "^(COMPOSER|PAPER)" .env

# View logs
tail -f composer_*.log

# Run full test suite
pytest test_composer_integration.py -v
```

---

## Documentation Map

| Document | For | Time |
|----------|-----|------|
| **CREDENTIAL_SETUP_GUIDE.md** | Detailed credential setup | 15 min |
| **COMPOSER_QUICK_REFERENCE.md** | One-page cheat sheet | 2 min |
| **COMPOSER_INTEGRATION_GUIDE.md** | Full API reference | 30 min |
| **This file** | Quick start | 3 min |

---

## Status Check

```bash
# Everything good?
python3 << 'EOF'
import os
from dotenv import load_dotenv

load_dotenv()

checks = {
    'API Key': os.getenv('COMPOSER_API_KEY') is not None,
    'Wallet': os.getenv('COMPOSER_WALLET_ADDRESS') is not None,
    'Network': os.getenv('COMPOSER_NETWORK') is not None,
    '.env file': os.path.exists('.env'),
}

for check, status in checks.items():
    symbol = '✅' if status else '❌'
    print(f"{symbol} {check}")

all_pass = all(checks.values())
print(f"\n{'✅ Ready!' if all_pass else '⚠️ Please run setup'}")
EOF
```

---

## You're Ready! 🎉

Your credentials are now:
- ✅ Configured securely
- ✅ Validated and tested
- ✅ Ready for MEM integration
- ✅ Backed up safely

### Next Steps:
1. **Review**: `COMPOSER_QUICK_REFERENCE.md` (2 min)
2. **Integrate**: Choose Option A, B, or C above
3. **Test**: Run test suite with `pytest`
4. **Trade**: Start with paper trading first!

---

## Need Help?

1. **Error in setup**: Check error message, see "Common Issues" above
2. **Can't connect**: `python3 verify_credentials.py` for diagnostics
3. **API issues**: Check Composer dashboard status
4. **Code questions**: See `COMPOSER_INTEGRATION_GUIDE.md`

**Everything should just work! 🚀**

---

**Status**: ✅ Production Ready | **Version**: 1.0 | **Setup Time**: ~5 minutes
