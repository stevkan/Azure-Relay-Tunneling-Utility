export type TunnelType = 'typescript' | 'dotnet-core' | 'dotnet-wcf';

export interface AzureManagementConfig {
  subscriptionId?: string;
  tenantId?: string;
  clientId?: string;
  clientSecret?: string;
  useDefaultAzureCredential?: boolean;
}

export interface TunnelConfig {
  id: string;
  name: string;
  type: TunnelType;
  relayNamespace: string;
  hybridConnectionName: string;
  keyName: string;
  encryptedKey: string; // Base64 encoded DPAPI blob
  targetHost: string;
  targetPort: number;
  
  // Advanced / Type-Specific Options
  enableDetailedLogging?: boolean;
  dynamicResourceCreation?: boolean;
  resourceGroupName?: string;
  requiresClientAuthorization?: boolean;
  description?: string;
  
  // .NET 8 Specific
  enableWebSocketSupport?: boolean;
  targetWebSocketAddress?: string;
  
  // WCF Specific
  serviceDiscoveryMode?: 'Private' | 'Public';
  
  // Azure Management (for dynamic creation)
  azureManagement?: AzureManagementConfig;
}

export interface AppConfig {
  version: number;
  tunnels: TunnelConfig[];
}
