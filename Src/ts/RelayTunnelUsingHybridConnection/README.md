# Azure Relay Tunnel (TypeScript/Node.js)

**Version: 0.9.0-beta.3**

> **🚨 BETA VERSION - KNOWN ISSUES**
>
> **This TypeScript implementation is currently in beta and has known issues when connecting to DirectLine clients (e.g., Web Chat):**
> 
> - **Outgoing Activities (Client → Bot):**
>   - Messages may fail to send, resulting in **502 Bad Gateway** errors
>   - Messages may be delayed by several seconds before being processed
> 
> - **Incoming Activities (Bot → Client):**
>   - Conversation update activities (indicating conversation start) fail to be received
>   - No obvious error messages are produced when this occurs
> 
> - **WebSocket Connection Issues:**
>   - When DirectLine's `webSocket` property is set to `true` (default), every successfully sent message produces a **502 Bad Gateway** error in the browser console
>     - **Note:** These 502 errors on successful messages don't appear to impact service and can safely be ignored
>   - Setting `webSocket` to `false` (polling mode) eliminates the 502 errors on successful messages, but delayed/failed messages still occur
> 
> **Recommendation:** For production use with DirectLine/Web Chat, use the [.NET Hybrid Connection implementation](../../dotnet/RelayTunnelUsingHybridConnection/README.md) instead, which does not have these issues.

A TypeScript/Node.js implementation of the Azure Relay **Hybrid Connection** tunneling utility. This cross-platform tool forwards HTTP and WebSocket traffic from Azure to your local machine.

**⚠️ WCF Relay Not Supported:** This implementation only supports Hybrid Connections. WCF Relay is .NET-specific technology with no Node.js/TypeScript libraries available. For WCF Relay, use the [.NET version](../dotnet/RelayTunnelUsingWCF/README.md).

**⚠️ Library Maintenance Notice:** This implementation uses Microsoft's official `hyco-ws` (v1.0.5, March 2017) and `hyco-https` (v1.4.5, Feb 2021) libraries for Azure Relay Hybrid Connections. While these libraries are unmaintained, they remain the only officially supported way to use Hybrid Connections from Node.js. The libraries are stable and functional, but users should be aware of their age. For the most actively maintained relay implementation, consider the [.NET version](../../dotnet/RelayTunnelUsingHybridConnection/README.md).

## 🎯 Overview

An HTTP/WebSocket tunneling utility that forwards traffic from Azure to your local services. Perfect for:
- Exposing local web servers, APIs, or services through Azure endpoints
- Debugging bots and agents locally while receiving real traffic from Azure-hosted channels
- Testing with real ChannelData from channels like WebChat, Teams, Skype
- Development and testing without deploying to Azure

## ✨ Features

✅ **Cross-Platform**: Runs on Windows, Linux, and macOS (Node.js 20+)
✅ **HTTP & WebSocket Support**: Full support for both protocols
✅ **Multi-Relay Support**: Configure and run multiple relays simultaneously
✅ **Auto-Provisioning**: Optional dynamic Hybrid Connection creation/deletion
✅ **CLI Support**: Command-line arguments for configuration and verbosity
✅ **Type-Safe Configuration**: Zod validation for environment variables
✅ **Structured Logging**: Pino logger with pretty output
✅ **Graceful Shutdown**: Proper cleanup of resources on exit

## 📋 Prerequisites

- **Node.js 20+** (required)
- **npm** or **pnpm**
- Azure Relay namespace with SAS policy credentials
- Local bot/agent service to proxy requests to

## 🚀 Installation

### From Source

```bash
cd Src/ts
npm install
npm run build
```

### Run Development Mode

```bash
npm start
# or with file watching
npm run dev
```

### Build Executable

```bash
# Build for all platforms
npm run pkg:all

# Executables will be in ./bin/
# - relay-tunnel-win.exe (Windows)
# - relay-tunnel-linux (Linux)
# - relay-tunnel-macos (macOS)
# - .env.template
```

## ⚙️ Configuration

### Step 1: Setup Environment File

Copy `.env.template` to `.env` (this file is in .gitignore to prevent accidental credential commits):

```bash
cp .env.template .env
```

### Step 2: Configure Your Relays

Update `.env` with your Azure Relay settings:

```bash
# Azure Management Configuration (required for dynamic resource creation)
AZURE_SUBSCRIPTION_ID=your-subscription-id
AZURE_TENANT_ID=your-tenant-id
AZURE_CLIENT_ID=your-client-id
AZURE_CLIENT_SECRET=your-client-secret
AZURE_USE_DEFAULT_CREDENTIAL=true

# Relay Configuration
# Format: namespace|name|policyName|policyKey|targetUrl|enabled|detailedLogging|dynamic|resourceGroup|requiresAuth|description
RELAYS=common-relay|my-bot|RootManageSharedAccessKey|abc123==|http://localhost:3978/|true|true|false||true|Bot Relay

# Optional
SHUTDOWN_TIMEOUT_SECONDS=30
```

### Multiple Relays

Separate multiple relays with semicolons (`;`):

```bash
RELAYS=namespace1|relay1|policy|key1|http://localhost:3978/|true|true|false||true|Relay1;namespace2|relay2|policy|key2|http://localhost:8080/|true|false|false||true|Relay2
```

### Configuration Format

**Relay Format:** `namespace|name|policyName|policyKey|targetUrl|enabled|detailedLogging|dynamic|resourceGroup|requiresAuth|description`

**Note:** Fields are separated by pipe (`|`) to avoid conflicts with colons in URLs.

#### Environment Variables

