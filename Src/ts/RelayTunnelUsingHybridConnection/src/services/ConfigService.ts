import * as fs from 'fs';
import * as path from 'path';
import { Dpapi, isPlatformSupported } from '@primno/dpapi';
import { AppConfig } from '../types/Configuration.js';

export class ConfigService {
  private configPath: string;

  constructor(configPath?: string) {
    if (configPath) {
      this.configPath = configPath;
    } else {
      const appData = process.env.APPDATA || (process.platform == 'darwin' ? process.env.HOME + '/Library/Preferences' : process.env.HOME + "/.local/share");
      this.configPath = path.join(appData, 'AzureRelayTunnel', 'config.json');
    }
  }

  public getConfigPath(): string {
    return this.configPath;
  }

  public ensureConfigDirectory(): void {
    const dir = path.dirname(this.configPath);
    if (!fs.existsSync(dir)) {
      fs.mkdirSync(dir, { recursive: true });
    }
  }

  public loadConfig(): AppConfig {
    if (!fs.existsSync(this.configPath)) {
      return { version: 1, tunnels: [] };
    }
    const content = fs.readFileSync(this.configPath, 'utf-8');
    try {
      return JSON.parse(content) as AppConfig;
    } catch (error) {
      console.error("Error parsing config file:", error);
      return { version: 1, tunnels: [] };
    }
  }

  public saveConfig(config: AppConfig): void {
    this.ensureConfigDirectory();
    fs.writeFileSync(this.configPath, JSON.stringify(config, null, 2), 'utf-8');
  }

  public encryptKey(plainTextKey: string): string {
    if (!isPlatformSupported) {
      throw new Error('DPAPI encryption is only supported on Windows.');
    }
    const buffer = Buffer.from(plainTextKey, 'utf-8');
    const encrypted = Dpapi.protectData(buffer, null, 'CurrentUser');
    return Buffer.from(encrypted).toString('base64');
  }

  public decryptKey(encryptedKeyBase64: string): string {
    if (!isPlatformSupported) {
      throw new Error('DPAPI decryption is only supported on Windows.');
    }
    const encrypted = Buffer.from(encryptedKeyBase64, 'base64');
    const decrypted = Dpapi.unprotectData(encrypted, null, 'CurrentUser');
    return Buffer.from(decrypted).toString('utf-8');
  }
}
