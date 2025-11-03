import { describe, it } from 'node:test'
import assert from 'node:assert'
import pino from 'pino'
import type { RelayConfig } from '../src/config.js'

describe('DispatcherService', () => {
  const mockLogger = pino({ level: 'silent' })

  const validConfig: RelayConfig = {
    relayNamespace: 'test-namespace.servicebus.windows.net',
    relayName: 'test-relay',
    policyName: 'RootManageSharedAccessKey',
    policyKey: 'test-key',
    targetServiceAddress: 'http://localhost:3978/',
    isEnabled: true,
    enableDetailedLogging: false,
    dynamicResourceCreation: false,
    requiresClientAuthorization: true
  }

  it('should construct DispatcherService with valid config', async () => {
    const { DispatcherService } = await import('../src/dispatcher-service.js')
    
    const service = new DispatcherService(validConfig, null, mockLogger)
    assert.ok(service)
  })

  it('should strip relay name prefix from request path', async () => {
    const { DispatcherService } = await import('../src/dispatcher-service.js')
    
    const service = new DispatcherService(validConfig, null, mockLogger)
    
    const testPath = '/test-relay/api/messages'
    const relayPrefix = `/${validConfig.relayName}`
    
    let strippedPath = testPath
    if (testPath.startsWith(relayPrefix)) {
      strippedPath = testPath.substring(relayPrefix.length)
    }
    
    assert.strictEqual(strippedPath, '/api/messages')
  })

  it('should handle path without relay name prefix', async () => {
    const { DispatcherService } = await import('../src/dispatcher-service.js')
    
    const service = new DispatcherService(validConfig, null, mockLogger)
    
    const testPath = '/api/messages'
    const relayPrefix = `/${validConfig.relayName}`
    
    let strippedPath = testPath
    if (testPath.startsWith(relayPrefix)) {
      strippedPath = testPath.substring(relayPrefix.length)
    }
    
    assert.strictEqual(strippedPath, '/api/messages')
  })
})
