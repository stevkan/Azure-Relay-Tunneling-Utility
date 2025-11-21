import { DispatcherService } from '@shared/dispatcher-service';
import { ConfigService } from '@shared/services/ConfigService';
import { AppConfig, TunnelConfig } from '@shared/types/Configuration';
import { ChildProcess, spawn } from 'child_process';
import path from 'path';
import { SimpleLogger } from '@shared/simple-logger';

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
    
    const relayConfig = {
        relayNamespace: tunnel.relayNamespace,
        relayName: tunnel.hybridConnectionName,
        policyName: tunnel.keyName,
        policyKey: policyKey,
        targetServiceAddress: `http://${tunnel.targetHost}:${tunnel.targetPort}/`,
        isEnabled: true,
        enableDetailedLogging: true,
        dynamicResourceCreation: false,
        requiresClientAuthorization: false
    };

    const dispatcher = new DispatcherService(relayConfig as any, null, logger);
    const abortController = new AbortController();
    
    await dispatcher.openAsync(abortController.signal);

    this.activeTunnels.set(tunnel.id, {
      id: tunnel.id,
      status: 'running',
      dispatcher,
      abortController
    });
  }

  private async startDotNetCoreTunnel(tunnel: TunnelConfig) {
    // Path relative to the root of the repo for dev
    // In prod, we need to bundle these
    const exePath = path.resolve(__dirname, '../../../../../Src/dotnet/RelayTunnelUsingHybridConnection/bin/Debug/net8.0/RelayTunnelUsingHybridConnection.exe');
    
    // We can pass config via CLI args if we implement that, 
    // OR we rely on the fact that the process reads the SAME config file
    // BUT the .NET process by default reads all enabled tunnels. 
    // We want to run ONE specific tunnel.
    // The current .NET implementation runs ALL enabled tunnels from config.
    
    // To support running a single tunnel, we might need to update the .NET CLI to accept overrides or an ID.
    // Alternatively, the GUI can generate a temporary config file for that process.
    // OR we just let it run and it will pick up the config. But wait, if we have 5 tunnels in config, and we click "Start" on one in GUI,
    // we don't want to launch 5 processes each running 5 tunnels (25 tunnels!).
    
    // SOLUTION: Update .NET apps to accept `--tunnel-id <id>` to run only a specific tunnel from the shared config.
    // This is a good feature to add to Phase 3/4.
    
    console.log(`Launching .NET Core tunnel: ${exePath}`);
    const child = spawn(exePath, ['--tunnel-id', tunnel.id], {
        windowsHide: true
    });

    this.setupChildProcess(tunnel.id, child);
  }

  private async startDotNetWcfTunnel(tunnel: TunnelConfig) {
    const exePath = path.resolve(__dirname, '../../../../../Src/dotnet/RelayTunnelUsingWCF/bin/Debug/RelayTunnelUsingWCF.exe');
    
    console.log(`Launching .NET WCF tunnel: ${exePath}`);
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
