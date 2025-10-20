# Bybit Testnet Setup Guide 🚀

## Step 1: Create Testnet Account (2 minutes)

1. Go to: **https://testnet.bybit.com**
2. Click "Sign Up" (top right)
3. Enter email and create password
4. Verify email (check spam folder)
5. Login to testnet

✅ **Done!** You now have a Bybit testnet account

## Step 2: Get Free Testnet USDT (30 seconds)

1. Login to https://testnet.bybit.com
2. Click on your profile (top right)
3. Click "Assets" → "Spot Account"
4. You should see **10,000 USDT** already credited! 💰

If not, look for a "Get Testnet Funds" button and click it.

## Step 3: Create API Key (2 minutes)

1. Click on your profile icon (top right)
2. Select "API Management" or go to: https://testnet.bybit.com/app/user/api-management
3. Click "Create New Key"
4. Configure API Key:
   - **API Key Name**: AlgoTrendy
   - **API Key Type**: System-generated API Keys
   - **Permissions**:
     - ✅ Read-Write (for trading)
     - ✅ Contract Trading
     - ✅ Spot Trading
   - **IP Restriction**: No IP restriction (for testing)
   - **2FA**: Enter your 2FA code if enabled
5. Click "Confirm"
6. **SAVE THESE IMMEDIATELY** (you can't see the secret again):
   - API Key: `XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX`
   - Secret Key: `YYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYY`

⚠️ **CRITICAL**: Copy both keys NOW and save them somewhere safe!

## Step 4: Configure AlgoTrendy (1 minute)

### Option A: Using .env file (Recommended)

Edit `/root/AlgoTrendy_v2.6/.env` and add:

```bash
# Bybit Testnet Configuration
BYBIT_API_KEY=your_api_key_here
BYBIT_API_SECRET=your_secret_key_here
BYBIT_TESTNET=true
DEFAULT_BROKER=bybit
```

### Option B: Using environment variables

```bash
export BYBIT_API_KEY="your_api_key_here"
export BYBIT_API_SECRET="your_secret_key_here"
export BYBIT_TESTNET=true
export DEFAULT_BROKER=bybit
```

## Step 5: Test Connection (30 seconds)

Run the test script:

```bash
cd /root/AlgoTrendy_v2.6
dotnet run --project backend/AlgoTrendy.API/AlgoTrendy.API.csproj
```

Or run the integration tests:

```bash
cd /root/AlgoTrendy_v2.6/backend
dotnet test --filter "Broker=Bybit"
```

## Step 6: Verify It's Working

You should see:
- ✅ Connection successful
- ✅ Balance: 10000 USDT (or similar)
- ✅ API calls working

## Quick Test Script

I've created a quick test script at `/root/AlgoTrendy_v2.6/test_bybit.sh`

Run it:
```bash
chmod +x /root/AlgoTrendy_v2.6/test_bybit.sh
./test_bybit.sh
```

## Troubleshooting

### Error: "API key is invalid"
- Double-check you copied the API key and secret correctly
- Make sure there are no extra spaces
- Verify the API key has trading permissions enabled

### Error: "IP restriction"
- Go back to API Management
- Edit your API key
- Set "IP Restriction" to "No restriction"

### Error: "Insufficient permissions"
- Edit your API key in Bybit
- Enable both "Spot Trading" and "Contract Trading" permissions

### Error: "Connection refused"
- Make sure you're using **testnet.bybit.com** URLs
- Set `BYBIT_TESTNET=true`

## What You Can Do Now

With testnet set up, you can:

1. **Test Trading Strategies** - Risk-free with fake money
2. **Place Orders** - Market, limit, stop-loss orders
3. **Test Leverage** - Up to 100x leverage (be careful, even in testnet!)
4. **Monitor Positions** - Real-time position tracking
5. **Test API Integration** - All AlgoTrendy features

## Moving to Production

⚠️ **NEVER use production keys in testnet, or vice versa!**

When ready for production:
1. Create account at https://www.bybit.com
2. Complete KYC verification
3. Deposit real funds
4. Create production API keys
5. Set `BYBIT_TESTNET=false`
6. Test with SMALL amounts first!

## Useful Links

- **Testnet**: https://testnet.bybit.com
- **API Docs**: https://bybit-exchange.github.io/docs/v5/intro
- **API Management**: https://testnet.bybit.com/app/user/api-management
- **Bybit.Net GitHub**: https://github.com/JKorf/Bybit.Net

## Security Best Practices

1. ✅ Never commit API keys to git
2. ✅ Use `.env` file (already in `.gitignore`)
3. ✅ Use IP restrictions in production
4. ✅ Use read-only keys when possible
5. ✅ Rotate keys regularly
6. ✅ Use different keys for testnet vs production

---

**Total Setup Time**: ~5 minutes
**Cost**: FREE
**Risk**: ZERO (testnet money)
**Fun**: HIGH 🚀

Ready to trade! 📈
