using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.ServiceModel;
using System.ServiceModel.Description;
using Microsoft.ServiceBus;
using Newtonsoft.Json;

namespace RelayTunnelUsingWCF
{
    class Program
    {
        private static List<ServiceHost> _serviceHosts = new List<ServiceHost>();
        private static AppSettings _appSettings;

        static void Main(string[] args)
        {
            Console.WriteLine("Azure Relay WCF Utility (.NET Framework)");
            Console.WriteLine("===============================================");

            try
            {
                // Load configuration from appsettings.json
                LoadConfiguration();

                var enabledRelays = _appSettings.Relays.Where(r => r.IsEnabled).ToList();
                
                if (enabledRelays.Count == 0)
                {
                    Console.WriteLine("❌ No enabled relay configurations found in appsettings.json");
                    Console.WriteLine("Press any key to exit...");
                    Console.ReadKey();
                    return;
                }

                Console.WriteLine($"Found {enabledRelays.Count} enabled relay configuration(s):");
                Console.WriteLine();

                // Start each enabled relay
                foreach (var config in enabledRelays)
                {
                    try
                    {
                        StartRelay(config);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"❌ Failed to start relay '{config.RelayName}': {ex.Message}");
                    }
                }

                if (_serviceHosts.Count == 0)
                {
                    Console.WriteLine("❌ No relay services could be started.");
                    Console.WriteLine("Press any key to exit...");
                    Console.ReadKey();
                    return;
                }

                Console.WriteLine();
                Console.WriteLine($"✓ {_serviceHosts.Count} WCF Relay service(s) are now RUNNING and visible in Azure!");
                Console.WriteLine("✓ The relay endpoints will appear in your Azure Relay namespace.");
                Console.WriteLine("✓ Requests to the relays will be forwarded to their target services.");
                Console.WriteLine();
                Console.WriteLine("Press Ctrl+C or Enter to stop all services...");

                // Hook up console cancel handler for graceful shutdown
                Console.CancelKeyPress += OnCancelKeyPress;

                // Keep the services running
                Console.ReadLine();
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
            finally
            {
                StopAllServices();
            }
        }

        private static void LoadConfiguration()
        {
            var configPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "appsettings.json");
            
            if (!File.Exists(configPath))
            {
                throw new FileNotFoundException("appsettings.json file not found. Please create it in the application directory.");
            }

            var jsonContent = File.ReadAllText(configPath);
            _appSettings = JsonConvert.DeserializeObject<AppSettings>(jsonContent);

            if (_appSettings?.Relays == null || _appSettings.Relays.Count == 0)
            {
                throw new InvalidOperationException("No relay configurations found in appsettings.json");
            }

            // Check if configuration appears to be unconfigured (template values)
            CheckForUnconfiguredSettings();
        }

        private static void CheckForUnconfiguredSettings()
        {
            var unconfiguredRelays = new List<string>();

            foreach (var relay in _appSettings.Relays.Where(r => r.IsEnabled))
            {
                var missingFields = new List<string>();

                if (string.IsNullOrWhiteSpace(relay.RelayNamespace))
                    missingFields.Add("RelayNamespace");
                if (string.IsNullOrWhiteSpace(relay.RelayName))
                    missingFields.Add("RelayName");
                if (string.IsNullOrWhiteSpace(relay.PolicyName))
                    missingFields.Add("PolicyName");
                if (string.IsNullOrWhiteSpace(relay.PolicyKey))
                    missingFields.Add("PolicyKey");

                if (missingFields.Count > 0)
                {
                    unconfiguredRelays.Add($"Relay '{relay.RelayName ?? "unnamed"}' is missing: {string.Join(", ", missingFields)}");
                }
            }

            if (unconfiguredRelays.Count > 0)
            {
                Console.WriteLine("❌ Configuration Error: appsettings.json has not been properly configured.");
                Console.WriteLine();
                Console.WriteLine("Missing required configuration values:");
                foreach (var error in unconfiguredRelays)
                {
                    Console.WriteLine($"  • {error}");
                }
                Console.WriteLine();
                Console.WriteLine("Please update appsettings.json with your Azure Relay settings.");
                Console.WriteLine("Refer to appsettings-template.json for the required fields.");
                Console.WriteLine();
                throw new InvalidOperationException("Configuration validation failed. Please configure appsettings.json before running.");
            }
        }

