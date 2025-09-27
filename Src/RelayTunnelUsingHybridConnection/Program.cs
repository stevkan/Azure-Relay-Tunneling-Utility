using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using System.Linq;

namespace RelayTunnelUsingHybridConnection
{
    public class Program
    {
        public static IConfiguration Configuration { get; set; }

        public static async Task Main(string[] args)
        {
            Console.WriteLine("Azure Service Bus Relay Utility (.NET Core)");
            Console.WriteLine("============================================");
            Console.WriteLine();

            var builder = new ConfigurationBuilder()
                .SetBasePath(AppDomain.CurrentDomain.BaseDirectory)
                .AddJsonFile("appsettings.json", true, true)
                .AddEnvironmentVariables();
            Configuration = builder.Build();

            try
            {
                // Load Azure Management configuration
                var azureManagementConfig = new AzureManagementConfig();
                Microsoft.Extensions.Configuration.ConfigurationBinder.Bind(Configuration.GetSection("AzureManagement"), azureManagementConfig);

                // Initialize ARM resource manager if we have valid configuration
                RelayResourceManager resourceManager = null;
                var hasDynamicRelays = false;

                // Use GetChildren and manual binding for .NET 8 compatibility
                var relayConfigs = new List<RelayConfig>();
                foreach (var section in Configuration.GetSection("Relays").GetChildren())
                {
                    var cfg = new RelayConfig();
                    Microsoft.Extensions.Configuration.ConfigurationBinder.Bind(section, cfg);
                    relayConfigs.Add(cfg);
                    
                    if (cfg.DynamicResourceCreation)
                    {
                        hasDynamicRelays = true;
                    }
                }

                if (relayConfigs.Count == 0)
                {
                    Console.WriteLine("No relay configurations found in appsettings.json.");
                    return;
                }

                // Initialize ARM resource manager if needed
                if (hasDynamicRelays)
                {
                    if (string.IsNullOrEmpty(azureManagementConfig.SubscriptionId))
                    {
                        Console.WriteLine("❌ Error: Dynamic resource creation is enabled but SubscriptionId is not configured in AzureManagement section.");
                        return;
                    }

                    Console.WriteLine("Initializing Azure Resource Manager for dynamic resource management...");
                    resourceManager = new RelayResourceManager(azureManagementConfig);
                    Console.WriteLine("✓ Azure Resource Manager initialized");
                }

                // Only create relays where IsEnabled is true
                var enabledRelayConfigs = relayConfigs.Where(cfg => cfg.IsEnabled).ToList();
                if (enabledRelayConfigs.Count == 0)
                {
                    Console.WriteLine("No enabled relay configurations found in appsettings.json.");
                    return;
                }

                Console.WriteLine($"Found {enabledRelayConfigs.Count} enabled relay configuration(s):");
                foreach (var cfg in enabledRelayConfigs)
                {
                    Console.WriteLine($"  - {cfg.RelayName} → {cfg.TargetServiceAddress} (Dynamic: {cfg.DynamicResourceCreation})");
                }
                Console.WriteLine();

                var dispatcherServices = enabledRelayConfigs.Select(cfg =>
                    new DispatcherService(cfg, resourceManager)
                ).ToList();

                var openTasks = dispatcherServices.Select(ds => ds.OpenAsync(CancellationToken.None)).ToList();
                await Task.WhenAll(openTasks);

                Console.ReadLine();

                var closeTasks = dispatcherServices.Select(ds => ds.CloseAsync(CancellationToken.None)).ToList();
                await Task.WhenAll(closeTasks);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error: {ex.Message}");
                if (ex.InnerException != null)
                {
                    Console.WriteLine($"   Inner Exception: {ex.InnerException.Message}");
                }
                Console.WriteLine();
                Console.WriteLine("Press any key to exit...");
                Console.ReadKey();
            }
        }
    }
}