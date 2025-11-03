import { describe, it } from 'node:test'
import assert from 'node:assert'
import pino from 'pino'

describe('RelayResourceManager', () => {
  const mockLogger = pino({ level: 'silent' })

  it('should require subscriptionId in config', async () => {
    const { RelayResourceManager } = await import('../src/relay-resource-manager.js')
    
    const config = {
      useDefaultAzureCredential: true
    }

    assert.throws(() => {
      new RelayResourceManager(config, mockLogger)
    }, /SubscriptionId is required/)
  })

  it('should construct with DefaultAzureCredential when useDefaultAzureCredential is true', async () => {
    const { RelayResourceManager } = await import('../src/relay-resource-manager.js')
    
    const config = {
      subscriptionId: 'sub-123',
      useDefaultAzureCredential: true
    }

    const manager = new RelayResourceManager(config, mockLogger)
    assert.ok(manager)
  })

  it('should construct with ClientSecretCredential when useDefaultAzureCredential is false', async () => {
    const { RelayResourceManager } = await import('../src/relay-resource-manager.js')
    
    const config = {
      subscriptionId: 'sub-123',
      tenantId: 'tenant-123',
      clientId: 'client-123',
      clientSecret: 'secret-123',
      useDefaultAzureCredential: false
    }

    const manager = new RelayResourceManager(config, mockLogger)
    assert.ok(manager)
  })
})
