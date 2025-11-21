import { describe, it, afterEach, after } from 'node:test';
import assert from 'node:assert';
import * as fs from 'fs';
import * as path from 'path';
import * as os from 'os';
import { ConfigService } from '../src/services/ConfigService.js';
import { AppConfig } from '../src/types/Configuration.js';
import { isPlatformSupported } from '@primno/dpapi';

describe('ConfigService', () => {
  const tempDir = fs.mkdtempSync(path.join(os.tmpdir(), 'relay-tunnel-test-'));
  const configPath = path.join(tempDir, 'config.json');
  const configService = new ConfigService(configPath);

  afterEach(() => {
    if (fs.existsSync(configPath)) {
        fs.unlinkSync(configPath);
    }
  });

  after(() => {
      if (fs.existsSync(tempDir)) {
          fs.rmdirSync(tempDir);
      }
  });

  it('should return default config if file does not exist', () => {
    const config = configService.loadConfig();
    assert.strictEqual(config.version, 1);
    assert.deepStrictEqual(config.tunnels, []);
  });

  it('should save and load config', () => {
    const config: AppConfig = {
      version: 1,
      tunnels: [
        {
          id: 'test-id',
          name: 'test-tunnel',
          type: 'typescript',
          relayNamespace: 'ns',
          hybridConnectionName: 'hc',
          keyName: 'kn',
          encryptedKey: 'enc-key',
          targetHost: 'localhost',
          targetPort: 8080
        }
      ]
    };

    configService.saveConfig(config);
    const loaded = configService.loadConfig();
    assert.deepStrictEqual(loaded, config);
  });

  it('should encrypt and decrypt keys on Windows', (t) => {
      if (!isPlatformSupported) {
          t.skip('DPAPI not supported on this platform');
          return;
      }

      const secret = "superSecretKey123!";
      const encrypted = configService.encryptKey(secret);
      assert.notStrictEqual(encrypted, secret);
      
      const decrypted = configService.decryptKey(encrypted);
      assert.strictEqual(decrypted, secret);
  });
});
