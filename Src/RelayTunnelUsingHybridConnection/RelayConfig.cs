namespace RelayTunnelUsingHybridConnection
{
    public class RelayConfig
    {
        private string _relayName;
        
        public bool IsEnabled { get; set; }
        public string RelayNamespace { get; set; }
        
        public string RelayName 
        { 
            get => _relayName;
            set
            {
                _relayName = value?.ToLowerInvariant();
                // Track if conversion happened for warning
                if (!string.IsNullOrEmpty(value) && value != _relayName)
                {
                    OriginalRelayName = value;
                }
            }
        }
        
        // Internal property to track if relay name was converted
        internal string OriginalRelayName { get; private set; }
        
        public string PolicyName { get; set; }
        public string PolicyKey { get; set; }
        public string TargetServiceAddress { get; set; }
        
        // WebSocket Support properties
        public bool EnableWebSocketSupport { get; set; } = true;
        public string TargetWebSocketAddress { get; set; }
        
        // ARM Resource Management properties
        public bool DynamicResourceCreation { get; set; } = false;
        public string ResourceGroupName { get; set; }
        public string Description { get; set; } = "Dynamically created hybrid connection";
        public bool RequiresClientAuthorization { get; set; } = true;
    }
}
