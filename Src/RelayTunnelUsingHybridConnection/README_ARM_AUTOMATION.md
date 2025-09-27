# ARM Template Automation Implementation

This document describes the implementation of ARM Template Automation in the RelayTunnelUsingHybridConnection project, which provides dynamic Hybrid Connection lifecycle management.

## 🎯 Goal Achieved

**Dynamic "Appear/Disappear" Behavior**: When you enable `DynamicResourceCreation: true` for a relay:
- **Start App**: Creates the Hybrid Connection resource in Azure → **Appears in Portal**
- **Stop App**: Deletes the Hybrid Connection resource from Azure → **Disappears from Portal**

## 📋 What Was Implemented

### 1. **New Classes Added**
- **`AzureManagementConfig.cs`**: Configuration for Azure ARM authentication
- **`RelayResourceManager.cs`**: Handles ARM operations for Hybrid Connections

### 2. **Enhanced Classes**
- **`RelayConfig.cs`**: Added ARM resource management properties
- **`DispatcherService.cs`**: Integrated resource lifecycle management
- **`Program.cs`**: Added ARM configuration handling and error management

### 3. **New NuGet Packages**
- `Azure.ResourceManager` v1.9.0
- `Azure.ResourceManager.Relay` v1.1.0  
- `Azure.Identity` v1.16.0

### 4. **Updated Configuration**
- Enhanced `appsettings.json` with ARM settings and new relay properties

## 🔧 Configuration

### Required Settings in appsettings.json

```json
{
  "AzureManagement": {
    "SubscriptionId": "your-subscription-id",
    "TenantId": "your-tenant-id", 
    "UseDefaultAzureCredential": true
  },
  "Relays": [
    {
      "RelayName": "dynamic-relay",
      "DynamicResourceCreation": true,
      "ResourceGroupName": "your-rg-name",
      // ... other settings
    }
  ]
}
```

### Authentication Options

1. **Default Azure Credential** (Recommended)
   - Uses Managed Identity, Azure CLI, Visual Studio, etc.
   - Set `UseDefaultAzureCredential: true`

2. **Service Principal**
   - Set `UseDefaultAzureCredential: false`
   - Provide `ClientId` and `ClientSecret`

## 🚀 How It Works

### Startup Flow
1. Load configuration including ARM settings
2. Initialize `RelayResourceManager` if dynamic relays exist
3. For each relay with `DynamicResourceCreation: true`:
   - Call ARM API to create Hybrid Connection
   - Wait for resource availability
   - Connect HybridConnectionListener
4. Start HTTP request proxying

### Shutdown Flow
1. Close HybridConnectionListener connections
2. For each dynamically created relay:
   - Call ARM API to delete Hybrid Connection
   - Resource disappears from Azure portal

## 📁 Files Changed

### Original State (Preserved in Memory)
- Uses basic Microsoft.Azure.Relay with pre-existing connections
- No dynamic resource management
- Simple configuration structure

### New Implementation
- **RelayTunnelUsingHybridConnection.csproj**: Added ARM management packages
- **RelayConfig.cs**: Added `DynamicResourceCreation`, `ResourceGroupName`, etc.
- **DispatcherService.cs**: Integrated ARM lifecycle in `OpenAsync`/`CloseAsync`
- **Program.cs**: Added ARM config loading and error handling
- **appsettings.json**: Extended with ARM settings and relay properties
- **AzureManagementConfig.cs**: New authentication configuration
- **RelayResourceManager.cs**: New ARM operations handler

## 🔐 Security & Permissions

### Required Azure Permissions
The authenticated identity needs:
- **Contributor** role on the Resource Group, OR
- **Relay Namespace Contributor** role on the specific namespace

### Recommended Setup
1. **Development**: Use Azure CLI authentication
2. **Production**: Use Managed Identity with minimal required permissions

## ⚡ Usage Examples

### Example 1: Static Relay (Original Behavior)
```json
{
  "RelayName": "existing-relay",
  "DynamicResourceCreation": false,  // Default
  "IsEnabled": true
}
```
→ Connects to pre-existing Hybrid Connection

### Example 2: Dynamic Relay (New Behavior)  
```json
{
  "RelayName": "temp-relay",
  "DynamicResourceCreation": true,   // NEW!
  "ResourceGroupName": "my-rg",
  "IsEnabled": true
}
```
→ Creates/deletes Hybrid Connection dynamically

## 🔄 Rollback Instructions

To revert to the original implementation:

1. **Restore packages**: Remove ARM packages from `.csproj`
2. **Restore RelayConfig.cs**: Remove ARM properties  
3. **Restore DispatcherService.cs**: Revert constructor and lifecycle methods
4. **Restore Program.cs**: Remove ARM configuration handling
5. **Restore appsettings.json**: Remove ARM section and new properties
6. **Delete new files**: `AzureManagementConfig.cs`, `RelayResourceManager.cs`

The original state is preserved in the AI memory for easy restoration.

## 🎉 Result

You now have **true dynamic relay behavior** where Hybrid Connection resources appear and disappear in the Azure portal based on your application's lifecycle, while maintaining compatibility with existing static relay configurations.
