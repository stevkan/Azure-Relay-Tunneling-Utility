import { Readable } from 'stream'
import { Logger } from 'pino'
import type { RelayConfig } from './config.js'
import type { RelayResourceManager } from './relay-resource-manager.js'

import { createRequire } from 'module'
const require = createRequire(import.meta.url)

const https = require('hyco-https')
const WebSocket = require('hyco-ws')
// const fetch = require('node-fetch') // using global fetch

export class DispatcherService {
  private httpServer?: any
  private wsServer?: any

  constructor(
    private config: RelayConfig,
    private resourceManager: RelayResourceManager | null,
    private logger: Logger
  ) {}

  async openAsync(_token: AbortSignal): Promise<void> {
    try {

      if (this.config.dynamicResourceCreation && this.resourceManager) {
        console.log(`Creating Hybrid Connection '${this.config.relayName}'...`)
        const created = await this.resourceManager.createHybridConnectionAsync(this.config)
        if (!created) {
          throw new Error(`Failed to create Hybrid Connection '${this.config.relayName}'`)
        }
        console.log(`✓ Created '${this.config.relayName}'`)
      }

      const listenUri = https.createRelayListenUri(this.config.relayNamespace, this.config.relayName)

      this.httpServer = https.createRelayedServer(
        {
          server: listenUri,
          token: () => https.createRelayToken(listenUri, this.config.policyName, this.config.policyKey)
        },
        async (req: any, res: any) => {
          await this.handleHttpRequest(req, res)
        }
      )

      this.httpServer.on('error', (error: Error) => {
        console.error(`HTTPS Server error: ${error.message}`)
      })

      this.httpServer.listen((err?: Error) => {
        if (err) {
          console.error(`HTTPS listen error: ${err.message}`)
          throw err
        }
      })

      const wsListenUri = WebSocket.createRelayListenUri(this.config.relayNamespace, this.config.relayName)
      
      this.wsServer = WebSocket.createRelayedServer(
        {
          server: wsListenUri,
          token: () => WebSocket.createRelayToken(wsListenUri, this.config.policyName, this.config.policyKey)
        },
        (ws: any) => {
          this.handleWebSocketConnection(ws)
        }
      )

      this.wsServer.on('error', (error: Error) => {
        console.error(`WebSocket Server error: ${error.message}`)
      })

      const namespace = this.config.relayNamespace.includes('.servicebus.windows.net')
        ? this.config.relayNamespace
        : `${this.config.relayNamespace}.servicebus.windows.net`
      const publicEndpoint = `https://${namespace}/${this.config.relayName}`

      console.log('─────────────────────────────────────────────────────────')
      console.log(`✓ Relay '${this.config.relayName}' is ready`)
      console.log(`  Public Endpoint:  ${publicEndpoint}`)
      console.log(`  Routing To:       ${this.config.targetServiceAddress}`)
      console.log(`  WebSocket:        Enabled → ${this.config.targetServiceAddress.replace(/^http/, 'ws')}`)
      console.log('─────────────────────────────────────────────────────────')
    } catch (error: any) {
      this.logger.error(`Failed to start relay '${this.config.relayName}': ${error.message}`)
      throw error
    }
  }

  async closeAsync(_token: AbortSignal): Promise<void> {
    try {
      this.logger.info(`Stopping relay: ${this.config.relayName}`)

      if (this.wsServer) {
        await new Promise<void>((resolve) => {
          this.wsServer.close(() => resolve())
        })
      }

      if (this.httpServer) {
        await new Promise<void>((resolve) => {
          this.httpServer.close(() => resolve())
        })
      }

      if (this.config.dynamicResourceCreation && this.resourceManager) {
        await this.resourceManager.deleteHybridConnectionAsync(this.config)
      }

      this.logger.info(`✓ Stopped relay: ${this.config.relayName}`)
    } catch (error: any) {
      this.logger.error(`Error stopping relay '${this.config.relayName}': ${error.message}`)
      throw error
    }
  }

  private async handleHttpRequest(req: any, res: any): Promise<void> {
    try {
      // Strip the relay name from the beginning of the path
      // e.g., /botport/api/messages -> /api/messages
      let path = req.url
      const relayPrefix = `/${this.config.relayName}`
      if (path.startsWith(relayPrefix)) {
        path = path.substring(relayPrefix.length)
      }
      
      const targetUrl = new URL(path, this.config.targetServiceAddress).toString()

      if (this.config.enableDetailedLogging) {
        this.logger.info(`Proxying ${req.method} ${req.url} to ${targetUrl}`)
      }

      const headers: any = { ...req.headers }
      delete headers.host
      delete headers.connection
      delete headers['content-length']

      const fetchOptions: any = {
        method: req.method,
        headers,
        redirect: 'manual'
      }

      if (req.method !== 'GET' && req.method !== 'HEAD') {
        fetchOptions.body = req
      }

      const response = await fetch(targetUrl, fetchOptions)

      const responseHeaders: any = {}
      response.headers.forEach((value: string, name: string) => {
        responseHeaders[name] = value
      })

      res.writeHead(response.status, response.statusText, responseHeaders)
      
      if (response.body) {
        // @ts-ignore
        Readable.fromWeb(response.body).pipe(res)
      } else {
        res.end()
      }
    } catch (error: any) {
      console.error(`HTTP proxy error for ${req.method} ${req.url}: ${error.message}`)
      if (error.stack && this.config.enableDetailedLogging) {
        console.error(error.stack)
      }
      
      if (!res.headersSent) {
        res.writeHead(502, { 'Content-Type': 'text/plain' })
      }
      res.end(`Relay proxy error: ${error.message}`)
    }
  }

  private handleWebSocketConnection(ws: any): void {
    try {
      const targetWsUrl = this.config.targetServiceAddress.replace(/^http/, 'ws')

      if (this.config.enableDetailedLogging) {
        this.logger.info(`WebSocket connection established, proxying to ${targetWsUrl}`)
      }

      const targetWs = new (require('ws'))(targetWsUrl)

      targetWs.on('open', () => {
        this.logger.debug('Target WebSocket connection established')

        ws.on('message', (data: any) => {
          if (targetWs.readyState === 1) {
            targetWs.send(data)
          }
        })

        targetWs.on('message', (data: any) => {
          if (ws.readyState === 1) {
            ws.send(data)
          }
        })

        ws.on('close', () => {
          this.logger.debug('Relay WebSocket closed')
          targetWs.close()
        })

        targetWs.on('close', () => {
          this.logger.debug('Target WebSocket closed')
          ws.close()
        })

        ws.on('error', (error: Error) => {
          this.logger.error(`Relay WebSocket error: ${error.message}`)
          targetWs.close()
        })

        targetWs.on('error', (error: Error) => {
          this.logger.error(`Target WebSocket error: ${error.message}`)
          ws.close()
        })
      })

      targetWs.on('error', (error: Error) => {
        this.logger.error(`Failed to connect to target WebSocket: ${error.message}`)
        ws.close()
      })
    } catch (error: any) {
      this.logger.error(`WebSocket proxy error: ${error}`)
      ws.close()
    }
  }
}
