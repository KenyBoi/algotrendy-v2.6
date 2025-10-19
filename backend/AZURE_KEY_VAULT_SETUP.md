# Azure Key Vault Setup Guide

This guide explains how to set up and configure Azure Key Vault for secure credential management in AlgoTrendy v2.6.

---

## Why Azure Key Vault?

**Problem:** Hardcoded credentials in configuration files pose serious security risks:
- Accidental commits to version control expose secrets
- Difficult to rotate credentials
- No audit trail of secret access
- Violates security best practices

**Solution:** Azure Key Vault provides:
- ✅ Centralized, encrypted secret storage
- ✅ Audit logging of all secret access
- ✅ Easy credential rotation without code changes
- ✅ Integration with Azure Managed Identity (no credentials needed!)
- ✅ Automatic secret expiration and renewal

---

## Prerequisites

1. **Azure Subscription** - You'll need an active Azure subscription
2. **Azure CLI** - Install from https://docs.microsoft.com/cli/azure/install-azure-cli
3. **.NET 8 SDK** - For running the application
4. **Permissions** - Ability to create Azure resources and assign roles

---

## Part 1: Create Azure Key Vault

### Option A: Using Azure Portal (Recommended for First-Time Setup)

1. **Navigate to Azure Portal**: https://portal.azure.com

2. **Create Key Vault:**
   - Click "Create a resource"
   - Search for "Key Vault"
   - Click "Create"

3. **Configure Basic Settings:**
   ```
   Subscription: [Your subscription]
   Resource Group: algotrendy-resources (create new)
   Key Vault Name: algotrendy-vault (must be globally unique)
   Region: East US (or your preferred region)
   Pricing Tier: Standard
   ```

4. **Access Configuration:**
   - Access Policy: Permission model = "Vault access policy"
   - Click "Add Access Policy"
   - Select permissions:
     - **Secret permissions**: Get, List, Set, Delete
   - Select principal: [Your Azure AD account]
   - Click "Add"

5. **Networking:**
   - Allow public access: **Yes** (for development)
   - For production: Configure virtual network rules

6. **Review + Create:**
   - Click "Create"
   - Wait for deployment to complete

7. **Note Your Key Vault URI:**
   - After deployment, go to your Key Vault
   - Copy the "Vault URI" (e.g., `https://algotrendy-vault.vault.azure.net/`)

### Option B: Using Azure CLI (Faster for Experienced Users)

```bash
# Login to Azure
az login

# Set variables
RESOURCE_GROUP="algotrendy-resources"
KEY_VAULT_NAME="algotrendy-vault"  # Must be globally unique
LOCATION="eastus"

# Create resource group
az group create \
  --name $RESOURCE_GROUP \
  --location $LOCATION

# Create Key Vault
az keyvault create \
  --name $KEY_VAULT_NAME \
  --resource-group $RESOURCE_GROUP \
  --location $LOCATION \
  --sku standard

# Grant yourself access
USER_OBJECT_ID=$(az ad signed-in-user show --query id -o tsv)

az keyvault set-policy \
  --name $KEY_VAULT_NAME \
  --object-id $USER_OBJECT_ID \
  --secret-permissions get list set delete

# Display the Vault URI
az keyvault show \
  --name $KEY_VAULT_NAME \
  --query properties.vaultUri \
  -o tsv
```

**Copy the Vault URI** - you'll need it for configuration.

---

## Part 2: Add Secrets to Key Vault

### Secret Naming Convention

Use the following naming pattern for consistency:

| Secret Purpose | Secret Name | Example Value |
|----------------|-------------|---------------|
| Binance API Key | `binance-api-key` | `your_binance_api_key` |
| Binance API Secret | `binance-api-secret` | `your_binance_api_secret` |
| OKX API Key | `okx-api-key` | `your_okx_api_key` |
| OKX API Secret | `okx-api-secret` | `your_okx_api_secret` |
| Database Password | `questdb-password` | `your_database_password` |
| JWT Secret | `jwt-secret` | `generated_random_string` |

### Add Secrets via Azure Portal

1. Navigate to your Key Vault
2. Click "Secrets" in the left menu
3. Click "+ Generate/Import"
4. Fill in:
   ```
   Upload options: Manual
   Name: binance-api-key
   Value: [paste your Binance API key]
   Content type: text/plain (optional)
   Activation date: [leave blank or set]
   Expiration date: [optional - recommended for rotation]
   ```
5. Click "Create"
6. Repeat for all secrets

### Add Secrets via Azure CLI

