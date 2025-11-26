using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace RelayTunnelUsingHybridConnection.Configuration
{
    public class AppConfig
    {
        [JsonPropertyName("version")]
        public int Version { get; set; } = 1;

        [JsonPropertyName("azureManagement")]
        public AzureManagementConfig AzureManagement { get; set; }

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

        [JsonPropertyName("enableDetailedLogging")]
        public bool? EnableDetailedLogging { get; set; }

        [JsonPropertyName("dynamicResourceCreation")]
        public bool? DynamicResourceCreation { get; set; }

        [JsonPropertyName("resourceGroupName")]
        public string ResourceGroupName { get; set; }

        [JsonPropertyName("requiresClientAuthorization")]
        public bool? RequiresClientAuthorization { get; set; }

        [JsonPropertyName("description")]
        public string Description { get; set; }

        [JsonPropertyName("enableWebSocketSupport")]
        public bool? EnableWebSocketSupport { get; set; }

        [JsonPropertyName("targetWebSocketAddress")]
        public string TargetWebSocketAddress { get; set; }

        [JsonPropertyName("serviceDiscoveryMode")]
        public string ServiceDiscoveryMode { get; set; }

        [JsonPropertyName("azureManagement")]
        public AzureManagementConfig AzureManagement { get; set; }
    }

    public class AzureManagementConfig
    {
        [JsonPropertyName("subscriptionId")]
        public string SubscriptionId { get; set; }

        [JsonPropertyName("tenantId")]
        public string TenantId { get; set; }

        [JsonPropertyName("clientId")]
        public string ClientId { get; set; }

        [JsonPropertyName("clientSecret")]
        public string ClientSecret { get; set; }

        [JsonPropertyName("useDefaultAzureCredential")]
        public bool? UseDefaultAzureCredential { get; set; }
    }
}
