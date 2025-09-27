namespace RelayTunnelUsingHybridConnection
{
    public class AzureManagementConfig
    {
        public string SubscriptionId { get; set; }
        public string TenantId { get; set; }
        public string ClientId { get; set; }
        public string ClientSecret { get; set; }
        public bool UseDefaultAzureCredential { get; set; } = true;
    }
}
