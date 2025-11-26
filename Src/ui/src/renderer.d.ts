import { AppConfig } from '@shared/types/Configuration';

export interface IElectronAPI {
  getConfig: () => Promise<AppConfig>;
  saveConfig: (config: AppConfig) => Promise<boolean>;
  encryptKey: (key: string) => Promise<string>;
  decryptKey: (encryptedKey: string) => Promise<string>;
  startTunnel: (id: string) => Promise<boolean>;
  stopTunnel: (id: string) => Promise<boolean>;
  getTunnelStatus: (id: string) => Promise<'running' | 'stopped' | 'error'>;
  deleteTunnel: (id: string) => Promise<boolean>;
}

declare global {
  interface Window {
    electronAPI: IElectronAPI;
  }
}
