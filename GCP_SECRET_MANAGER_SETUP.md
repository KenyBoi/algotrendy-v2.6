# Google Secret Manager Setup - API Credentials Retrieval

**Date:** October 19, 2025
**Purpose:** Retrieve TradeStation, NinjaTrader, and Interactive Brokers API credentials from GCP

---

## Prerequisites

You need:
1. **GCP Service Account JSON Key** file
2. **GCP Project ID** where secrets are stored
3. **Service Account Permissions**: Secret Manager Secret Accessor role

---

## Quick Setup (5 minutes)

### Step 1: Get Your GCP Service Account Key

1. Go to [GCP Console](https://console.cloud.google.com)
2. Navigate to: **IAM & Admin** → **Service Accounts**
3. Find or create a service account with "Secret Manager Secret Accessor" role
4. Click **Actions** → **Manage Keys**
5. Click **Add Key** → **Create New Key** → Select **JSON**
6. Download the JSON file (save it securely!)

### Step 2: Upload Service Account Key to Server

```bash
# Option A: Use scp from your local machine
scp /path/to/service-account.json root@your-server:/root/gcp-credentials.json

# Option B: Create file directly (paste JSON content)
nano /root/gcp-credentials.json
# Paste the JSON content, save and exit (Ctrl+X, Y, Enter)
```

### Step 3: Set Environment Variables

```bash
# Set the credentials path
export GOOGLE_APPLICATION_CREDENTIALS=/root/gcp-credentials.json

# Set your GCP project ID
export GCP_PROJECT_ID=your-project-id-here

# Make them permanent (optional)
echo 'export GOOGLE_APPLICATION_CREDENTIALS=/root/gcp-credentials.json' >> ~/.bashrc
echo 'export GCP_PROJECT_ID=your-project-id-here' >> ~/.bashrc
source ~/.bashrc
```

### Step 4: Run the Retrieval Script

```bash
cd /root/AlgoTrendy_v2.6
python3 scripts/retrieve_gcp_secrets.py
```

The script will:
- ✅ Install google-cloud-secret-manager library
- ✅ Connect to GCP Secret Manager
- ✅ Retrieve all API credentials
- ✅ Offer to save them to `.env` file automatically

---

## Secret Names in GCP

The script expects secrets with these names:

### TradeStation
- `tradestation-api-key`
- `tradestation-api-secret`
- `tradestation-account-id`

### NinjaTrader
- `ninjatrader-username`
- `ninjatrader-password`
- `ninjatrader-account-id`
- `ninjatrader-connection-type`

### Interactive Brokers
- `ibkr-username`
- `ibkr-password`
- `ibkr-account-id`
- `ibkr-gateway-port`

**If your secret names are different**, you can either:
1. Rename them in GCP to match these names, OR
2. Edit the script at `scripts/retrieve_gcp_secrets.py` (line 54-70) to match your names

---

## Manual Retrieval (Alternative)

If you prefer to manually retrieve secrets:

### From GCP Console

1. Go to [Secret Manager](https://console.cloud.google.com/security/secret-manager)
2. Click on each secret
3. Click **View Secret Value**
4. Copy the value
5. Add to `/root/AlgoTrendy_v2.6/.env` file

### .env File Format

```bash
# TradeStation
TRADESTATION_API_KEY=your_api_key_here
TRADESTATION_API_SECRET=your_secret_here
TRADESTATION_ACCOUNT_ID=your_account_id
TRADESTATION_USE_PAPER=true

# NinjaTrader
NINJATRADER_USERNAME=your_username
NINJATRADER_PASSWORD=your_password
NINJATRADER_ACCOUNT_ID=your_account_id
NINJATRADER_CONNECTION_TYPE=REST  # or "NinjaScript"

# Interactive Brokers
IBKR_USERNAME=your_username
IBKR_PASSWORD=your_password
IBKR_ACCOUNT_ID=your_account_id
IBKR_GATEWAY_PORT=4001  # Default: 4001 for live, 4002 for paper
```

---

## Troubleshooting

### Problem: "google-cloud-secret-manager not installed"

**Solution:**
```bash
pip install google-cloud-secret-manager
```

### Problem: "Could not retrieve secret: 404 Not Found"

**Causes:**
- Secret name doesn't exist in GCP
- Wrong project ID
- Service account doesn't have permission

**Solution:**
```bash
# List all secrets in your project
gcloud secrets list --project=your-project-id

# Check secret exists
gcloud secrets versions access latest --secret="tradestation-api-key" --project=your-project-id
```

### Problem: "Permission denied"

**Cause:** Service account doesn't have Secret Manager Secret Accessor role

**Solution:**
1. Go to GCP Console → IAM & Admin → IAM
2. Find your service account
3. Add role: "Secret Manager Secret Accessor"

### Problem: "GOOGLE_APPLICATION_CREDENTIALS not set"

**Solution:**
```bash
export GOOGLE_APPLICATION_CREDENTIALS=/root/gcp-credentials.json
```

### Problem: "Project ID not found"

**Solution:**
```bash
# Find your project ID
gcloud projects list

# Set it
export GCP_PROJECT_ID=your-project-id
```

---

## Security Best Practices

### ✅ DO

1. **Restrict Service Account Permissions**
   - Only grant "Secret Manager Secret Accessor" role
   - Don't grant owner/editor roles

2. **Secure the JSON Key File**
   ```bash
   chmod 600 /root/gcp-credentials.json
   ```

3. **Use Environment Variables**
   - Don't hardcode credentials in scripts

4. **Rotate Secrets Regularly**
   - Update secrets in GCP every 90 days
   - Re-run retrieval script after rotation

5. **Enable Audit Logging**
   - Monitor who accesses secrets in GCP

### ❌ DON'T

1. **Never Commit Service Account Key to Git**
   - Already in .gitignore

2. **Don't Share Service Account Key**
   - Each team member should have their own

3. **Don't Store Key in Public Locations**
   - Keep it in /root/ with restricted permissions

---

## Verification

After running the script, verify credentials were added:

```bash
# Check .env file (shows first 30 chars only)
grep "TRADESTATION_API_KEY" /root/AlgoTrendy_v2.6/.env | cut -c1-40

# Should show: TRADESTATION_API_KEY=<your_key>...
```

---

## Next Steps

After credentials are retrieved:

1. **Verify .env file** has all credentials
2. **Test broker connections** (we'll create test scripts)
3. **Implement broker integrations** in v2.6
4. **Deploy to production**

---

## Additional Resources

- **GCP Secret Manager Docs:** https://cloud.google.com/secret-manager/docs
- **Service Account Setup:** https://cloud.google.com/iam/docs/creating-managing-service-accounts
- **Python Client Library:** https://googleapis.dev/python/secretmanager/latest/

---

**Status:** Ready to retrieve credentials
**Script Location:** `/root/AlgoTrendy_v2.6/scripts/retrieve_gcp_secrets.py`
**Next Step:** Run the retrieval script after setting up GCP authentication
