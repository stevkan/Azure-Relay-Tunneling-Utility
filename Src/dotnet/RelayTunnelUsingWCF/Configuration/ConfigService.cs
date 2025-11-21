using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using Newtonsoft.Json;

namespace RelayTunnelUsingWCF.Configuration
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

        public string GetConfigPath()
        {
            return _configPath;
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
                return JsonConvert.DeserializeObject<AppConfig>(content);
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

            var content = JsonConvert.SerializeObject(config, Formatting.Indented);
            File.WriteAllText(_configPath, content);
        }

        public string EncryptKey(string plainTextKey)
        {
            var plainBytes = Encoding.UTF8.GetBytes(plainTextKey);
            // In .NET Framework, ProtectedData is in System.Security.Cryptography
            var encryptedBytes = ProtectedData.Protect(plainBytes, null, DataProtectionScope.CurrentUser);
            return Convert.ToBase64String(encryptedBytes);
        }

        public string DecryptKey(string encryptedKeyBase64)
        {
            var encryptedBytes = Convert.FromBase64String(encryptedKeyBase64);
            var plainBytes = ProtectedData.Unprotect(encryptedBytes, null, DataProtectionScope.CurrentUser);
            return Encoding.UTF8.GetString(plainBytes);
        }
    }
}