| Variable | Required | Description |
|----------|----------|-------------|
| `AZURE_SUBSCRIPTION_ID` | Yes* | Azure subscription ID (required if using dynamic resource creation) |
| `AZURE_TENANT_ID` | Yes* | Azure AD tenant ID (required if not using DefaultAzureCredential) |
| `AZURE_CLIENT_ID` | Yes* | Service principal client ID (required if not using DefaultAzureCredential) |
| `AZURE_CLIENT_SECRET` | Yes* | Service principal client secret (required if not using DefaultAzureCredential) |
| `AZURE_USE_DEFAULT_CREDENTIAL` | No | Use Azure CLI/Managed Identity for auth. Default: `true` |
| `RELAYS` | Yes | Relay configurations (see format above) |
| `SHUTDOWN_TIMEOUT_SECONDS` | No | Shutdown timeout in seconds. Default: `30` |

#### Relay Fields

| Position | Field | Required | Description |
|----------|-------|----------|-------------|
| 1 | namespace | Yes | Relay namespace name (e.g., "common-relay") |
| 2 | name | Yes | Hybrid Connection name (auto-converted to lowercase) |
| 3 | policyName | Yes | SAS policy name (usually "RootManageSharedAccessKey") |
| 4 | policyKey | Yes | SAS policy key |
| 5 | targetUrl | Yes | Local service URL (e.g., "http://localhost:3978/") |
| 6 | enabled | No | `true` or `false`. Default: `true` |
| 7 | detailedLogging | No | `true` or `false`. Default: `true` |
| 8 | dynamic | No | Auto-create/delete connection. `true` or `false`. Default: `false` |
| 9 | resourceGroup | Yes* | Azure resource group (required if dynamic is `true`) |
| 10 | requiresAuth | No | Require client authorization. `true` or `false`. Default: `true` |
| 11 | description | No | Description/metadata for the connection |

## 🎮 Usage

### CLI Options

```bash
# Use default .env file
npm start

# Specify custom env file
npm start -- --env-file /path/to/.env.production

# Enable verbose logging
npm start -- --verbose

# Show help
npm start -- --help

# Show version
npm start -- --version
```

### Using Built Executable

```bash
# Windows
relay-tunnel.exe --env-file .env --verbose

# Linux/macOS
./relay-tunnel --env-file .env --verbose
```

### Configure Azure Bot Messaging Endpoint

1. Login to the Azure portal and open your Azure Bot's registration
2. Select **Settings** under Bot management
3. In the **Messaging endpoint** field, enter:
   ```
   https://[your-relay-namespace].servicebus.windows.net/[your-relay-name]/api/messages
   ```
4. Click **Save**

### Test Your Bot

1. Start your local bot/agent on the configured target address (e.g., `http://localhost:3978`)
2. Run the relay tunnel utility
3. Test your bot/agent on a channel (Test in Web Chat, Skype, Teams, etc.)

### Stop the Application

Press **Ctrl+C** to gracefully stop all services. Dynamic Hybrid Connections will be automatically deleted.

## 📝 Example Output

```
Azure Relay Hybrid Connection Utility (TypeScript/Node.js)
============================================================

Initializing Azure Resource Manager for dynamic resource management...
✓ Azure Resource Manager initialized
Found 1 enabled relay configuration(s):
  • my-relay → http://localhost:3978/ (Dynamic: true)

Starting relay: my-relay
  Namespace: common-relay
  Target: http://localhost:3978/
  Dynamic: true
Creating Hybrid Connection 'my-relay'...
✓ Created 'my-relay'
✓ Relay 'my-relay' started successfully
✓ WebSocket relay listening for 'my-relay'

Press Ctrl+C to stop...
```

## 🔄 Dynamic Resource Provisioning

When `dynamicResourceCreation` is enabled:

1. **Startup**: Hybrid Connection is automatically created in Azure
2. **Runtime**: Relay listens for HTTP/WebSocket connections
3. **Shutdown**: Hybrid Connection is automatically deleted from Azure

This is useful for:
- Temporary development environments
- CI/CD pipelines
- Testing scenarios

## 🔐 Security Notes

- Store SAS keys securely (consider using Azure Key Vault)
- Use `AZURE_USE_DEFAULT_CREDENTIAL=true` when running in Azure (Managed Identity)
- The relay uses HTTPS for external access
- The `.env` file is in .gitignore to prevent credential leaks

## 🐛 Troubleshooting

### Configuration Errors
- Verify all .env settings are configured correctly
- Check validation errors in console output
- Ensure you've copied `.env.template` to `.env`
- Check relay format: `namespace|name|policy|key|url|enabled|logging|dynamic|rg|auth|desc`
- Use pipe (`|`) as delimiter, not colon (`:`) - colons conflict with URLs

### Authentication Issues
- Check SAS key and policy permissions
- Verify Azure credentials if using dynamic provisioning
- Try `useDefaultAzureCredential: false` with explicit credentials

### Network/Connection Issues
- Ensure target service is accessible and running
- Verify ports are open for the target service
- Check that the RelayNamespace is correct

### Enable Verbose Logging

Run with `--verbose` flag for detailed debug output:

```bash
npm start -- --verbose
```

## 📚 Additional Resources

- [Main Project README](../../README.md)
- [.NET Version](../dotnet/RelayTunnelUsingHybridConnection/README.md)
- [Azure Relay Documentation](https://docs.microsoft.com/azure/azure-relay/)
- [hyco-ws GitHub](https://github.com/Azure/azure-relay-node)
- [hyco-https GitHub](https://github.com/Azure/azure-relay-node)

## 📄 License

MIT
