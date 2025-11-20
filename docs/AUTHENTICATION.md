# Azure Authentication Guide

This guide covers Azure authentication for **RelayTunnelUsingHybridConnection** when using dynamic resource creation (`DynamicResourceCreation: true`).

## Quick Start

**For Local Development:**
```bash
az login
az account set --subscription "your-subscription-id"
```
Set in `appsettings.json` (.NET) or `.env` (TypeScript):

**.NET:**
```json
"AzureManagement": {
  "SubscriptionId": "your-subscription-id",
  "UseDefaultAzureCredential": true
}
```

**TypeScript:**
```bash
AZURE_SUBSCRIPTION_ID=your-subscription-id
AZURE_USE_DEFAULT_CREDENTIAL=true
```

**For Production:**
Create a Service Principal and set:

**.NET:**
```json
"AzureManagement": {
  "SubscriptionId": "your-subscription-id",
  "UseDefaultAzureCredential": false,
  "ClientId": "your-client-id",
  "ClientSecret": "your-client-secret"
}
```

**TypeScript:**
```bash
AZURE_SUBSCRIPTION_ID=your-subscription-id
AZURE_USE_DEFAULT_CREDENTIAL=false
AZURE_CLIENT_ID=your-client-id
AZURE_CLIENT_SECRET=your-client-secret
```

**Required Permission:** Contributor role on Resource Group (or Azure Relay Owner on namespace)

---

## When Authentication is Required

Azure authentication is **only required** if you're using `DynamicResourceCreation: true` to automatically create/delete hybrid connections via ARM templates.

If using static resources (`DynamicResourceCreation: false`), you only need the SAS key - no additional authentication required.

---

## Configuration Reference

### .NET Version (appsettings.json)

```json
{
  "AzureManagement": {
    "SubscriptionId": "your-azure-subscription-id",     // Required
    "TenantId": "your-azure-tenant-id",                 // Optional
    "UseDefaultAzureCredential": true,                  // true = Azure CLI/Managed Identity
    "ClientId": "",                                     // Required if UseDefaultAzureCredential=false
    "ClientSecret": ""                                  // Required if UseDefaultAzureCredential=false
  }
}
```

### TypeScript Version (.env)

For the TypeScript implementation, configuration uses environment variables (or a `.env` file):

```bash
# Azure Management
AZURE_SUBSCRIPTION_ID=your-subscription-id
AZURE_TENANT_ID=your-tenant-id
AZURE_USE_DEFAULT_CREDENTIAL=true                  # true = Azure CLI/Managed Identity

# Service Principal (if UseDefaultAzureCredential=false)
AZURE_CLIENT_ID=your-client-id
AZURE_CLIENT_SECRET=your-client-secret
```

---

## Authentication Methods

### Method Comparison

| Method | Best For | Pros | Cons |
|--------|----------|------|------|
| **Azure CLI** | Development | Easy setup, no secrets to manage | Requires periodic login |
| **Service Principal** | Production | Automated, auditable | Secrets to manage |
| **Managed Identity** | Azure VMs/App Service | No secrets, most secure | Azure infrastructure only |

<details>
<summary><strong>Option 1: Azure CLI (Development)</strong></summary>

### Azure CLI Authentication

**Setup:**

