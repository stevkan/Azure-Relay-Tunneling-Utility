export type TunnelType = 'typescript' | 'dotnet-core' | 'dotnet-wcf';

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
}

export interface AppConfig {
  version: number;
  tunnels: TunnelConfig[];
}
