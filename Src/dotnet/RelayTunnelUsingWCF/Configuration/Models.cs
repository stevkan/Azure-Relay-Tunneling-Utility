using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace RelayTunnelUsingWCF.Configuration
{
    public class AppConfig
    {
        [JsonProperty("version")]
        public int Version { get; set; } = 1;

        [JsonProperty("tunnels")]
        public List<TunnelConfig> Tunnels { get; set; } = new List<TunnelConfig>();
    }

    public class TunnelConfig
    {
        [JsonProperty("id")]
        public string Id { get; set; }

        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("type")]
        public string Type { get; set; }

        [JsonProperty("relayNamespace")]
        public string RelayNamespace { get; set; }

        [JsonProperty("hybridConnectionName")]
        public string HybridConnectionName { get; set; }

        [JsonProperty("keyName")]
        public string KeyName { get; set; }

        [JsonProperty("encryptedKey")]
        public string EncryptedKey { get; set; }

        [JsonProperty("targetHost")]
        public string TargetHost { get; set; }

        [JsonProperty("targetPort")]
        public int TargetPort { get; set; }
    }
}
