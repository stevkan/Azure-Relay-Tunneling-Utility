#!/usr/bin/env node

import { existsSync } from 'fs'
import { resolve } from 'path'
import yargs from 'yargs'
import { hideBin } from 'yargs/helpers'
import dotenv from 'dotenv'
import pino from 'pino'
import { SimpleLogger } from './simple-logger'
import { loadConfigFromEnv } from './config'
import { RelayResourceManager } from './relay-resource-manager'
import { DispatcherService } from './dispatcher-service'

async function main() {
  const argv = await yargs(hideBin(process.argv))
    .option('env-file', {
      alias: 'e',
      type: 'string',
      description: 'Path to .env file',
      default: '.env'
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

  let envPath = argv['env-file']
  
  // If not an absolute path, try relative to executable directory first
  if (!resolve(envPath).startsWith(envPath)) {
    const exeDir = __dirname
    const envInExeDir = resolve(exeDir, envPath)
    
    if (existsSync(envInExeDir)) {
      envPath = envInExeDir
    } else {
      envPath = resolve(envPath)
    }
  } else {
    envPath = resolve(envPath)
  }
  
  if (existsSync(envPath)) {
    dotenv.config({ path: envPath })
  } else {
    dotenv.config()
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

  try {
    let settings

    try {
      settings = loadConfigFromEnv()
    } catch (error: any) {
      console.error('❌ Configuration Error: Failed to load configuration from environment variables.')
      console.error()
      console.error(`Error: ${error.message}`)
      console.error()
      console.error('Please create a .env file with your Azure Relay settings.')
      console.error('Refer to .env.template for the required environment variables.')
      console.error()
      process.exit(1)
    }

    const enabledRelays = settings.relays.filter((r) => r.isEnabled)

    if (enabledRelays.length === 0) {
      console.log('No enabled relay configurations found.')
      console.log()
      process.exit(0)
    }

    const hasDynamicRelays = enabledRelays.some((r) => r.dynamicResourceCreation)

    let resourceManager: RelayResourceManager | null = null

    if (hasDynamicRelays) {
      if (!settings.azureManagement?.subscriptionId) {
        console.error('❌ Error: Dynamic resource creation is enabled but AZURE_SUBSCRIPTION_ID is not set.')
        console.error()
        process.exit(1)
      }

      console.log('Initializing Azure Resource Manager for dynamic resource management...')
      resourceManager = new RelayResourceManager(settings.azureManagement, logger)
      console.log('✓ Azure Resource Manager initialized')
    }

    console.log(`Found ${enabledRelays.length} enabled relay configuration(s):`)
    console.log()

    const dispatcherServices = enabledRelays.map((cfg) => {
      return new DispatcherService(cfg, resourceManager, logger.child({ relay: cfg.relayName }))
    })

    const abortController = new AbortController()

    await Promise.all(dispatcherServices.map((ds) => ds.openAsync(abortController.signal)))
    
    console.log()
    console.log('Press Ctrl+C to stop...')

    const shutdownHandler = async () => {
      console.log()
      console.log('Shutting down and cleaning up resources...')

      const shutdownTimeout = settings.shutdownTimeoutSeconds || 30
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
  } catch (error: any) {
    console.error(`❌ Error: ${error.message}`)
    if (error.stack && argv.verbose) {
      console.error(error.stack)
    }
    console.error()
    await waitForKeypress()
    process.exit(1)
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