```bash
KEY_VAULT_NAME="algotrendy-vault"

# Add Binance credentials
az keyvault secret set \
  --vault-name $KEY_VAULT_NAME \
  --name binance-api-key \
  --value "YOUR_BINANCE_API_KEY"

az keyvault secret set \
  --vault-name $KEY_VAULT_NAME \
  --name binance-api-secret \
  --value "YOUR_BINANCE_API_SECRET"

# Add OKX credentials
az keyvault secret set \
  --vault-name $KEY_VAULT_NAME \
  --name okx-api-key \
  --value "YOUR_OKX_API_KEY"

az keyvault secret set \
  --vault-name $KEY_VAULT_NAME \
  --name okx-api-secret \
  --value "YOUR_OKX_API_SECRET"

# Add database password
az keyvault secret set \
  --vault-name $KEY_VAULT_NAME \
  --name questdb-password \
  --value "YOUR_DATABASE_PASSWORD"

# Generate and add JWT secret
JWT_SECRET=$(openssl rand -base64 64)
az keyvault secret set \
  --vault-name $KEY_VAULT_NAME \
  --name jwt-secret \
  --value "$JWT_SECRET"
```

---

## Part 3: Configure AlgoTrendy Application

### Development Environment (Local Machine)

**Method 1: User Secrets (Recommended for Development)**

```bash
cd /root/AlgoTrendy_v2.6/backend/AlgoTrendy.API

# Initialize user secrets
dotnet user-secrets init

# Set Key Vault URI
dotnet user-secrets set "AzureKeyVault:KeyVaultUri" "https://algotrendy-vault.vault.azure.net/"

# Set to use DefaultAzureCredential (uses Azure CLI login)
dotnet user-secrets set "AzureKeyVault:UseManagedIdentity" "true"
```

**Method 2: Environment Variables**

```bash
# Linux/macOS
export AzureKeyVault__KeyVaultUri="https://algotrendy-vault.vault.azure.net/"
export AzureKeyVault__UseManagedIdentity="true"

# Windows PowerShell
$env:AzureKeyVault__KeyVaultUri="https://algotrendy-vault.vault.azure.net/"
$env:AzureKeyVault__UseManagedIdentity="true"
```

**Method 3: appsettings.Development.json** (NOT recommended - easy to commit)

```json
{
  "AzureKeyVault": {
    "KeyVaultUri": "https://algotrendy-vault.vault.azure.net/",
    "UseManagedIdentity": true
  }
}
```

### Production Environment (Azure App Service / Container)

**Option A: Managed Identity (Recommended - No Credentials Needed!)**

1. **Enable Managed Identity on your App Service:**
   ```bash
   RESOURCE_GROUP="algotrendy-resources"
   APP_NAME="algotrendy-api"

   az webapp identity assign \
     --resource-group $RESOURCE_GROUP \
     --name $APP_NAME
   ```

2. **Grant the Managed Identity access to Key Vault:**
   ```bash
   # Get the Managed Identity's principal ID
   PRINCIPAL_ID=$(az webapp identity show \
     --resource-group $RESOURCE_GROUP \
     --name $APP_NAME \
     --query principalId \
     -o tsv)

   # Grant access to Key Vault
   az keyvault set-policy \
     --name algotrendy-vault \
     --object-id $PRINCIPAL_ID \
     --secret-permissions get list
   ```

3. **Set App Service Configuration:**
   ```bash
   az webapp config appsettings set \
     --resource-group $RESOURCE_GROUP \
     --name $APP_NAME \
     --settings \
       AzureKeyVault__KeyVaultUri="https://algotrendy-vault.vault.azure.net/" \
       AzureKeyVault__UseManagedIdentity="true"
   ```

**Option B: Service Principal (Alternative)**

1. **Create Service Principal:**
   ```bash
   SP_NAME="algotrendy-api-sp"

   az ad sp create-for-rbac \
     --name $SP_NAME \
     --role reader \
     --scopes /subscriptions/{subscription-id}/resourceGroups/algotrendy-resources \
     --query "{clientId:appId, clientSecret:password, tenantId:tenant}" \
     -o json
   ```

2. **Save the output** (clientId, clientSecret, tenantId)

3. **Grant Service Principal access to Key Vault:**
   ```bash
   CLIENT_ID="<client-id-from-step-1>"

   az keyvault set-policy \
     --name algotrendy-vault \
     --spn $CLIENT_ID \
     --secret-permissions get list
   ```

4. **Set App Service Configuration:**
   ```bash
   az webapp config appsettings set \
     --resource-group $RESOURCE_GROUP \
     --name $APP_NAME \
     --settings \
       AzureKeyVault__KeyVaultUri="https://algotrendy-vault.vault.azure.net/" \
       AzureKeyVault__UseManagedIdentity="false" \
       AzureKeyVault__TenantId="<tenant-id>" \
       AzureKeyVault__ClientId="<client-id>" \
       AzureKeyVault__ClientSecret="<client-secret>"
   ```

---

## Part 4: Verify Setup

### Test Connection Locally

1. **Login to Azure CLI:**
   ```bash
   az login
   ```

2. **Run the application:**
   ```bash
   cd /root/AlgoTrendy_v2.6/backend/AlgoTrendy.API
   dotnet run
   ```

3. **Check logs** for successful Key Vault connection:
   ```
   [INFO] Azure Key Vault integrated as configuration source. Vault: https://algotrendy-vault.vault.azure.net/
   [INFO] Azure Key Vault Secrets Service initialized. Vault: https://algotrendy-vault.vault.azure.net/, UseManagedIdentity: True
   ```

