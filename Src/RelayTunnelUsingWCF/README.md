# RelayTunnelUsingWCF

This .NET Framework 4.8 console application provides **true WCF Relay functionality** where the relay endpoint appears in your Azure Service Bus namespace when running and disappears when stopped.

## Features

✅ **Dynamic Relay Registration**: Relay appears in Azure when started, disappears when stopped  
✅ **HTTP Request Proxying**: Forwards all HTTP requests to your target service  
✅ **Configurable**: Relay name and target service configurable via App.config  
✅ **Proper Lifecycle Management**: Graceful startup and shutdown  
✅ **Error Handling**: Comprehensive error handling and logging  

## Configuration

Update the `App.config` file with your Azure Service Bus settings:

```xml
<appSettings>
  <add key="RelayNamespace" value="your-relay-namespace" />
  <add key="RelayName" value="your-relay-name" />
  <add key="PolicyName" value="RootManageSharedAccessKey" />
  <add key="PolicyKey" value="your-sas-key" />
  <add key="TargetServiceAddress" value="http://localhost:3978/" />
  <add key="ServiceDiscoveryMode" value="Private" />
</appSettings>
```

### Configuration Settings

- **RelayNamespace**: Your Azure Service Bus namespace (without `.servicebus.windows.net`)
- **RelayName**: The name of the WCF relay endpoint (configurable dynamically)
- **PolicyName**: The name of your SAS policy (usually `RootManageSharedAccessKey`)
- **PolicyKey**: Your SAS key from Azure portal
- **TargetServiceAddress**: The URL of your .NET 8 app to proxy requests to
- **ServiceDiscoveryMode**: `Private` (default) or `Public`

## How to Run

1. **Prerequisites**: .NET Framework 4.8 must be installed
2. **Build**: Restore NuGet packages and build the project
3. **Configure**: Update `App.config` with your Azure Service Bus settings
4. **Run**: Execute `RelayTunnelUsingWCF.exe`

```bash
# Build the project
msbuild RelayTunnelUsingWCF.csproj

# Run the executable
RelayTunnelUsingWCF.exe
```

## Usage Flow

1. **Start**: Run the console application
2. **Relay Registration**: The WCF relay endpoint appears in Azure Service Bus
3. **Request Proxying**: HTTP requests to the relay are forwarded to your target service
4. **Stop**: Press Ctrl+C or Enter to stop
5. **Cleanup**: The relay endpoint disappears from Azure Service Bus

## Example Output

```
Azure Service Bus WCF Relay Host
=================================
Relay Namespace: common-relay
Relay Name: wcf-connection
Target Service: http://localhost:3978/

Service Address: https://common-relay.servicebus.windows.net/wcf-connection
Opening WCF Relay service...

✓ WCF Relay service is now RUNNING and visible in Azure!
✓ The relay endpoint will appear in your Azure Service Bus namespace.
✓ Requests to the relay will be forwarded to your target service.

Press Ctrl+C or Enter to stop the service...
```

## Integration with .NET 8 App

This WCF Relay Host is designed to work alongside your existing .NET 8 RelayTunnelUsingHybridConnection project:

1. **Start your .NET 8 app** on the configured target address (e.g., `http://localhost:3978`)
2. **Start this WCF Relay Host** to create the relay endpoint in Azure
3. **Access via Azure**: Requests to `https://your-namespace.servicebus.windows.net/your-relay-name` will be proxied to your .NET 8 app

## Security Notes

- Store your SAS key securely (consider using Azure Key Vault for production)
- The relay uses HTTPS for external access but HTTP for internal proxying
- Configure firewall rules appropriately for your target service

## Troubleshooting

**Common Issues:**
- **Configuration Errors**: Verify all app settings are configured correctly
- **Network Issues**: Ensure target service is accessible
- **Authentication**: Check SAS key and policy permissions
- **Firewall**: Verify ports are open for the target service

**Logs**: The console displays detailed startup and request proxying information
