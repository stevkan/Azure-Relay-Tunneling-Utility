# RelayTunnelUsingHybridConnection

**Version: 1.6.3**

This .NET 8 console application provides **Azure Hybrid Connection** functionality with optional **dynamic resource management** using ARM templates. Hybrid connections can be created/deleted automatically when the application starts/stops.

## 🎯 Overview

An HTTP tunneling utility that forwards HTTP traffic from Azure to your local machine. This is useful for:
- Exposing local web servers, APIs, or HTTP services through Azure endpoints
- Debugging bots and agents locally while receiving real traffic from Azure-hosted channels
- Testing with real ChannelData from channels like WebChat, Teams, Skype
- Development and testing without deploying to Azure

## ✨ Features

✅ **Hybrid Connection Support**: Uses Azure Relay Hybrid Connections (modern approach)  
✅ **Dynamic Resource Creation**: Optional ARM template automation - resources appear/disappear automatically  
✅ **HTTP Request Proxying**: Forwards all HTTP requests to your target service  
✅ **Multi-Relay Support**: Configure multiple relay endpoints in a single application  
✅ **Flexible Authentication**: Azure CLI, Service Principal, or Managed Identity  
✅ **Comprehensive Configuration**: Full JSON-based configuration system  

## 📋 Prerequisites

- **Cross-platform support:** Windows, Linux, macOS (x64)
- .NET 8 Runtime must be installed
- Visual Studio or .NET CLI (for building)
- Azure Relay namespace with SAS policy credentials
- Local bot/agent service to proxy requests to
- (Optional) Azure CLI or Service Principal for dynamic resource creation

## ⚙️ Quick Start Configuration

**Step 1:** Rename `appsettings-template.json` to `appsettings.json`

**Step 2:** Choose your mode and configure:

### Static Resources (Simpler - Good for Getting Started)

Pre-create hybrid connection in Azure Portal, then:

```json
{
  "Relays": [
    {
      "RelayNamespace": "your-namespace",
      "RelayName": "your-relay-name",
      "PolicyName": "RootManageSharedAccessKey",
      "PolicyKey": "your-key",
      "TargetServiceAddress": "http://localhost:3978",
      "IsEnabled": true,
      "DynamicResourceCreation": false
    }
  ]
}
```

### Dynamic Resources (Advanced - Auto Create/Delete)

Requires Azure authentication:

```json
{
  "AzureManagement": {
    "SubscriptionId": "your-subscription-id",
    "UseDefaultAzureCredential": true
  },
  "Relays": [
    {
      "RelayNamespace": "your-namespace",
      "RelayName": "your-relay-name",
      "PolicyName": "RootManageSharedAccessKey",
      "PolicyKey": "your-key",
      "TargetServiceAddress": "http://localhost:3978",
      "IsEnabled": true,
      "DynamicResourceCreation": true,
      "ResourceGroupName": "your-resource-group"
    }
  ]
}
```

**Note:** Azure requires lowercase relay names. Uppercase letters are auto-converted.

