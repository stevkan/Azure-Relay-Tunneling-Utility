using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace RelayTunnelUsingHybridConnection.Configuration
{
    public class ConfigService
    {
        private readonly string _configPath;

        public ConfigService(string configPath = null)
        {
            if (!string.IsNullOrEmpty(configPath))
            {
                _configPath = configPath;
            }
            else
            {
                var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
                _configPath = Path.Combine(appData, "AzureRelayTunnel", "config.json");
            }
        }

        public AppConfig LoadConfig()
        {
            if (!File.Exists(_configPath))
            {
                return new AppConfig();
            }

            var content = File.ReadAllText(_configPath);
            try 
            {
                return JsonSerializer.Deserialize<AppConfig>(content);
            }
            catch (Exception)
            {
                return new AppConfig();
            }
        }

        public void SaveConfig(AppConfig config)
        {
            var dir = Path.GetDirectoryName(_configPath);
            if (!Directory.Exists(dir))
            {
                Directory.CreateDirectory(dir);
            }

            var options = new JsonSerializerOptions { WriteIndented = true };
            var content = JsonSerializer.Serialize(config, options);
            File.WriteAllText(_configPath, content);
        }

        public string EncryptKey(string plainTextKey)
        {
            if (!OperatingSystem.IsWindows())
            {
                throw new PlatformNotSupportedException("DPAPI is only supported on Windows.");
            }

            var plainBytes = Encoding.UTF8.GetBytes(plainTextKey);
            // Using null for entropy to match the simple usage in Node.js
            // Scope must be CurrentUser to match Node's usage
            var encryptedBytes = ProtectedData.Protect(plainBytes, null, DataProtectionScope.CurrentUser);
            return Convert.ToBase64String(encryptedBytes);
        }

        public string DecryptKey(string encryptedKeyBase64)
        {
            if (!OperatingSystem.IsWindows())
            {
                throw new PlatformNotSupportedException("DPAPI is only supported on Windows.");
            }

            var encryptedBytes = Convert.FromBase64String(encryptedKeyBase64);
            var plainBytes = ProtectedData.Unprotect(encryptedBytes, null, DataProtectionScope.CurrentUser);
            return Encoding.UTF8.GetString(plainBytes);
        }
    }
}
