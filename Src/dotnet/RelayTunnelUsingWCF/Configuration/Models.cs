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

        [JsonProperty("enableDetailedLogging")]
        public bool? EnableDetailedLogging { get; set; }

        [JsonProperty("dynamicResourceCreation")]
        public bool? DynamicResourceCreation { get; set; }

        [JsonProperty("resourceGroupName")]
        public string ResourceGroupName { get; set; }

        [JsonProperty("requiresClientAuthorization")]
        public bool? RequiresClientAuthorization { get; set; }

        [JsonProperty("description")]
        public string Description { get; set; }

        [JsonProperty("enableWebSocketSupport")]
        public bool? EnableWebSocketSupport { get; set; }

        [JsonProperty("targetWebSocketAddress")]
        public string TargetWebSocketAddress { get; set; }

        [JsonProperty("serviceDiscoveryMode")]
        public string ServiceDiscoveryMode { get; set; }

        [JsonProperty("azureManagement")]
        public AzureManagementConfig AzureManagement { get; set; }
    }

    public class AzureManagementConfig
    {
        [JsonProperty("subscriptionId")]
        public string SubscriptionId { get; set; }

        [JsonProperty("tenantId")]
        public string TenantId { get; set; }

        [JsonProperty("clientId")]
        public string ClientId { get; set; }

        [JsonProperty("clientSecret")]
        public string ClientSecret { get; set; }

        [JsonProperty("useDefaultAzureCredential")]
        public bool? UseDefaultAzureCredential { get; set; }
    }
}