**📚 Full configuration reference:** See [Configuration Properties](#configuration-properties) below

## 🔐 Authentication (for Dynamic Resource Creation)

If you're using `DynamicResourceCreation: true`, you need Azure permissions. Three authentication methods are available:

**Quick Setup:**
1. **Azure CLI** (Development) - Run `az login`, set `UseDefaultAzureCredential: true`
2. **Service Principal** (Production) - Set `UseDefaultAzureCredential: false`, provide `ClientId` and `ClientSecret`
3. **Managed Identity** (Azure VMs/App Service) - Set `UseDefaultAzureCredential: true`

**Required Permissions:**
- **Contributor** role on Resource Group, or
- **Relay Namespace Contributor** role on the namespace

📚 **For detailed authentication setup, see:** [Authentication Guide](../../docs/AUTHENTICATION.md)

## 🚀 How to Run

### Build and Run

```bash
# Build the project (or use Visual Studio)
dotnet build

# Run the application  
dotnet run
# OR
RelayTunnelUsingHybridConnection.exe
```

Or in Visual Studio, press **F5** to run the project.

### Configure Azure Bot Messaging Endpoint

Before testing the relay, your Azure Bot's messaging endpoint must be updated:

1. Login to the Azure portal and open your Azure Bot's registration
2. Select **Settings** under Bot management to open the settings blade
3. In the **Messaging endpoint** field, enter the service bus namespace and relay
4. Append **"/api/messages"** to the end. For example:
   ```
   https://[your-relay-namespace].servicebus.windows.net/[your-relay-name]/api/messages
   ```
5. Click **Save**

### Test Your Bot

1. Open and run your locally hosted bot/agent on the configured target address (e.g., `http://localhost:3978`)
2. Test your bot/agent on a channel (Test in Web Chat, Skype, Teams, etc.)
3. User data is captured and logged as activity occurs

### Stop the Application

Press **Ctrl+C** or **Enter** to stop. If using dynamic resource creation, the hybrid connection resources will be automatically deleted from Azure.

## 📤 Publishing Executable

### Windows Users
Download the latest pre-built binaries from the **[Releases Page](../../../releases)**.

### Building from Source (Linux/macOS)

To build and run on Linux or macOS:

1. **Install .NET 8 SDK:**
   - [Download .NET 8](https://dotnet.microsoft.com/download/dotnet/8.0) for your OS.

2. **Clone the Repository:**
   ```bash
   git clone https://github.com/stevkan/AzureRelayTunnelingUtility.git
   cd AzureRelayTunnelingUtility
   ```

3. **Build and Publish:**
   ```bash
   # Linux
   dotnet publish Src/dotnet/RelayTunnelUsingHybridConnection/RelayTunnelUsingHybridConnection.csproj --configuration Release --runtime linux-x64 --self-contained false --output ./bin/linux-x64

   # macOS
   dotnet publish Src/dotnet/RelayTunnelUsingHybridConnection/RelayTunnelUsingHybridConnection.csproj --configuration Release --runtime osx-x64 --self-contained false --output ./bin/osx-x64
   ```

4. **Run the Utility:**
   ```bash
   # Linux
   ./bin/linux-x64/RelayTunnelUsingHybridConnection

   # macOS
   ./bin/osx-x64/RelayTunnelUsingHybridConnection
   ```

### Publishing Self-Contained Executable

**Using .NET CLI:**
```bash
# Windows
dotnet publish -c Release -r win-x64 --self-contained
```

Output location: `/bin/Release/net8.0/{runtime}` folder with all necessary files. The `appsettings.json` can be edited without recompiling.

## 📝 Example Output

```
Azure Relay Hybrid Connection Utility (.NET Core)
============================================

Initializing Azure Resource Manager...
√ Azure Resource Manager initialized
Found 1 enabled relay configuration(s):
  - my-relay --> http://localhost:3978 (Dynamic: True)

Creating Hybrid Connection 'my-relay' in namespace 'my-namespace'...
√ Hybrid Connection 'my-relay' created successfully

Azure Relay is listening on
        sb://[your-relay-namespace].servicebus.windows.net/[your-relay-name]
and routing requests to
        http://localhost:3978/

Press [Enter] to exit
```

## 🔄 Usage Flow

1. **Start**: Run the console application
2. **Resource Creation** (if dynamic): Hybrid connection resources are created in Azure
3. **Connection Registration**: The hybrid connection endpoints become available
4. **Request Proxying**: HTTP requests to the relay are forwarded to your target service
5. **Stop**: Press Ctrl+C or Enter to stop
6. **Cleanup** (if dynamic): The hybrid connection resources are automatically deleted from Azure

## 🆚 Dynamic vs Static Resources

**Dynamic** (`DynamicResourceCreation: true`)
- ✅ Best for development and testing
- ✅ Resources auto-appear when app starts
- ✅ Resources auto-disappear when app stops
- ✅ Clean environment - no leftover resources
- ⚠️ Requires Azure authentication

**Static** (`DynamicResourceCreation: false`)
- ✅ Best for production environments
- ✅ Persistent, manually managed connections
- ✅ No Azure management permissions needed
- ✅ Simpler configuration

## 🔗 Integration with Other Projects

This Hybrid Connection Host can work alongside your existing applications:

1. **Start your local service** on the configured target address (e.g., `http://localhost:3978`)
2. **Start this Hybrid Connection Host** to create the connection in Azure  
3. **Access via Azure**: Requests to `https://[your-relay-namespace].servicebus.windows.net/[your-relay-name]` will be proxied to your local service

**Note**: This project uses Azure Relay Hybrid Connections (modern), which is different from the WCF Relay approach used in the RelayTunnelUsingWCF project. Both serve similar purposes but use different Azure technologies.

## ⚠️ Known Limitations

### Simultaneous HTTP and WebSocket Tunneling

Using a single hybrid connection for both HTTP proxying (e.g., serving web pages from a local web server) and WebSocket tunneling simultaneously is **not supported in all scenarios**. The success of this configuration depends on your specific use case:

**Supported Scenarios:**
- HTTP-only traffic (web pages, REST APIs, static files)
- WebSocket-only traffic (bot Direct Line connections, real-time communication)
- Sequential use (switching between HTTP and WebSocket at different times)

**Unsupported Scenarios:**
- A single relay simultaneously handling both HTTP requests for a web application AND HTTP/WebSocket connections for the same or different service

**Recommendation:** If you need both HTTP and WebSocket support, create separate hybrid connections (i.e., separate relay namespaces) with distinct relay names — one configured for HTTP traffic and another for WebSocket traffic.

## 🐛 Troubleshooting

### Quick Solutions

**Subscription Mismatch (Most Common)**
```bash
az account show  # Check current subscription
az account set --subscription "your-subscription-id"  # Set correct one
```

**Authentication Errors**
- Run `az login` for Azure CLI authentication
- Verify permissions on resource group
- Check `SubscriptionId` in appsettings.json

**Configuration Issues**
- Rename `appsettings-template.json` to `appsettings.json`
- Validate JSON syntax
- Ensure relay names are lowercase

**Resource Creation Failures**
- Verify `SubscriptionId` and `ResourceGroupName` are correct
- Ensure relay namespace exists (Azure Relay, not Service Bus)
- Check you have Contributor or Relay Namespace Contributor role

📚 **For comprehensive troubleshooting, see:** [Troubleshooting Guide](../../docs/TROUBLESHOOTING.md)

## 📋 Configuration Properties

### AzureManagement Section (Only required for dynamic resource creation)

| Property | Type | Required | Description |
|----------|------|----------|-------------|
| `SubscriptionId` | string | Yes* | Your Azure subscription ID. Required if any relay uses `DynamicResourceCreation: true` |
| `TenantId` | string | No | Azure tenant ID. Optional when using DefaultAzureCredential |
| `UseDefaultAzureCredential` | boolean | No | If `true` (default), uses Azure CLI, Managed Identity, or Visual Studio credentials. If `false`, uses ClientId/ClientSecret |
| `ClientId` | string | No* | Service Principal client ID. Required if `UseDefaultAzureCredential: false` |
| `ClientSecret` | string | No* | Service Principal client secret. Required if `UseDefaultAzureCredential: false` |

### Relays Section

| Property | Type | Required | Description |
|----------|------|----------|-------------|
| `RelayNamespace` | string | Yes | Relay namespace name (e.g., "namespace") - ".servicebus.windows.net" is appended automatically |
| `RelayName` | string | Yes | Name of the hybrid connection (automatically converted to lowercase) |
| `PolicyName` | string | Yes | Name of the shared access policy |
| `PolicyKey` | string | Yes | Key for the shared access policy |
| `TargetServiceAddress` | string | Yes | Local service URL to proxy requests to (e.g., "http://localhost:3978") |
| `IsEnabled` | boolean | Yes | Whether this relay configuration is active |
| `DynamicResourceCreation` | boolean | No | If `true`, automatically creates/deletes the hybrid connection resource. Default: `false` |
| `ResourceGroupName` | string | Yes* | Azure resource group name. Required if `DynamicResourceCreation: true` |
| `Description` | string | No | Description for the dynamically created resource. Default: "Dynamically created hybrid connection" |
| `RequiresClientAuthorization` | boolean | No | Whether clients need authorization to connect. Default: `true` |

## 📚 Additional Resources

### Project Documentation
- **[Main Project README](../../README.md)** - Overview and project comparison
- **[WCF Relay Alternative](../RelayTunnelUsingWCF/README.md)** - Legacy .NET Framework option
- **[ARM Automation Details](README_ARM_AUTOMATION.md)** - Technical implementation deep-dive

### Guides
- **[Authentication Guide](../../docs/AUTHENTICATION.md)** - Detailed Azure authentication setup for all methods
- **[Troubleshooting Guide](../../docs/TROUBLESHOOTING.md)** - Comprehensive solutions for common issues
- **[Technical Comparison](../../docs/COMPARISON.md)** - WCF vs Hybrid Connection comparison

### External Links
- [Azure Relay Documentation](https://docs.microsoft.com/azure/azure-relay/)
