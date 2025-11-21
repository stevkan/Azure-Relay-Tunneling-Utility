#!/usr/bin/env node

import yargs from 'yargs'
import { hideBin } from 'yargs/helpers'
import pino from 'pino'
import { SimpleLogger } from './simple-logger.js'
import { RelayResourceManager } from './relay-resource-manager.js'
import { DispatcherService } from './dispatcher-service.js'
import { ConfigService } from './services/ConfigService.js'
import { TunnelConfig } from './types/Configuration.js'
import { input, password, select } from '@inquirer/prompts'
import { v4 as uuidv4 } from 'uuid'
import { exec } from 'child_process'

async function main() {
  const argv = await yargs(hideBin(process.argv))
    .command('config', 'Manage configuration', (yargs) => {
      return yargs
        .command('edit', 'Open configuration file in default editor')
        .command('show', 'Show current configuration')
    })
    .option('verbose', {
      alias: 'v',
      type: 'boolean',
      description: 'Enable verbose logging',
      default: false
    })
    .help()
    .alias('help', 'h')
    .version()
    .alias('version', 'V')
    .parse()

  const configService = new ConfigService();
  const configPath = configService.getConfigPath();
  
  const command = argv._[0];
  if (command === 'config') {
    const subCommand = argv._[1];
    if (subCommand === 'edit') {
      console.log(`Opening config file: ${configPath}`);
      const startCommand = process.platform == 'win32' ? 'start' : 'open';
      exec(`${startCommand} "" "${configPath}"`);
      return;
    } else if (subCommand === 'show') {
      console.log(`Configuration file: ${configPath}`);
      const config = configService.loadConfig();
      console.log(JSON.stringify(config, null, 2));
      return;
    }
  }

  const isPkg = typeof (process as any).pkg !== 'undefined'
  
  const logger: any = isPkg
    ? new SimpleLogger(argv.verbose ? 'debug' : 'info')
    : pino({
        level: argv.verbose ? 'debug' : 'info',
        transport: {
          target: 'pino-pretty',
          options: {
            colorize: true,
            translateTime: 'HH:MM:ss',
            ignore: 'pid,hostname'
          }
        }
      })

  console.log('Azure Relay Hybrid Connection Utility (TypeScript/Node.js)')
  console.log('============================================================')
  console.log()

  let appConfig = configService.loadConfig();

  if (appConfig.tunnels.length === 0) {
    console.log('No tunnels configured. Entering interactive setup...');
    const shouldSetup = await select({
        message: 'No configuration found. Would you like to set up a tunnel now?',
        choices: [
            { name: 'Yes', value: true },
            { name: 'No', value: false }
        ]
    });

    if (shouldSetup) {
        const name = await input({ message: 'Tunnel Name (e.g. Production DB):' });
        const relayNamespace = await input({ message: 'Azure Relay Namespace:' });
        const hybridConnectionName = await input({ message: 'Hybrid Connection Name:' });
        const keyName = await input({ message: 'SAS Key Name (e.g. RootManageSharedAccessKey):', default: 'RootManageSharedAccessKey' });
        const key = await password({ message: 'SAS Key:' });
        const targetHost = await input({ message: 'Target Host (e.g. localhost):', default: 'localhost' });
        const targetPortStr = await input({ message: 'Target Port:', default: '8080' });

        const tunnel: TunnelConfig = {
            id: uuidv4(),
            name,
            type: 'typescript',
            relayNamespace,
            hybridConnectionName,
            keyName,
            encryptedKey: configService.encryptKey(key),
            targetHost,
            targetPort: parseInt(targetPortStr, 10)
        };

        appConfig.tunnels.push(tunnel);
        configService.saveConfig(appConfig);
        console.log('Configuration saved successfully!');
    } else {
        console.log('Exiting. Use "relay-tunnel config edit" to configure manually.');
        return;
    }
  }

  // Filter for TypeScript tunnels only
  const tsTunnels = appConfig.tunnels.filter(t => t.type === 'typescript');
  
  if (tsTunnels.length === 0) {
      console.log('No tunnels of type "typescript" found in configuration.');
      console.log(`Found ${appConfig.tunnels.length} total tunnels.`);
      return;
  }

  console.log(`Found ${tsTunnels.length} enabled relay configuration(s):`)
  console.log()

  // Map TunnelConfig to internal RelayConfig
  const relayConfigs = tsTunnels.map(t => {
      let policyKey = '';
      try {
          policyKey = configService.decryptKey(t.encryptedKey);
      } catch (err) {
          logger.error(`Failed to decrypt key for tunnel ${t.name}: ${err}`);
          return null;
      }

      return {
          relayNamespace: t.relayNamespace,
          relayName: t.hybridConnectionName,
          policyName: t.keyName,
          policyKey: policyKey,
          targetServiceAddress: `http://${t.targetHost}:${t.targetPort}/`,
          isEnabled: true,
          // Defaults for flags not yet in JSON config
          enableDetailedLogging: false,
          dynamicResourceCreation: false,
          requiresClientAuthorization: false,
          resourceGroupName: '',
          originalRelayName: t.hybridConnectionName
      };
  }).filter(c => c !== null);

  // TODO: Dynamic Resource Manager support is not fully ported to JSON config yet
  // It requires Azure Management creds which are also not in the simple schema yet.
  // Passing null for now.
  const resourceManager: RelayResourceManager | null = null;

  const dispatcherServices = relayConfigs.map((cfg) => {
    return new DispatcherService(cfg!, resourceManager, logger.child({ relay: cfg!.relayName }))
  })

  const abortController = new AbortController()

  await Promise.all(dispatcherServices.map((ds) => ds.openAsync(abortController.signal)))
  
  console.log()
  console.log('Press Ctrl+C to stop...')

  const shutdownHandler = async () => {
    console.log()
    console.log('Shutting down and cleaning up resources...')

    const shutdownTimeout = 30 // default
    const shutdownAbortController = new AbortController()
    const timeoutId = setTimeout(() => {
      shutdownAbortController.abort()
    }, shutdownTimeout * 1000)

    try {
      await Promise.all(
        dispatcherServices.map((ds) => ds.closeAsync(shutdownAbortController.signal))
      )
      clearTimeout(timeoutId)
      console.log('✓ All resources cleaned up successfully')
    } catch (error: any) {
      if (error.name === 'AbortError') {
        console.log(`⚠️ Shutdown timeout (${shutdownTimeout}s) exceeded. Some resources may not have cleaned up properly.`)
      } else {
        console.error(`⚠️ Error during cleanup: ${error.message}`)
      }
    }

    process.exit(0)
  }

  process.on('SIGINT', shutdownHandler)
  process.on('SIGTERM', shutdownHandler)
  process.on('SIGHUP', shutdownHandler)

  if (process.platform === 'win32') {
    const readline = await import('readline')
    const rl = readline.createInterface({
      input: process.stdin,
      output: process.stdout
    })

    rl.on('SIGINT', shutdownHandler)
  }
}

async function waitForKeypress() {
  if (process.stdin.isTTY) {
    console.log('Press any key to exit...')
    process.stdin.setRawMode(true)
    return new Promise<void>((resolve) => {
      process.stdin.once('data', () => {
        process.stdin.setRawMode(false)
        resolve()
      })
    })
  }
}

main().catch(async (error) => {
  console.error('Fatal error:', error)
  await waitForKeypress()
  process.exit(1)
})
