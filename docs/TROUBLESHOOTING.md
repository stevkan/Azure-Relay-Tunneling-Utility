# Troubleshooting Guide

This guide covers common issues encountered when using Azure Relay Tunneling Utility for both WCF and Hybrid Connection implementations.

## Table of Contents

- [Configuration Issues](#configuration-issues)
- [Authentication Issues](#authentication-issues)
- [Network and Connectivity Issues](#network-and-connectivity-issues)
- [Azure Resource Issues](#azure-resource-issues)
- [Runtime Issues](#runtime-issues)
- [Azure Bot Integration Issues](#azure-bot-integration-issues)
- [TypeScript / Node.js Specific Issues](#typescript--nodejs-specific-issues)

---

## Configuration Issues

### Problem: "appsettings.json not found" (or .env issues)

**For .NET Version:**
**Symptoms:**
- Application fails to start
- Error message about missing configuration file

**Solutions:**
1. Ensure you've renamed `appsettings-template.json` to `appsettings.json`
2. Verify the file is in the same directory as the executable

**For TypeScript Version:**
**Symptoms:**
- Application starts but connects to nothing
- Validation errors for missing environment variables

**Solutions:**
1. Ensure you've copied `.env.template` to `.env`
2. Verify `.env` is in the root of the project or executable directory

---

### Problem: JSON syntax errors in appsettings.json

**Symptoms:**
- Application crashes on startup
- Parsing error messages

**Solutions:**
1. Validate JSON syntax using an online validator or IDE
2. Common mistakes:
   - Missing commas between properties
   - Extra commas after the last property in an object
   - Unclosed quotes or brackets
   - Smart quotes instead of straight quotes (`"` vs `"`)
3. Compare your file to `appsettings-template.json`
4. Use a JSON formatter/linter

**Example of valid JSON:**
```json
{
  "Relays": [
    {
      "RelayNamespace": "namespace",
      "RelayName": "relay-name",
      "IsEnabled": true
    }
  ]
}
```

---

### Problem: Multiple relays not starting

**Symptoms:**
- Only some relays start
- No error messages for disabled relays

**Solutions:**
1. Check `IsEnabled: true` for each relay you want to run
2. Ensure each relay has a unique `RelayName`
3. Verify all required fields are populated for each relay
4. Check console output for which relays are enabled

---

### Problem: Relay name case warnings (Hybrid Connection only)

**Symptoms:**
- Warning: "Relay name contains uppercase letters"
- Automatic conversion to lowercase

**Solutions:**
1. Update your `RelayName` in appsettings.json to use only lowercase letters
2. Azure Relay requires lowercase names - the app will auto-convert but it's better to fix the config
3. Update your Azure Bot messaging endpoint if the case changed

---

## Authentication Issues

### Problem: "Unable to authenticate to Azure"

**Symptoms:**
- Dynamic resource creation fails
- Authentication errors on startup

**Solutions:**

**For Azure CLI authentication:**
```bash
# Check if you're logged in
az account show

# Login if needed
az login

# Verify correct subscription
az account list --output table

# Set correct subscription
az account set --subscription "your-subscription-id"
```

**For Service Principal:**
- Verify `ClientId` and `ClientSecret` are correct
- Ensure `UseDefaultAzureCredential: false` is set
- Check TenantId matches your Azure AD tenant
- Verify the service principal hasn't expired

**For Managed Identity:**
- Confirm Managed Identity is enabled on the Azure resource
- Ensure permissions have been granted
- Restart the Azure resource after enabling identity

📚 **See:** [AUTHENTICATION.md](AUTHENTICATION.md) for detailed authentication setup

---

### Problem: "The Resource 'Microsoft.Relay/namespaces/...' was not found"

**Symptoms:**
- Error during dynamic resource creation
- Subscription/resource mismatch errors

**Solutions:**
1. **Most Common Cause - Subscription Mismatch:**
   ```bash
   # Check current subscription
   az account show
   
   # Set correct subscription
   az account set --subscription "your-subscription-id"
   ```

2. Verify `SubscriptionId` in appsettings.json matches your Azure CLI subscription
3. Ensure the relay namespace actually exists in Azure
4. Check that `ResourceGroupName` is correct
5. Verify the namespace is an **Azure Relay** namespace, not Service Bus

---

### Problem: "Insufficient permissions" or "Authorization failed"

**Symptoms:**
- Can authenticate but can't create/delete resources
- Permission denied errors

**Solutions:**
1. Verify role assignment exists:
   ```bash
   az role assignment list --assignee {your-email-or-principal-id} --output table
   ```

2. Grant appropriate permissions:
   ```bash
   # Option A: Contributor on Resource Group (recommended)
   az role assignment create \
     --assignee {email-or-principal-id} \
     --role Contributor \
     --scope /subscriptions/{sub-id}/resourceGroups/{rg-name}
   
   # Option B: Azure Relay Owner (minimum permission)
   az role assignment create \
     --assignee {email-or-principal-id} \
     --role "Azure Relay Owner" \
     --scope /subscriptions/{sub-id}/resourceGroups/{rg-name}/providers/Microsoft.Relay/namespaces/{namespace}
   ```

3. Wait 5-10 minutes for permissions to propagate
4. Verify the scope is correct (resource group or namespace)

---

## Network and Connectivity Issues

### Problem: "Unable to connect to relay"

**Symptoms:**
- Relay starts but can't establish connection
- Timeout errors

**Solutions:**
1. Check internet connectivity
2. Verify firewall isn't blocking outbound HTTPS (port 443)
3. If behind corporate proxy:
   - Configure proxy settings in your environment
   - Some corporate proxies block WebSocket connections
4. Check Azure Relay namespace is accessible:
   ```bash
   nslookup [your-namespace].servicebus.windows.net
   ```

---

### Problem: Target service not reachable

**Symptoms:**
- Relay connects but requests fail
- "Connection refused" errors
- 502/504 gateway errors

**Solutions:**
1. Verify target service is running:
   - Check `http://localhost:3978` (or your configured port) in browser
   - Ensure the service started before the relay

2. Verify `TargetServiceAddress` in appsettings.json:
   - Correct port number
   - Correct protocol (http vs https)
   - Trailing slash if required by your service

3. Check firewall rules on localhost
4. For bots, ensure the bot endpoint includes `/api/messages` path

---

### Problem: Relay connects but no traffic

**Symptoms:**
- Relay reports "listening" but no requests arrive
- Azure Bot shows errors

**Solutions:**
1. Verify Azure Bot messaging endpoint is configured correctly:
   - Should be: `https://[your-namespace].servicebus.windows.net/relay-name/api/messages`
   - Check for typos in namespace or relay name
   - Ensure no extra spaces

2. Test the bot in Test Web Chat (Azure Portal)
3. Check relay name matches exactly (case-sensitive for WCF)
4. Verify the relay is actually visible in Azure Portal (for dynamic creation)

---

## Azure Resource Issues

### Problem: Dynamic resource creation fails

**Symptoms:**
- Error creating hybrid connection
- Resource creation timeout

**Solutions:**
1. Verify Azure credentials and permissions (see Authentication Issues)
2. Check resource group name is correct
3. Ensure relay namespace exists in the specified resource group
4. Verify namespace is **Azure Relay**, not Service Bus namespace
5. Check Azure region availability and quotas
6. Review Azure service health status

---

### Problem: Resource not deleted on shutdown

**Symptoms:**
- Hybrid connection remains in Azure after app stops
- Cleanup fails

**Solutions:**
1. Check console output for deletion errors
2. Manually delete the hybrid connection in Azure Portal
3. Verify the service principal/identity has delete permissions
4. Ensure app shutdown gracefully (Ctrl+C or Enter, not killed)

---

### Problem: "Relay name already exists"

**Symptoms:**
- Can't create relay with dynamic creation
- Name conflict errors

**Solutions:**
1. Choose a different relay name
2. Delete the existing hybrid connection in Azure Portal
3. Ensure previous instance of the app was properly shut down
4. Check if another app is using the same relay name

---

## Runtime Issues

### Problem: High latency or slow responses

**Symptoms:**
- Requests take longer than expected
- Timeouts

**Solutions:**
1. Azure Relay adds network latency - this is expected
2. Check target service performance
3. Verify network connection quality
4. Consider Azure region proximity to your location
5. Monitor Azure Relay metrics in Azure Portal

---

### Problem: Intermittent disconnections

**Symptoms:**
- Relay connection drops randomly
- Need to restart frequently

**Solutions:**
1. Check network stability
2. Verify Azure service health
3. Monitor for pattern (time of day, after certain duration)
4. Check for aggressive firewall/proxy connection timeouts
5. Review Azure Relay connection limits and quotas

---

### Problem: "Policy key is invalid"

**Symptoms:**
- Authentication to relay fails
- Unauthorized errors

**Solutions:**
1. Verify `PolicyKey` in appsettings.json is correct
2. Copy key directly from Azure Portal:
   - Azure Portal → Relay namespace → Shared access policies
   - Copy the primary or secondary key
3. Ensure no extra spaces or line breaks in the key
4. Check `PolicyName` matches (usually "RootManageSharedAccessKey")
5. Verify the policy hasn't been deleted or regenerated

---

## Azure Bot Integration Issues

### Problem: Bot doesn't receive messages

**Symptoms:**
- Bot shows as online but doesn't respond
- Messages timeout in Test Web Chat

**Solutions:**
1. Verify messaging endpoint in Azure Bot Settings:
   ```
   https://[your-namespace].servicebus.windows.net/[your-relay-name]/api/messages
   ```

2. Ensure relay is running and shows "listening"
3. Check target bot service is running on the configured port
4. Verify bot handles POST requests to `/api/messages`
5. Check bot logs for errors
6. Test local bot directly: `POST http://localhost:3978/api/messages`

---

### Problem: ChannelData missing or incorrect

**Symptoms:**
- Expected channel-specific data not present
- Bot behavior differs from production

**Solutions:**
1. Verify you're testing through the correct channel
2. Some channel data only appears in specific channels (Teams, Slack, etc.)
3. Check bot is configured for the channel in Azure
4. Review request logging (enable `EnableDetailedLogging: true` for WCF)

---

## Debugging Tips

### Enable Detailed Logging

**For WCF Relay:**
```json
{
  "EnableDetailedLogging": true
}
```

**For Hybrid Connection:**
- Check console output for request/response details
- Monitor Azure Relay metrics in Azure Portal

### Test Components Independently

1. **Test local service directly:**
   ```bash
   curl -X POST http://localhost:3978/api/messages \
     -H "Content-Type: application/json" \
     -d '{"text": "test"}'
   ```

2. **Verify Azure credentials:**
   ```bash
   az account show
   az relay namespace list --resource-group your-rg
   ```

3. **Check relay visibility:**
   - Azure Portal → Relay namespace → Hybrid Connections (or WCF Relays)
   - Should see your relay listed when app is running

### Common Environment Variables

Set these for additional debugging:

```bash
# Enable Azure SDK logging
export AZURE_LOG_LEVEL=verbose

# .NET Core detailed logging
export DOTNET_ENVIRONMENT=Development
```

---

## TypeScript / Node.js Specific Issues

### Problem: 502 Bad Gateway with DirectLine/Web Chat

**Symptoms:**
- Web Chat shows "Send failed" or "Retry"
- Browser console shows 502 errors for WebSocket messages
- Messages may still arrive but are reported as failed to the client

**Cause:**
There is a known compatibility issue between the Azure Relay Node.js library (`hyco-ws`) and the DirectLine WebSocket protocol.

**Workarounds:**
1. **Ignore the errors:** Often the message is actually delivered despite the 502 error.
2. **Disable WebSocket in Web Chat:** Force Web Chat to use polling mode.
   ```js
   window.WebChat.renderWebChat({
     directLine: window.WebChat.createDirectLine({ token: '...' }),
     webSocket: false // Force polling
   }, document.getElementById('webchat'));
   ```
3. **Use the .NET Version:** The .NET implementation does not have this issue and is recommended for DirectLine/Web Chat use.

### Problem: "Invalid relay configuration format"

**Symptoms:**
- Startup error: "Invalid relay configuration"
- Regex validation failure

**Solutions:**
1. Ensure you are using **pipes** (`|`) as delimiters, NOT colons.
   - ✅ Correct: `ns|name|policy|key|http://localhost...`
   - ❌ Incorrect: `ns:name:policy:key...`
2. Check that you have all required fields (11 fields total).
3. Ensure empty fields (like `resourceGroup` if not dynamic) are still delimited (e.g., `...|false||true|...`).

---

## Getting Additional Help

If you're still experiencing issues:

1. **Check related documentation:**
   - [AUTHENTICATION.md](AUTHENTICATION.md) - Authentication setup
   - [COMPARISON.md](COMPARISON.md) - Understanding WCF vs Hybrid Connection
   - Project-specific READMEs

2. **Review Azure service health:**
   - [Azure Status](https://status.azure.com/)
   - Check for regional outages

3. **Verify Azure quotas and limits:**
   - [Azure Relay limits](https://docs.microsoft.com/azure/azure-relay/relay-faq#what-are-the-quotas-and-limits-for-azure-relay)

4. **Create an issue:**
   - Include error messages
   - Include configuration (redact secrets)
   - Include steps to reproduce
   - Specify which project (WCF or Hybrid Connection)

5. **Azure Support:**
   - For Azure-specific issues (authentication, permissions, quotas)
   - [Azure Support](https://azure.microsoft.com/support/)