1. Install Azure CLI: [Download](https://docs.microsoft.com/cli/azure/install-azure-cli)

2. Login:
   ```bash
   az login
   ```

3. Verify and set subscription:
   ```bash
   az account show
   az account set --subscription "your-subscription-id"
   ```

4. Configure application:

   **.NET (`appsettings.json`):**
   ```json
   {
     "AzureManagement": {
       "SubscriptionId": "your-subscription-id",
       "UseDefaultAzureCredential": true
     }
   }
   ```

   **TypeScript (`.env`):**
   ```bash
   AZURE_SUBSCRIPTION_ID=your-subscription-id
   AZURE_USE_DEFAULT_CREDENTIAL=true
   ```

**Pros:**
- ✅ Easiest setup for local development
- ✅ No client secrets to manage
- ✅ Works with MFA-enabled accounts

**Cons:**
- ❌ Not for production/CI/CD
- ❌ Requires interactive login periodically
</details>

<details>
<summary><strong>Option 2: Service Principal (Production)</strong></summary>

### Service Principal Authentication

**Setup:**

1. Create Service Principal:
   ```bash
   az ad sp create-for-rbac --name "relay-tunnel-sp" \
     --role Contributor \
     --scopes /subscriptions/{subscription-id}/resourceGroups/{resource-group-name}
   ```

2. Copy the output:
   ```json
   {
     "appId": "12345678-1234-1234-1234-123456789012",
     "password": "your-client-secret",
     "tenant": "your-tenant-id"
   }
   ```

3. Configure application:

   **.NET (`appsettings.json`):**
   ```json
   {
     "AzureManagement": {
       "SubscriptionId": "your-subscription-id",
       "TenantId": "your-tenant-id",
       "UseDefaultAzureCredential": false,
       "ClientId": "12345678-1234-1234-1234-123456789012",
       "ClientSecret": "your-client-secret"
     }
   }
   ```

   **TypeScript (`.env`):**
   ```bash
   AZURE_SUBSCRIPTION_ID=your-subscription-id
   AZURE_TENANT_ID=your-tenant-id
   AZURE_USE_DEFAULT_CREDENTIAL=false
   AZURE_CLIENT_ID=12345678-1234-1234-1234-123456789012
   AZURE_CLIENT_SECRET=your-client-secret
   ```

**Security:**
- Store secrets in Azure Key Vault or secure secret management
- Use separate service principals for dev/staging/production
- Rotate credentials regularly
- Never commit to source control

**Pros:**
- ✅ Production-ready
- ✅ Works in CI/CD pipelines
- ✅ Auditable

**Cons:**
- ❌ Secrets to manage securely
</details>

<details>
<summary><strong>Option 3: Managed Identity (Azure Infrastructure)</strong></summary>

### Managed Identity Authentication

**For Azure VMs or App Services only**

**Setup:**

1. Enable Managed Identity on your Azure resource:
   - VM: Azure Portal → VM → Identity → System assigned → Status: On
   - App Service: Azure Portal → App Service → Identity → System assigned → Status: On

2. Grant permissions:
   ```bash
   az role assignment create \
     --assignee {managed-identity-principal-id} \
     --role Contributor \
     --scope /subscriptions/{subscription-id}/resourceGroups/{resource-group-name}
   ```

3. Configure application:

   **.NET (`appsettings.json`):**
   ```json
   {
     "AzureManagement": {
       "SubscriptionId": "your-subscription-id",
       "UseDefaultAzureCredential": true
     }
   }
   ```

   **TypeScript (`.env`):**
   ```bash
   AZURE_SUBSCRIPTION_ID=your-subscription-id
   AZURE_USE_DEFAULT_CREDENTIAL=true
   ```

**Pros:**
- ✅ No credentials to manage
- ✅ Most secure option
- ✅ Azure handles authentication

**Cons:**
- ❌ Only works on Azure infrastructure
- ❌ Not for local development
</details>

---

## Required Azure Permissions

Grant **one of the following** to your identity:

### Option A: Contributor on Resource Group (Recommended)
```bash
az role assignment create \
  --assignee {principal-id-or-email} \
  --role Contributor \
  --scope /subscriptions/{subscription-id}/resourceGroups/{resource-group-name}
```

### Option B: Azure Relay Owner (Minimum)
```bash
az role assignment create \
  --assignee {principal-id-or-email} \
  --role "Azure Relay Owner" \
  --scope /subscriptions/{sub-id}/resourceGroups/{rg}/providers/Microsoft.Relay/namespaces/{namespace}
```

---

## How DefaultAzureCredential Works

When `UseDefaultAzureCredential: true`, the SDK tries authentication in this order:

1. Environment Variables (`AZURE_CLIENT_ID`, `AZURE_TENANT_ID`, `AZURE_CLIENT_SECRET`)
2. Managed Identity (if on Azure VM/App Service)
3. Visual Studio (if logged in)
4. Azure CLI (`az login`)
5. Azure PowerShell
6. Interactive Browser (last resort)

This flexibility allows the same config to work across dev/staging/production.

---

## Common Issues

### "Unable to authenticate"
```bash
az account show  # Verify you're logged in
az account set --subscription "your-id"  # Set correct subscription
```

### "The Resource 'Microsoft.Relay/namespaces/...' was not found"
**Subscription mismatch:**
```bash
az account show  # Check current subscription
az account set --subscription "your-subscription-id"
```

### "Insufficient permissions"
- Verify role assignment exists
- Wait 5-10 minutes for permissions to propagate
- Check scope is correct (resource group or namespace)

### Service Principal fails
- Ensure `UseDefaultAzureCredential: false`
- Verify ClientId and ClientSecret are correct
- Check service principal hasn't expired

### Managed Identity not detected
- Verify identity is enabled on Azure resource
- Restart Azure resource after enabling
- Check network connectivity to Azure metadata endpoint

---

## Security Best Practices

**Credential Management:**
- Never commit credentials to source control
- Store Service Principal secrets in Azure Key Vault
- Rotate Service Principal credentials every 90-180 days

**Least Privilege:**
- Grant minimum required permissions (Azure Relay Owner > Contributor > Owner)
- Scope to resource group, not entire subscription
- Use separate identities for dev/staging/production

**Monitoring:**
- Enable Azure AD sign-in logs
- Monitor service principal usage
- Set alerts for suspicious authentication patterns

---

## Additional Resources

- [Azure Identity SDK](https://docs.microsoft.com/dotnet/api/azure.identity)
- [DefaultAzureCredential](https://docs.microsoft.com/dotnet/api/azure.identity.defaultazurecredential)
- [Azure CLI Authentication](https://docs.microsoft.com/cli/azure/authenticate-azure-cli)
- [Managed Identity Overview](https://docs.microsoft.com/azure/active-directory/managed-identities-azure-resources/overview)
