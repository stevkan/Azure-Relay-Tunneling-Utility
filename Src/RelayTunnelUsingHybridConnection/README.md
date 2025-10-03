# RelayTunnelUsingHybridConnection

**Version: 1.3.5**

This .NET 8 console application provides **Azure Hybrid Connection** functionality with optional **dynamic resource management** using ARM templates. Hybrid connections can be created/deleted automatically when the application starts/stops.

## Acknowledgments 
This project is part of a rewrite inspired by the original work that [Gabriel Gonzalez (gabog)](https://github.com/gabog) created in his project [AzureServiceBusBotRelay](https://github.com/gabog/AzureServiceBusBotRelay).

Part of this code is also based on the work that [Pedro Felix](https://github.com/pmhsfelix) did in his project [here](https://github.com/pmhsfelix/WebApi.Explorations.ServiceBusRelayHost).

## Features

✅ **Hybrid Connection Support**: Uses Azure Relay Hybrid Connections (modern approach)  
✅ **Dynamic Resource Creation**: Optional ARM template automation - resources appear/disappear automatically  
✅ **HTTP Request Proxying**: Forwards all HTTP requests to your target service  
✅ **Multi-Relay Support**: Configure multiple relay endpoints in a single application  
✅ **Flexible Authentication**: Azure CLI, Service Principal, or Managed Identity  
✅ **Comprehensive Configuration**: Full JSON-based configuration system  

## Configuration

**Important**: After cloning, rename `appsettings-template.json` to `appsettings.json` (this file is in .gitignore to prevent accidental credential commits).

Update the `appsettings.json` file with your Azure Relay settings. The application supports multiple relay configurations in an array format:

### Basic Configuration

```json
{
  "Relays": [
    {
      "RelayNamespace": "your-relay-namespace",
      "RelayName": "your-relay-name",
      "PolicyName": "root-shared-access-key-name",
      "PolicyKey": "root-shared-access-key",
      "TargetServiceAddress": "http://localhost:3978",
      "IsEnabled": true,
      "DynamicResourceCreation": false
    },
    {
      "RelayNamespace": "your-relay-namespace",
      "RelayName": "another-relay-name",
      "PolicyName": "root-shared-access-key-name",
      "PolicyKey": "root-shared-access-key",
      "TargetServiceAddress": "http://localhost:3978",
      "IsEnabled": true,
      "DynamicResourceCreation": false
    }
  ]
}
```

**Note:** Azure Relay requires lowercase names. If you specify uppercase letters in `RelayName`, they will be automatically converted to lowercase with a warning displayed at startup.

### Dynamic Resource Creation (Optional)

For automatic hybrid connection lifecycle management, add the AzureManagement section:

```json
{
  "AzureManagement": {
    "SubscriptionId": "your-azure-subscription-id",
    "UseDefaultAzureCredential": true
  },
  "Relays": [
    {
      "RelayNamespace": "your-relay-namespace",
      "RelayName": "your-relay-name",
      "PolicyName": "root-shared-access-key-name", 
      "PolicyKey": "root-shared-access-key",
      "TargetServiceAddress": "http://localhost:3978",
      "IsEnabled": true,
      "DynamicResourceCreation": true,
      "ResourceGroupName": "your-resource-group-name"
    },
    {
      "RelayNamespace": "your-relay-namespace",
      "RelayName": "another-relay-name",
      "PolicyName": "root-shared-access-key-name", 
      "PolicyKey": "root-shared-access-key",
      "TargetServiceAddress": "http://localhost:3978",
      "IsEnabled": true,
      "DynamicResourceCreation": true,
      "ResourceGroupName": "your-resource-group-name"
    }
  ]
}
```

## Authentication Options (for Dynamic Resource Creation)

**Option 1: Azure CLI (Recommended for Development)**
- Run `az login` in your terminal
- Set `UseDefaultAzureCredential: true` (default)
- Leave `ClientId` and `ClientSecret` empty
- The app will automatically use your Azure CLI credentials

**Option 2: Service Principal (Recommended for Production)** 
- Create a Service Principal with Contributor access to your resource group
- Set `UseDefaultAzureCredential: false`
- Provide your `ClientId` and `ClientSecret` in the configuration
- Optionally specify `TenantId` for additional security

**Option 3: Managed Identity (For Azure VMs/App Service)**
- Assign a Managed Identity to your VM/App Service in Azure
- Grant the identity Contributor access to your resource group
- Set `UseDefaultAzureCredential: true` (default)
- Leave `ClientId` and `ClientSecret` empty

**Required Azure Permissions:**
The authenticated identity needs one of:
- **Contributor** role on the Resource Group containing your relay namespace
- **Relay Namespace Contributor** role on the specific namespace

## How to Run

1. **Prerequisites**: .NET 8 Runtime must be installed
2. **Clone and Setup**: Clone the repository and rename `appsettings-template.json` to `appsettings.json`
3. **Configure**: Update `appsettings.json` with your settings
4. **Authentication** (if using dynamic creation): Ensure Azure authentication is configured
5. **Build**: Restore packages and build the project
6. **Run**: Execute `RelayTunnelUsingHybridConnection.exe`

```bash
# Build the project (or use Visual Studio)
dotnet build

# Run the application  
dotnet run
# OR
RelayTunnelUsingHybridConnection.exe
```

### Azure Bot Configuration
Before testing, update your Azure Bot's messaging endpoint:
1. Login to the Azure portal and open your Azure Bot registration
2. Select **Settings** under Bot management  
3. In **Messaging endpoint**, enter: `https://[your-relay-namespace].servicebus.windows.net/your-relay-name/api/messages`
4. Click **Save**

## Usage Flow

1. **Start**: Run the console application
2. **Resource Creation** (if dynamic): Hybrid connection resources are created in Azure
3. **Connection Registration**: The hybrid connection endpoints become available
4. **Request Proxying**: HTTP requests to the relay are forwarded to your target service
5. **Stop**: Press Ctrl+C or Enter to stop
6. **Cleanup** (if dynamic): The hybrid connection resources are automatically deleted from Azure

## Example Output

```
Azure Relay Hybrid Connection Utility (.NET Core)
============================================

Initializing Azure Resource Manager for dynamic resource management...
√ Azure Resource Manager initialized
Found 2 enabled relay configuration(s):
  - your-relay-name --> http://localhost:3978 (Dynamic: True)
  - another-relay-name --> http://localhost:8500 (Dynamic: True)

Dynamic resource creation enabled for 'your-relay-name'
Dynamic resource creation enabled for 'another-relay-name'
Creating Hybrid Connection 'your-relay-name' in namespace 'common-relay'...
Creating Hybrid Connection 'another-relay-name' in namespace 'common-relay'...
√ Hybrid Connection 'your-relay-name' created successfully
√ Hybrid Connection 'another-relay-name' created successfully
Azure Relay is listening on
        sb://your-relay-namespace.servicebus.windows.net/your-relay-name
and routing requests to
        http://localhost:3978/


Press [Enter] to exit
Azure Relay is listening on
        sb://your-relay-namespace.servicebus.windows.net/another-relay-name
and routing requests to
        http://localhost:8500/


Press [Enter] to exit
```

## Dynamic vs Static Resources

- **Dynamic** (`DynamicResourceCreation: true`): Best for development, testing, and temporary deployments. Resources automatically appear when app starts and disappear when it stops.
- **Static** (`DynamicResourceCreation: false`): Best for production environments where you want persistent, manually managed hybrid connections.

## Integration with Other Projects

This Hybrid Connection Host can work alongside your existing applications:

1. **Start your local service** on the configured target address (e.g., `http://localhost:3978`)
2. **Start this Hybrid Connection Host** to create the connection in Azure  
3. **Access via Azure**: Requests to `https://[your-relay-namespace].servicebus.windows.net/your-relay-name` will be proxied to your local service

**Note**: This project uses Azure Relay Hybrid Connections (modern), which is different from the WCF Relay approach used in the RelayTunnelUsingWCF project. Both serve similar purposes but use different Azure technologies.

## Troubleshooting

**Authentication Errors**
- Ensure you're logged into Azure CLI (`az login`) or have valid service principal credentials
- Verify the authenticated identity has sufficient permissions on the resource group

**Subscription Mismatch (Common Issue)**
- Error: "The Resource 'Microsoft.Relay/namespaces/...' was not found"
- **Fix**: Ensure your Azure CLI is using the correct subscription:
  ```bash
  az account show  # Check current subscription
  az account set --subscription "your-subscription-id"  # Set correct subscription
  ```
- Verify the subscription ID in appsettings.json matches your Azure CLI subscription

**Resource Creation Failures**
- Check that the `SubscriptionId` and `ResourceGroupName` are correct in appsettings.json
- Ensure the relay namespace exists in the specified resource group (must be an Azure Relay namespace, not Service Bus)
- Verify network connectivity to Azure ARM endpoints

**Configuration Errors**
- Validate JSON syntax in appsettings.json
- Ensure all required fields are populated for relays with `DynamicResourceCreation: true`
- Check that relay names are unique within the namespace

See the [ARM Automation README](README_ARM_AUTOMATION.md) for detailed implementation information about the dynamic resource creation feature.