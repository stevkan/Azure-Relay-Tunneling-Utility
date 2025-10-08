# RelayTunnelUsingWCF

**Version: 1.4.0**

This .NET Framework 4.8 console application provides **WCF Relay functionality** where the relay endpoint appears in your Azure Relay namespace when running and disappears when stopped.

## 🎯 Overview

An HTTP tunneling utility that forwards HTTP traffic from Azure to your local machine. This is useful for:
- Exposing local web servers, APIs, or HTTP services through Azure endpoints
- Debugging bots and agents locally while receiving real traffic from Azure-hosted channels
- Testing with real ChannelData from channels like WebChat, Teams, Skype
- Development and testing without deploying to Azure

## ✨ Features

✅ **Dynamic Relay Registration**: Relay appears in Azure when started, disappears when stopped  
✅ **HTTP Request Proxying**: Forwards all HTTP requests to your target service  
✅ **Multi-Relay Support**: Configure and run multiple relays simultaneously  
✅ **Configurable**: Relay name and target service configurable via appsettings.json  
✅ **Proper Lifecycle Management**: Graceful startup and shutdown  
✅ **Error Handling**: Comprehensive error handling and logging  

## ⚠️ Security Warning

**This project uses deprecated WCF Relay libraries that are no longer receiving security updates from Microsoft.**

- ❌ No ongoing security patches or updates
- ❌ Potential unpatched vulnerabilities
- ⚠️ **Not recommended for production use**
- ✅ **For production deployments, use [RelayTunnelUsingHybridConnection](../RelayTunnelUsingHybridConnection/README.md) instead**

This implementation is suitable for:
- Development and testing environments
- Legacy systems requiring WCF Relay compatibility
- Scenarios where you understand and accept the security risks

## 📋 Prerequisites

- .NET Framework 4.8 must be installed
- Visual Studio (for building)
- Azure Relay namespace with SAS policy credentials
- Local bot/agent service to proxy requests to

## ⚙️ Configuration

### Step 1: Setup Configuration File

**Important**: After cloning, rename `appsettings-template.json` to `appsettings.json` (this file is in .gitignore to prevent accidental credential commits).

### Step 2: Configure Your Relays

Update the `appsettings.json` file with your Azure Relay settings. The application supports multiple relay configurations in an array format:

```json
{
  "Relays": [
    {
      "RelayNamespace": "your-relay-namespace",
      "RelayName": "your-relay-name", 
      "PolicyName": "RootManageSharedAccessKey",
      "PolicyKey": "your-root-shared-access-key",
      "TargetServiceAddress": "http://localhost:3978/",
      "ServiceDiscoveryMode": "Private",
      "EnableDetailedLogging": true,
      "IsEnabled": true
    },
    {
      "RelayNamespace": "your-relay-namespace",
      "RelayName": "another-relay-name",
      "PolicyName": "RootManageSharedAccessKey",
      "PolicyKey": "your-root-shared-access-key", 
      "TargetServiceAddress": "http://localhost:3979/",
      "ServiceDiscoveryMode": "Public",
      "EnableDetailedLogging": false,
      "IsEnabled": false
    }
  ]
}
```

### Configuration Properties

| Property | Type | Required | Description |
|----------|------|----------|-------------|
| `RelayNamespace` | string | Yes | Relay namespace name only (e.g., "common-relay", not the full URL) |
| `RelayName` | string | Yes | Name of the WCF relay endpoint |
| `PolicyName` | string | Yes | Name of the shared access policy (usually "RootManageSharedAccessKey") |
| `PolicyKey` | string | Yes | Key for the shared access policy |
| `TargetServiceAddress` | string | Yes | Local service URL to proxy requests to (e.g., "http://localhost:3978/") |
| `ServiceDiscoveryMode` | string | No | Relay visibility: "Private" (default) or "Public" |
| `EnableDetailedLogging` | boolean | No | Whether to enable detailed request/response logging. Default: `true` |
| `IsEnabled` | boolean | Yes | Whether this relay configuration is active |

**Note**: The application will start all relays where `IsEnabled` is set to `true`. You can run multiple relays simultaneously by configuring multiple entries in the array.

## 🚀 How to Run

### Build and Run

1. **Clone and Setup**: Clone the repository and rename `appsettings-template.json` to `appsettings.json`
2. **Configure**: Update `appsettings.json` with your Azure Relay settings
3. **Build**: Restore NuGet packages and build the project

```bash
# Build the project (or use Visual Studio)
msbuild RelayTunnelUsingWCF.csproj

# Run the executable
RelayTunnelUsingWCF.exe
```

Or in Visual Studio, press **F5** to run the project.