### Test Secret Retrieval

```bash
# Install Azure CLI extension
az extension add --name keyvault-preview

# Test secret retrieval
az keyvault secret show \
  --vault-name algotrendy-vault \
  --name binance-api-key \
  --query value \
  -o tsv
```

### Test from Application Code

The application will automatically load secrets from Key Vault when configured. Check logs for:
```
[INFO] Loaded credentials for broker binance from Azure Key Vault
```

---

## Part 5: Secret Rotation

### Manual Rotation

1. **Generate new API key** from broker (Binance/OKX)

2. **Update secret in Key Vault:**
   ```bash
   az keyvault secret set \
     --vault-name algotrendy-vault \
     --name binance-api-key \
     --value "NEW_API_KEY"
   ```

3. **Restart application** (secrets are cached for 60 minutes by default)

### Automatic Rotation (Advanced)

Use Azure Key Vault's automatic rotation feature:

1. Set expiration dates on secrets
2. Configure rotation policy
3. Use Azure Functions to automate key regeneration

---

## Part 6: Audit and Monitoring

### Enable Diagnostic Logging

```bash
# Create Log Analytics Workspace
az monitor log-analytics workspace create \
  --resource-group algotrendy-resources \
  --workspace-name algotrendy-logs

# Get workspace ID
WORKSPACE_ID=$(az monitor log-analytics workspace show \
  --resource-group algotrendy-resources \
  --workspace-name algotrendy-logs \
  --query id \
  -o tsv)

# Enable Key Vault diagnostics
az monitor diagnostic-settings create \
  --resource $(az keyvault show --name algotrendy-vault --query id -o tsv) \
  --name KeyVaultDiagnostics \
  --workspace $WORKSPACE_ID \
  --logs '[{"category": "AuditEvent", "enabled": true}]'
```

### Query Audit Logs

```kusto
// In Azure Portal > Log Analytics Workspace > Logs

AzureDiagnostics
| where ResourceProvider == "MICROSOFT.KEYVAULT"
| where OperationName == "SecretGet" or OperationName == "SecretList"
| project TimeGenerated, CallerIPAddress, OperationName, ResultSignature, requestUri_s
| order by TimeGenerated desc
```

---

## NuGet Packages Required

The following packages are required (already included in project):

```xml
<PackageReference Include="Azure.Identity" Version="1.10.4" />
<PackageReference Include="Azure.Security.KeyVault.Secrets" Version="4.5.0" />
<PackageReference Include="Azure.Extensions.AspNetCore.Configuration.Secrets" Version="1.3.0" />
```

---

## Troubleshooting

### Error: "The user, group or application does not have secrets get permission"

**Solution:** Grant yourself access to the Key Vault:
```bash
az keyvault set-policy \
  --name algotrendy-vault \
  --object-id $(az ad signed-in-user show --query id -o tsv) \
  --secret-permissions get list
```

### Error: "Key Vault URI is not configured"

**Solution:** Set the URI in appsettings or user secrets:
```bash
dotnet user-secrets set "AzureKeyVault:KeyVaultUri" "https://algotrendy-vault.vault.azure.net/"
```

### Error: "DefaultAzureCredential failed to retrieve a token"

**Solution:** Login to Azure CLI:
```bash
az login
az account show  # Verify you're logged in
```

### Secrets Not Loading

**Solution:** Check cache duration and restart application:
- Secrets are cached for 60 minutes by default
- Restart application to force reload
- Or clear cache via `ISecretsService.ClearCache()`

---

## Security Best Practices

1. ✅ **Never commit secrets to git** - use .gitignore for sensitive files
2. ✅ **Use Managed Identity in production** - eliminates credentials
3. ✅ **Rotate secrets regularly** - set expiration dates
4. ✅ **Enable audit logging** - monitor secret access
5. ✅ **Restrict Key Vault access** - use principle of least privilege
6. ✅ **Use network restrictions** - limit access to specific IPs/VNets in production
7. ✅ **Enable soft-delete** - recover accidentally deleted secrets

---

## Cost Estimate

| Resource | Pricing | Monthly Cost (Estimate) |
|----------|---------|-------------------------|
| Key Vault (Standard) | $0.03/10,000 operations | ~$0.50 |
| Secrets | $0.03/10,000 operations | ~$0.10 |
| **Total** | | **~$0.60/month** |

Key Vault is extremely cost-effective for the security benefits it provides!

---

## Next Steps

1. ✅ Complete this setup guide
2. ✅ Test secret retrieval locally
3. ✅ Deploy to production with Managed Identity
4. ✅ Enable audit logging
5. ✅ Remove all hardcoded credentials from code
6. ✅ Add secret rotation schedule to calendar

---

## Support

For issues with Azure Key Vault setup:
- Azure Documentation: https://docs.microsoft.com/azure/key-vault/
- AlgoTrendy Team: dev@algotrendy.com

**Status:** Production-Ready
**Last Updated:** October 19, 2025
