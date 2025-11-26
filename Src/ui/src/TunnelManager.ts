import { DispatcherService } from '@shared/dispatcher-service';
import { ConfigService } from '@shared/services/ConfigService';
import { AppConfig, TunnelConfig } from '@shared/types/Configuration';
import { RelayResourceManager } from '@shared/relay-resource-manager';
import { ChildProcess, spawn } from 'child_process';
import path from 'path';
import { SimpleLogger } from '@shared/simple-logger';
import { app } from 'electron';
import fs from 'fs';

interface ActiveTunnel {
  id: string;
  process?: ChildProcess;
  dispatcher?: DispatcherService;
  abortController?: AbortController;
  status: 'running' | 'stopped' | 'error';
  error?: string;
}

export class TunnelManager {
  private activeTunnels = new Map<string, ActiveTunnel>();
  private configService: ConfigService;

  constructor(configService: ConfigService) {
    this.configService = configService;
  }

  public getStatus(id: string) {
    return this.activeTunnels.get(id)?.status || 'stopped';
  }

  public async startTunnel(id: string): Promise<void> {
    const config = this.configService.loadConfig();
    const tunnel = config.tunnels.find(t => t.id === id);
    if (!tunnel) throw new Error('Tunnel not found');

    if (this.activeTunnels.get(id)?.status === 'running') {
      return;
    }

    try {
      if (tunnel.type === 'typescript') {
        await this.startTsTunnel(tunnel);
      } else if (tunnel.type === 'dotnet-core') {
        await this.startDotNetCoreTunnel(tunnel);
      } else if (tunnel.type === 'dotnet-wcf') {
        await this.startDotNetWcfTunnel(tunnel);
      }
    } catch (error: any) {
      console.error(`Failed to start tunnel ${id}:`, error);
      this.activeTunnels.set(id, { id, status: 'error', error: error.message });
      throw error;
    }
  }

  public async stopTunnel(id: string): Promise<void> {
    const active = this.activeTunnels.get(id);
    if (!active) return;

    if (active.dispatcher && active.abortController) {
      active.abortController.abort();
      await active.dispatcher.closeAsync(new AbortController().signal);
    }

    if (active.process) {
      active.process.kill();
    }

    this.activeTunnels.delete(id);
  }

  private async startTsTunnel(tunnel: TunnelConfig) {
    const logger = new SimpleLogger('debug');
    const policyKey = this.configService.decryptKey(tunnel.encryptedKey);
    
    let resourceManager: RelayResourceManager | null = null;

    if (tunnel.dynamicResourceCreation) {
        if (!tunnel.azureManagement) {
             throw new Error('Azure Management configuration is required for dynamic resource creation');
        }
        
        // Ensure defaults are applied if values are missing (e.g. useDefaultAzureCredential)
        const azureConfig = {
            ...tunnel.azureManagement,
            useDefaultAzureCredential: tunnel.azureManagement.useDefaultAzureCredential ?? true
        };

        resourceManager = new RelayResourceManager(azureConfig, logger);
    }

    const relayConfig = {
        relayNamespace: tunnel.relayNamespace,
        relayName: tunnel.hybridConnectionName,
        policyName: tunnel.keyName,
        policyKey: policyKey,
        targetServiceAddress: `http://${tunnel.targetHost}:${tunnel.targetPort}/`,
        isEnabled: true,
        enableDetailedLogging: tunnel.enableDetailedLogging ?? true,
        dynamicResourceCreation: tunnel.dynamicResourceCreation ?? false,
        resourceGroupName: tunnel.resourceGroupName,
        requiresClientAuthorization: tunnel.requiresClientAuthorization ?? true,
        description: tunnel.description
    };

    const dispatcher = new DispatcherService(relayConfig as any, resourceManager, logger);
    const abortController = new AbortController();
    
    await dispatcher.openAsync(abortController.signal);

    this.activeTunnels.set(tunnel.id, {
      id: tunnel.id,
      status: 'running',
      dispatcher,
      abortController
    });
  }

  private getBinaryPath(relativePath: string, fileName: string): string {
    if (app.isPackaged) {
      return path.join(process.resourcesPath, 'bin', fileName);
    } else {
      return path.resolve(__dirname, relativePath, fileName);
    }
  }

  private async startDotNetCoreTunnel(tunnel: TunnelConfig) {
    const exePath = this.getBinaryPath(
        '../../../../Src/dotnet/RelayTunnelUsingHybridConnection/bin/Debug/net8.0/', 
        'RelayTunnelUsingHybridConnection.exe'
    );
    
    console.log(`Launching .NET Core tunnel: ${exePath}`);
    if (!fs.existsSync(exePath)) {
        throw new Error(`Executable not found at ${exePath}`);
    }

    const child = spawn(exePath, ['--tunnel-id', tunnel.id], {
        windowsHide: true
    });

    this.setupChildProcess(tunnel.id, child);
  }

  private async startDotNetWcfTunnel(tunnel: TunnelConfig) {
    const exePath = this.getBinaryPath(
        '../../../../Src/dotnet/RelayTunnelUsingWCF/bin/Debug/',
        'RelayTunnelUsingWCF.exe'
    );
    
    console.log(`Launching .NET WCF tunnel: ${exePath}`);
    if (!fs.existsSync(exePath)) {
        throw new Error(`Executable not found at ${exePath}`);
    }

    const child = spawn(exePath, ['--tunnel-id', tunnel.id], {
        windowsHide: true
    });

    this.setupChildProcess(tunnel.id, child);
  }

  private setupChildProcess(id: string, child: ChildProcess) {
    child.stdout?.on('data', (data) => console.log(`[${id}] ${data}`));
    child.stderr?.on('data', (data) => console.error(`[${id}] ${data}`));
    
    child.on('exit', (code) => {
        console.log(`[${id}] Process exited with code ${code}`);
        if (this.activeTunnels.get(id)?.process === child) {
            this.activeTunnels.delete(id);
        }
    });

    this.activeTunnels.set(id, {
        id,
        status: 'running',
        process: child
    });
  }
}
