using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace RelayTunnelUsingHybridConnection.Configuration
{
    public class AppConfig
    {
        [JsonPropertyName("version")]
        public int Version { get; set; } = 1;

        [JsonPropertyName("tunnels")]
        public List<TunnelConfig> Tunnels { get; set; } = new List<TunnelConfig>();
    }

    public class TunnelConfig
    {
        [JsonPropertyName("id")]
        public string Id { get; set; }

        [JsonPropertyName("name")]
        public string Name { get; set; }

        [JsonPropertyName("type")]
        public string Type { get; set; }

        [JsonPropertyName("relayNamespace")]
        public string RelayNamespace { get; set; }

        [JsonPropertyName("hybridConnectionName")]
        public string HybridConnectionName { get; set; }

        [JsonPropertyName("keyName")]
        public string KeyName { get; set; }

        [JsonPropertyName("encryptedKey")]
        public string EncryptedKey { get; set; }

        [JsonPropertyName("targetHost")]
        public string TargetHost { get; set; }

        [JsonPropertyName("targetPort")]
        public int TargetPort { get; set; }
    }
}