### Configure Azure Bot Messaging Endpoint

Before testing the relay, your Azure Bot's messaging endpoint must be updated:

1. Login to the Azure portal and open your Azure Bot's registration
2. Select **Settings** under Bot management to open the settings blade
3. In the **Messaging endpoint** field, enter the service bus namespace and relay. The relay should match the relay name entered in the **appsettings.json** file.
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

Press **Ctrl+C** or **Enter** to stop all services. The relay endpoints will automatically disappear from Azure Relay.

## 📤 Building Executable

Once testing is completed, you can compile the project into an executable:

1. Right click the project folder in Visual Studio and select **Build**
2. The .exe will output to the **/bin/debug** folder, along with other necessary files, located in the project's directory folder
3. All the files are necessary to run and should be included when moving the .exe to a new folder/location
4. The **appsettings.json** is in the same folder and can be edited as credentials change without needing to recompile the project

## 📝 Example Output

```
Azure Relay WCF Utility (.NET Framework)
===============================================
Found 2 enabled relay configuration(s):

Starting relay: your-relay-name
  Namespace: your-relay-namespace
  Target: http://localhost:3978/
  Service Address: https://[your-relay-namespace].servicebus.windows.net/[your-relay-name]/
  √ Relay 'your-relay-name' started successfully

Starting relay: another-relay-name
  Namespace: your-relay-namespace
  Target: http://localhost:8500/
  Service Address: https://[your-relay-namespace].servicebus.windows.net/[another-relay-name]/
  √ Relay 'another-relay-name' started successfully


√ 2 WCF Relay service(s) are now RUNNING and visible in Azure!
√ The relay endpoints will appear in your Azure Relay namespace.
√ Requests to the relays will be forwarded to their target services.

Press Ctrl+C or Enter to stop all services...
```

## 🔄 Usage Flow

1. **Start**: Run the console application
2. **Relay Registration**: The WCF relay endpoint appears in Azure Relay
3. **Request Proxying**: HTTP requests to the relay are forwarded to your target service
4. **Stop**: Press Ctrl+C or Enter to stop
5. **Cleanup**: The relay endpoint disappears from Azure Relay

## 🔗 Integration with Other Projects

This WCF Relay Host is designed to work alongside your existing applications:

1. **Start your local service** on the configured target address (e.g., `http://localhost:3978`)
2. **Start this WCF Relay Host** to create the relay endpoint in Azure
3. **Access via Azure**: Requests to `https://[your-relay-namespace].servicebus.windows.net/[your-relay-name]/api/messages` will be proxied to your local service

**Note**: This project provides WCF Relay functionality, which is different from the Hybrid Connections approach used in the RelayTunnelUsingHybridConnection project. Both serve similar purposes but use different Azure Relay technologies.

## 🔐 Security Notes

- ⚠️ **CRITICAL:** This project uses deprecated libraries with no security updates - not recommended for production
- Store your SAS key securely (consider using Azure Key Vault)
- The relay uses HTTPS for external access but HTTP for internal proxying
- Configure firewall rules appropriately for your target service
- The `appsettings.json` file is in .gitignore to prevent credential leaks
- **For production use, migrate to [RelayTunnelUsingHybridConnection](../RelayTunnelUsingHybridConnection/README.md)**

📚 **See:** [Technical Comparison Guide](../../docs/COMPARISON.md) for detailed security analysis and migration guidance

## 🐛 Troubleshooting

### Quick Solutions

**Configuration Errors**
- Verify all appsettings.json settings are configured correctly
- Make sure you've renamed `appsettings-template.json` to `appsettings.json`

**Authentication**
- Check SAS key and policy permissions
- Ensure the policy name matches what's in Azure (usually "RootManageSharedAccessKey")

**Network/Connection Issues**
- Ensure target service is accessible and running
- Verify ports are open for the target service
- Check that the RelayNamespace is correct

### Detailed Logging

Set `EnableDetailedLogging: true` in your relay configuration for verbose output.

📚 **For comprehensive troubleshooting, see:** [Troubleshooting Guide](../../docs/TROUBLESHOOTING.md)

## 📚 Additional Resources

### Project Documentation
- [Main Project README](../../README.md)
- [Hybrid Connection Alternative](../RelayTunnelUsingHybridConnection/README.md) - Modern, secure alternative

### Guides
- [Technical Comparison](../../docs/COMPARISON.md) - WCF vs Hybrid Connection, migration guide
- [Troubleshooting Guide](../../docs/TROUBLESHOOTING.md) - Comprehensive solutions for common issues

### External Links
- [Azure Relay Documentation](https://docs.microsoft.com/azure/azure-relay/)
