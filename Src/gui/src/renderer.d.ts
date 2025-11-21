import { AppConfig } from '@shared/types/Configuration';

export interface IElectronAPI {
  getConfig: () => Promise<AppConfig>;
  saveConfig: (config: AppConfig) => Promise<boolean>;
  encryptKey: (key: string) => Promise<string>;
  decryptKey: (encryptedKey: string) => Promise<string>;
}

declare global {
  interface Window {
    electronAPI: IElectronAPI;
  }
}