        private static void StartRelay(RelayConfiguration config)
        {
            // Validate configuration
            ValidateConfiguration(config);

            Console.WriteLine($"Starting relay: {config.RelayName}");
            Console.WriteLine($"  Namespace: {config.RelayNamespace}");
            Console.WriteLine($"  Target: {config.TargetServiceAddress}");

            // Build the service address
            var serviceAddress = ServiceBusEnvironment.CreateServiceUri("https", config.RelayNamespace, config.RelayName);
            Console.WriteLine($"  Service Address: {serviceAddress}");

            // Create the service host (without base address to avoid local HTTP listener)
            var serviceHost = new ServiceHost(typeof(ConfigurableRelayProxyService));

            // Add the service behavior to inject configuration
            serviceHost.Description.Behaviors.Add(new RelayProxyServiceFactory(config));

            // Configure the endpoint with static relay binding
            var binding = new WebHttpRelayBinding(EndToEndWebHttpSecurityMode.None, RelayClientAuthenticationType.None)
            {
                IsDynamic = true  // Set to false for dynamic relays
            };
            
            var endpoint = serviceHost.AddServiceEndpoint(
                typeof(IRelayProxyService),
                binding,
                serviceAddress);

            // Configure the relay credentials
            var tokenProvider = TokenProvider.CreateSharedAccessSignatureTokenProvider(config.PolicyName, config.PolicyKey);
            endpoint.Behaviors.Add(new TransportClientEndpointBehavior { TokenProvider = tokenProvider });

            // Add web HTTP behavior for REST-style operations
            endpoint.Behaviors.Add(new WebHttpBehavior());

            // Configure service registry settings (makes the relay visible/invisible in Azure)
            var discType = string.Equals(config.ServiceDiscoveryMode, "Public", StringComparison.OrdinalIgnoreCase) 
                ? DiscoveryType.Public 
                : DiscoveryType.Private;
                
            endpoint.Behaviors.Add(new ServiceRegistrySettings(discType));

            // Open the service
            serviceHost.Open();
            _serviceHosts.Add(serviceHost);

            Console.WriteLine($"  ✓ Relay '{config.RelayName}' started successfully");
            Console.WriteLine();
        }

        private static void ValidateConfiguration(RelayConfiguration config)
        {
            if (string.IsNullOrWhiteSpace(config.RelayNamespace))
                throw new InvalidOperationException($"RelayNamespace is required for relay '{config.RelayName}'");
                
            if (string.IsNullOrWhiteSpace(config.RelayName))
                throw new InvalidOperationException("RelayName is required");
                
            if (string.IsNullOrWhiteSpace(config.PolicyName))
                throw new InvalidOperationException($"PolicyName is required for relay '{config.RelayName}'");
                
            if (string.IsNullOrWhiteSpace(config.PolicyKey))
                throw new InvalidOperationException($"PolicyKey is required for relay '{config.RelayName}'");
                
            if (string.IsNullOrWhiteSpace(config.TargetServiceAddress))
                throw new InvalidOperationException($"TargetServiceAddress is required for relay '{config.RelayName}'");

            // Validate target address format
            if (!Uri.TryCreate(config.TargetServiceAddress, UriKind.Absolute, out _))
                throw new InvalidOperationException($"TargetServiceAddress must be a valid absolute URI for relay '{config.RelayName}'");
        }

        private static void OnCancelKeyPress(object sender, ConsoleCancelEventArgs e)
        {
            Console.WriteLine();
            Console.WriteLine("Shutdown signal received. Closing all relay services...");
            e.Cancel = true; // Don't terminate immediately, let us clean up
            StopAllServices();
            Environment.Exit(0);
        }

        private static void StopAllServices()
        {
            try
            {
                foreach (var serviceHost in _serviceHosts.ToList())
                {
                    try
                    {
                        if (serviceHost.State == CommunicationState.Opened)
                        {
                            serviceHost.Close();
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"⚠ Error while stopping service: {ex.Message}");
                        try
                        {
                            serviceHost?.Abort();
                        }
                        catch
                        {
                            // Ignore errors during abort
                        }
                    }
                }

                _serviceHosts.Clear();
                Console.WriteLine("✓ All WCF Relay services stopped. The relay endpoints are no longer visible in Azure.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"⚠ Error during shutdown: {ex.Message}");
            }
        }
    }
}
