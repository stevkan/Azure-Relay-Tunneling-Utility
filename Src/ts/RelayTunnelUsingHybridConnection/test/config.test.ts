import { describe, it, beforeEach, afterEach } from 'node:test'
import assert from 'node:assert'
import { loadConfigFromEnv, RelayConfigSchema, AppSettingsSchema } from '../src/config.js'

describe('Configuration', () => {
  describe('RelayConfigSchema', () => {

    it('should validate a valid relay configuration', () => {
      const validConfig = {
        relayNamespace: 'test-namespace',
        relayName: 'TEST-RELAY',
        policyName: 'RootManageSharedAccessKey',
        policyKey: 'test-key-123',
        targetServiceAddress: 'http://localhost:3978/',
        isEnabled: true,
        enableDetailedLogging: true,
        dynamicResourceCreation: false,
        requiresClientAuthorization: true
      }

      const result = RelayConfigSchema.safeParse(validConfig)
      assert.strictEqual(result.success, true)
      if (result.success) {
        assert.strictEqual(result.data.relayName, 'test-relay')
      }
    })

    it('should convert relay name to lowercase', () => {
      const config = {
        relayNamespace: 'test-namespace',
        relayName: 'MyRelayName',
        policyName: 'policy',
        policyKey: 'key',
        targetServiceAddress: 'http://localhost:3978/'
      }

      const result = RelayConfigSchema.safeParse(config)
      assert.strictEqual(result.success, true)
      if (result.success) {
        assert.strictEqual(result.data.relayName, 'myrelayname')
      }
    })

    it('should reject invalid URL in targetServiceAddress', () => {
      const config = {
        relayNamespace: 'test-namespace',
        relayName: 'test-relay',
        policyName: 'policy',
        policyKey: 'key',
        targetServiceAddress: 'not-a-valid-url'
      }

      const result = RelayConfigSchema.safeParse(config)
      assert.strictEqual(result.success, false)
    })

    it('should require relayNamespace', () => {
      const config = {
        relayNamespace: '',
        relayName: 'test-relay',
        policyName: 'policy',
        policyKey: 'key',
        targetServiceAddress: 'http://localhost:3978/'
      }

      const result = RelayConfigSchema.safeParse(config)
      assert.strictEqual(result.success, false)
    })
  })

  describe('loadConfigFromEnv', () => {
    beforeEach(() => {
      for (const key in process.env) {
        if (key.startsWith('AZURE_') || key === 'RELAYS' || key === 'SHUTDOWN_TIMEOUT_SECONDS') {
          delete process.env[key]
        }
      }
    })

    it('should load configuration from environment variables', () => {
      process.env.AZURE_SUBSCRIPTION_ID = 'sub-123'
      process.env.AZURE_USE_DEFAULT_CREDENTIAL = 'true'
      process.env.RELAYS = 'namespace|relay|policy|key|http://localhost:3978/|true|true|false||true|Test'
      process.env.SHUTDOWN_TIMEOUT_SECONDS = '30'

      const config = loadConfigFromEnv()

      assert.strictEqual(config.relays.length, 1)
      assert.strictEqual(config.relays[0].relayNamespace, 'namespace')
      assert.strictEqual(config.relays[0].relayName, 'relay')
      assert.strictEqual(config.relays[0].targetServiceAddress, 'http://localhost:3978/')
      assert.strictEqual(config.azureManagement?.subscriptionId, 'sub-123')
    })

    it('should parse multiple relays separated by semicolons', () => {
      process.env.RELAYS = 'ns1|relay1|p1|k1|http://localhost:3978/|true|true|false||true|R1;ns2|relay2|p2|k2|http://localhost:8080/|false|false|false||false|R2'

      const config = loadConfigFromEnv()

      assert.strictEqual(config.relays.length, 2)
      assert.strictEqual(config.relays[0].relayName, 'relay1')
      assert.strictEqual(config.relays[1].relayName, 'relay2')
      assert.strictEqual(config.relays[1].targetServiceAddress, 'http://localhost:8080/')
    })

    it('should parse boolean values correctly', () => {
      process.env.RELAYS = 'ns|relay|p|k|http://localhost:3978/|true|false|1|rg|0|Test'

      const config = loadConfigFromEnv()

      assert.strictEqual(config.relays[0].isEnabled, true)
      assert.strictEqual(config.relays[0].enableDetailedLogging, false)
      assert.strictEqual(config.relays[0].dynamicResourceCreation, true)
      assert.strictEqual(config.relays[0].requiresClientAuthorization, false)
    })

    it('should throw error if RELAYS environment variable is missing', () => {
      delete process.env.RELAYS

      assert.throws(() => {
        loadConfigFromEnv()
      }, /RELAYS environment variable is required/)
    })

    it('should throw error for invalid relay format', () => {
      process.env.RELAYS = 'only|four|fields|here'

      assert.throws(() => {
        loadConfigFromEnv()
      }, /Invalid relay configuration/)
    })
  })

  describe('AppSettingsSchema', () => {
    it('should validate complete app settings', () => {
      const settings = {
        azureManagement: {
          subscriptionId: 'sub-123',
          useDefaultAzureCredential: true
        },
        relays: [{
          relayNamespace: 'test-namespace',
          relayName: 'test-relay',
          policyName: 'policy',
          policyKey: 'key',
          targetServiceAddress: 'http://localhost:3978/'
        }],
        shutdownTimeoutSeconds: 30
      }

      const result = AppSettingsSchema.safeParse(settings)
      assert.strictEqual(result.success, true)
    })

    it('should require at least one relay', () => {
      const settings = {
        relays: []
      }

      const result = AppSettingsSchema.safeParse(settings)
      assert.strictEqual(result.success, false)
    })
  })
})
