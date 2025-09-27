using System.Collections.Generic;

namespace RelayTunnelUsingWCF
{
    public class RelayConfiguration
    {
        public string RelayNamespace { get; set; }
        public string RelayName { get; set; }
        public string PolicyName { get; set; }
        public string PolicyKey { get; set; }
        public string TargetServiceAddress { get; set; }
        public string ServiceDiscoveryMode { get; set; } = "Private";
        public bool EnableDetailedLogging { get; set; } = true;
        public bool IsEnabled { get; set; } = true;
    }

    public class AppSettings
    {
        public List<RelayConfiguration> Relays { get; set; } = new List<RelayConfiguration>();
    }
}
