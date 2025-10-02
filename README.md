# Overview
A relay utility for bots and agents based on Azure Service Bus.  

This utility allows you to forward a message sent to a bot or agent hosted on any channel to your local machine.

It is useful for debug scenarios or for more complex situations where you need to receive ChannelData in your requests from channels like WebChat hosted on websites.

**Version: 1.3.1**

## Acknowledgments 
This project is a rewrite inspired by the original work that [Gabriel Gonzalez (gabog)](https://github.com/gabog) created in his project [AzureServiceBusBotRelay](https://github.com/gabog/AzureServiceBusBotRelay).

Part of this code is also based on the work that [Pedro Felix](https://github.com/pmhsfelix) did in his project [here](https://github.com/pmhsfelix/WebApi.Explorations.ServiceBusRelayHost).

# How to configure and run the utility
### Building with .Net Framework (RelayTunnelUsingWCF)

1. Once the solution has been cloned to your machine, open the solution in Visual Studio.

2. In Solution Explorer, expand the **RelayTunnelUsingWCF** folder (located in the RelayTunnelUsingWCF directory).

3. Rename **appsettings-template.json** to **appsettings.json** (this file is in .gitignore to prevent accidental credential commits).

4. Open the **appsettings.json** file and configure the relay settings:
   
    a. **"RelayNamespace"** - The name of your Azure Relay namespace (e.g., "common-relay").
    
    b. **"RelayName"** - The name of the WCF relay endpoint to be created dynamically.
    
    c. **"PolicyName"** - The name of the shared access policy (usually "RootManageSharedAccessKey").
   
    d. **"PolicyKey"** - The key for the shared access policy from your Azure Relay namespace.
   
    e. **"TargetServiceAddress"** - The localhost address and port for your bot/agent (e.g., "http://localhost:3978/").
    
    f. **"ServiceDiscoveryMode"** - Set to "Private" or "Public" for relay visibility.
    
    g. **"EnableDetailedLogging"** - Set to `true` for detailed request/response logging.
    
    h. **"IsEnabled"** - Set to `true` to enable this relay configuration.
   
5. Before testing the relay, your Azure Bot's messaging endpoint must be updated to match the relay.
   
    a. Login to the Azure portal and open your Azure Bot's registration.
    
    b. Select **Settings** under Bot management to open the settings blade.
    
    c. In the **Messaging endpoint** field, enter the service bus namespace and relay. The relay should match the relay name entered in the **appsettings.json** file.
    
    d. Append **"/api/messages"** to the end to create the full endpoint to be used. For example, "https://example-relay.servicebus.windows.net/wcf-example-relay/api/messages".
    
    e. Click **Save** when completed.
   
6. In Visual Studio, press **F5** to run the project.
   
7. Open and run your locally hosted bot/agent.
   
8. Test your bot/agent on a channel (Test in Web Chat, Skype, Teams, etc.). User data is captured and logged as activity occurs.

9. Once testing is completed, you can compile the project into an executable.

    a. Right click the project folder in Visual Studio and select **Build**.

    b. The .exe will output to the **/bin/debug** folder, along with other necessary files, located in the project's directory folder. All the files are necessary to run and should be included when moving the .exe to a new folder/location.
    - The **appsettings.json** is in the same folder and can be edited as credentials change without needing to recompile the project.

### Building with .Net Core (RelayTunnelUsingHybridConnection)

1. Once the solution has been cloned to your machine, open the solution in Visual Studio.

2. In Solution Explorer, expand the **RelayTunnelUsingHybridConnection** folder.

3. Rename **appsettings-template.json** to **appsettings.json** (this file is in .gitignore to prevent accidental credential commits).

4. Open the **appsettings.json** file and configure the following sections:

#### Azure Management Configuration (for Dynamic Resource Creation)

The AzureManagement section is only required if you plan to use dynamic resource creation (`DynamicResourceCreation: true`). This feature automatically creates and deletes hybrid connection resources in Azure when your application starts and stops.

```json
"AzureManagement": {
  "SubscriptionId": "your-azure-subscription-id",
  "TenantId": "your-azure-tenant-id", 
  "UseDefaultAzureCredential": true,
  "ClientId": "",
  "ClientSecret": ""
}
```

**Authentication Setup Options:**

For dynamic resource creation, you need Azure permissions. Choose one of the following authentication methods:

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

#### Relay Configuration
For each relay in the "Relays" array, configure:

a. **"RelayNamespace"** - The name of your Azure Relay namespace (e.g., "your-namespace.servicebus.windows.net").

b. **"RelayName"** - The name of the hybrid connection.

c. **"PolicyName"** - The name of the shared access policy.

d. **"PolicyKey"** - The key for the shared access policy.

e. **"TargetServiceAddress"** - The localhost address and port for your bot/agent (e.g., "http://localhost:3978").

f. **"IsEnabled"** - Set to `true` to enable this relay configuration.

g. **"DynamicResourceCreation"** - Set to `true` to automatically create/delete the hybrid connection resource in Azure when the application starts/stops.

h. **"ResourceGroupName"** - Required if DynamicResourceCreation is true. The Azure resource group containing your relay namespace.

#### Example Configuration
```json
{
  "AzureManagement": {
    "SubscriptionId": "c4dfdf71-1cca-4bb4-8b21-2d24819f71f5",
    "UseDefaultAzureCredential": true
  },
  "Relays": [
    {
      "RelayNamespace": "[your-relay-namespace].servicebus.windows.net",
      "RelayName": "[your-relay-name]", 
      "PolicyName": "[your-policy-name]",
      "PolicyKey": "[your-policy-key]",
      "TargetServiceAddress": "http://localhost:3978",
      "IsEnabled": true,
      "DynamicResourceCreation": false,
      "ResourceGroupName": "your-resource-group-name"
    }
  ]
}
```

## 🚀 New Feature: Dynamic Resource Creation

The .NET Core version supports **ARM Template Automation** for dynamic hybrid connection lifecycle management. This feature is configured in the `AzureManagement` section above.

### Benefits
- **Automatic Resource Management**: Hybrid connections appear in Azure when your app starts and disappear when it stops
- **No Manual Setup**: No need to pre-create hybrid connections in Azure portal  
- **Multi-Configuration Support**: Mix static and dynamic relays in the same application
- **Secure Authentication**: Uses Azure Default Credential or Service Principal authentication
- **Clean Environment**: Resources are automatically cleaned up when development/testing is complete

### How It Works
1. **App Start**: If `DynamicResourceCreation: true`, the hybrid connection resource is automatically created in Azure using the ARM template
2. **App Running**: Your bot/agent receives messages through the dynamically created relay endpoint
3. **App Stop**: The hybrid connection resource is automatically deleted from Azure, leaving no trace

### When to Use Dynamic vs Static Resources
- **Dynamic** (`DynamicResourceCreation: true`): Best for development, testing, and temporary deployments
- **Static** (`DynamicResourceCreation: false`): Best for production environments where you want persistent, manually managed resources

   
5. Before testing the relay, your Azure Bot's messaging endpoint must be updated to match the relay.
   
    a. Login to the Azure portal and open your Azure Bot's registration.
    
    b. Select **Settings** under Bot management to open the settings blade.
    
    c. In the **Messaging endpoint** field, enter the service bus namespace and relay.
    
    d. Append **"/api/messages"** to the end to create the full endpoint to be used. For example, "https://example-relay.servicebus.windows.net/hc1/api/messages".
    
    e. Click **Save** when completed.
   
6. In Visual Studio, press **F5** to run the project.
   
7. Open and run your locally hosted bot/agent.
   
8. Test your bot/agent on a channel (Test in Web Chat, Skype, Teams, etc.). User data is captured and logged as activity occurs.

9. Once testing is completed, you can compile the project into an executable.

    a. Right click the project folder in Visual Studio and select **Publish**.

    b. For **Pick a publish Target**, select **Folder**.

    c. For **Folder or File Share**, choose an output location or keep the default.

    d. Click **Create Profile** to create a publish profile.

    e. Click **Configure...** to change the build configuration and change the following:

    - **Configuration** to "Debug | Any CPU"
    - **Deployment Mode** to "Self-contained"
    - **Target Runtime** to "win-x64"

    f. Click **Save** and then **Publish**

    g. The .exe will output to the **/bin/debug** folder, along with other necessary files, located in the project's directory folder. All the files are necessary to run and should be included when moving the .exe to a new folder/location.
    - The **appsettings.json** is in the same folder and can be edited as credentials change without needing to recompile the project.

## Dependencies and Architecture

### .NET Core Project Architecture (.NET 8)
The **RelayTunnelUsingHybridConnection** project includes:

- **Core Relay Functionality**: HTTP request proxying using Microsoft.Azure.Relay
- **ARM Template Automation**: Dynamic resource management using Azure.ResourceManager libraries
- **Multi-Relay Support**: Configure multiple relay endpoints in a single application
- **Flexible Authentication**: Support for various Azure authentication methods (Azure CLI, Service Principal, Managed Identity)

### Key NuGet Packages
- **Microsoft.Azure.Relay** (v2.0.0) - Core hybrid connection functionality
- **Azure.ResourceManager** (v1.9.0) - ARM resource management
- **Azure.ResourceManager.Relay** (v1.1.0) - Relay-specific ARM operations  
- **Azure.Identity** (v1.16.0) - Azure authentication
- **Microsoft.Extensions.Configuration** - Configuration management
- **Newtonsoft.Json** - JSON processing

### Project Files
- **`DispatcherService.cs`** - Handles HTTP request proxying and resource lifecycle
- **`RelayResourceManager.cs`** - Manages ARM operations for hybrid connections
- **`RelayConfig.cs`** - Configuration model with ARM properties
- **`AzureManagementConfig.cs`** - Azure authentication configuration
- **`Program.cs`** - Application entry point and orchestration
- **`appsettings.json`** - Configuration file (supports multiple relay configurations)

## Troubleshooting

### Common Issues with Dynamic Resource Creation

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

## Configuration Reference

### RelayTunnelUsingHybridConnection - appsettings.json Properties

#### AzureManagement Section
| Property | Type | Required | Description |
|----------|------|----------|-------------|
| `SubscriptionId` | string | Yes* | Your Azure subscription ID. Required if any relay uses `DynamicResourceCreation: true` |
| `TenantId` | string | No | Azure tenant ID. Optional when using DefaultAzureCredential |
| `UseDefaultAzureCredential` | boolean | No | If `true` (default), uses Azure CLI, Managed Identity, or Visual Studio credentials. If `false`, uses ClientId/ClientSecret |
| `ClientId` | string | No* | Service Principal client ID. Required if `UseDefaultAzureCredential: false` |
| `ClientSecret` | string | No* | Service Principal client secret. Required if `UseDefaultAzureCredential: false` |

#### Relays Section
| Property | Type | Required | Description |
|----------|------|----------|-------------|
| `RelayNamespace` | string | Yes | Full namespace URL (e.g., "namespace.servicebus.windows.net") |
| `RelayName` | string | Yes | Name of the hybrid connection |
| `PolicyName` | string | Yes | Name of the shared access policy |
| `PolicyKey` | string | Yes | Key for the shared access policy |
| `TargetServiceAddress` | string | Yes | Local service URL to proxy requests to (e.g., "http://localhost:3978") |
| `IsEnabled` | boolean | Yes | Whether this relay configuration is active |
| `DynamicResourceCreation` | boolean | No | If `true`, automatically creates/deletes the hybrid connection resource. Default: `false` |
| `ResourceGroupName` | string | Yes* | Azure resource group name. Required if `DynamicResourceCreation: true` |
| `Description` | string | No | Description for the dynamically created resource. Default: "Dynamically created hybrid connection" |
| `RequiresClientAuthorization` | boolean | No | Whether clients need authorization to connect. Default: `true` |

### RelayTunnelUsingWCF - appsettings.json Properties

#### Relays Section  
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

### Key Differences Between Projects

| Feature | .NET Core (RelayTunnelUsingHybridConnection) | .NET Framework (RelayTunnelUsingWCF) |
|---------|----------------------------------------|------------------------------------------|
| **Relay Type** | Hybrid Connections | WCF Relay |
| **Dynamic Creation** | ✅ Supported via ARM | ❌ Not supported |
| **Namespace Format** | Full URL required | Name only |
| **Authentication** | Multiple options (Azure CLI, Service Principal, Managed Identity) | SAS key only |
| **.NET Version** | .NET 8 | .NET Framework |
| **Modern Support** | ✅ Supported | ⚠️ Legacy (deprecated libraries) |
