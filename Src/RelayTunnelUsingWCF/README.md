# RelayTunnelUsingWCF

**Version: 1.2.0**

This .NET Framework 4.8 console application provides **true WCF Relay functionality** where the relay endpoint appears in your Azure Relay namespace when running and disappears when stopped.

## Acknowledgments 
This project is part of a rewrite inspired by the original work that [Gabriel Gonzalez (gabog)](https://github.com/gabog) created in his project [AzureServiceBusBotRelay](https://github.com/gabog/AzureServiceBusBotRelay).

Part of this code is also based on the work that [Pedro Felix](https://github.com/pmhsfelix) did in his project [here](https://github.com/pmhsfelix/WebApi.Explorations.ServiceBusRelayHost).

## Features

✅ **Dynamic Relay Registration**: Relay appears in Azure when started, disappears when stopped  
✅ **HTTP Request Proxying**: Forwards all HTTP requests to your target service  
✅ **Configurable**: Relay name and target service configurable via appsettings.json  
✅ **Proper Lifecycle Management**: Graceful startup and shutdown  
✅ **Error Handling**: Comprehensive error handling and logging  

## Configuration

**Important**: After cloning, rename `appsettings-template.json` to `appsettings.json` (this file is in .gitignore to prevent accidental credential commits).

Update the `appsettings.json` file with your Azure Relay settings. The application supports multiple relay configurations in an array format:

```json
{
  "Relays": [
    {
      "RelayNamespace": "your-relay-namespace",
      "RelayName": "your-relay-name", 
      "PolicyName": "root-shared-access-key-name",
      "PolicyKey": "root-shared-access-key",
      "TargetServiceAddress": "http://localhost:3978/",
      "ServiceDiscoveryMode": "Private",
      "EnableDetailedLogging": true,
      "IsEnabled": true
    },
    {
      "RelayNamespace": "your-relay-namespace",
      "RelayName": "another-relay-name",
      "PolicyName": "root-shared-access-key-name",
      "PolicyKey": "root-shared-access-key", 
      "TargetServiceAddress": "http://localhost:3979/",
      "ServiceDiscoveryMode": "Public",
      "EnableDetailedLogging": false,
      "IsEnabled": false
    }
  ]
}
```

### Configuration Settings

- **RelayNamespace**: Your Azure Relay namespace (without `.servicebus.windows.net`)
- **RelayName**: The name of the WCF relay endpoint (configurable dynamically)
- **PolicyName**: The name of your SAS policy (usually `RootManageSharedAccessKey`)
- **PolicyKey**: Your SAS key from Azure portal
- **TargetServiceAddress**: The URL of your local service to proxy requests to
- **ServiceDiscoveryMode**: `Private` (default) or `Public`
- **EnableDetailedLogging**: Set to `true` for detailed request/response logging
- **IsEnabled**: Set to `true` to enable this relay configuration

**Note**: The application will start all relays where `IsEnabled` is set to `true`. You can run multiple relays simultaneously by configuring multiple entries in the array.

## How to Run

1. **Prerequisites**: .NET Framework 4.8 must be installed
2. **Clone and Setup**: Clone the repository and rename `appsettings-template.json` to `appsettings.json`
3. **Configure**: Update `appsettings.json` with your Azure Relay settings (supports multiple relays in the array)
4. **Build**: Restore NuGet packages and build the project
5. **Run**: Execute `RelayTunnelUsingWCF.exe`

```bash
# Build the project (or use Visual Studio)
msbuild RelayTunnelUsingWCF.csproj

# Run the executable
RelayTunnelUsingWCF.exe
```

### Azure Bot Configuration
Before testing, update your Azure Bot's messaging endpoint:
1. Login to the Azure portal and open your Azure Bot registration
2. Select **Settings** under Bot management
3. In **Messaging endpoint**, enter: `https://your-relay-namespace.servicebus.windows.net/your-relay-name/api/messages`
4. Click **Save**

## Usage Flow

1. **Start**: Run the console application
2. **Relay Registration**: The WCF relay endpoint appears in Azure Relay
3. **Request Proxying**: HTTP requests to the relay are forwarded to your target service
4. **Stop**: Press Ctrl+C or Enter to stop
5. **Cleanup**: The relay endpoint disappears from Azure Relay

## Example Output

```
Azure Relay WCF Utility (.NET Framework)
===============================================
Found 2 enabled relay configuration(s):

Starting relay: your-relay-name
  Namespace: your-relay-namespace
  Target: http://localhost:3978/
  Service Address: https://your-relay-namespace.servicebus.windows.net/your-relay-name/
  √ Relay 'your-relay-name' started successfully

Starting relay: another-relay-name
  Namespace: your-relay-namespace
  Target: http://localhost:8500/
  Service Address: https://your-relay-namespace.servicebus.windows.net/another-relay-name/
  √ Relay 'another-relay-name' started successfully


√ 2 WCF Relay service(s) are now RUNNING and visible in Azure!
√ The relay endpoints will appear in your Azure Relay namespace.
√ Requests to the relays will be forwarded to their target services.

Press Ctrl+C or Enter to stop all services...
```

## Integration with Other Projects

This WCF Relay Host is designed to work alongside your existing applications:

1. **Start your local service** on the configured target address (e.g., `http://localhost:3978`)
2. **Start this WCF Relay Host** to create the relay endpoint in Azure
3. **Access via Azure**: Requests to `https://your-relay-namespace.servicebus.windows.net/your-relay-name` will be proxied to your local service

**Note**: This project provides WCF Relay functionality, which is different from the Hybrid Connections approach used in the RelayTunnelUsingHybridConnection project. Both serve similar purposes but use different Azure Relay technologies.

## Security Notes

- Store your SAS key securely (consider using Azure Key Vault for production)
- The relay uses HTTPS for external access but HTTP for internal proxying
- Configure firewall rules appropriately for your target service

## Troubleshooting

**Common Issues:**
- **Configuration Errors**: Verify all appsettings.json settings are configured correctly
- **Multiple Relays**: Ensure each relay has unique RelayName values and `IsEnabled` is set appropriately
- **Network Issues**: Ensure target service is accessible
- **Authentication**: Check SAS key and policy permissions
- **Firewall**: Verify ports are open for the target service
- **Template File**: Make sure you've renamed `appsettings-template.json` to `appsettings.json`

**Logs**: The console displays detailed startup and request proxying information for each enabled relay
