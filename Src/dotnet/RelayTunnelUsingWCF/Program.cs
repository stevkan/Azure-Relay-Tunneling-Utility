using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.ServiceModel;
using System.ServiceModel.Description;
using Microsoft.ServiceBus;
using Newtonsoft.Json;
using RelayTunnelUsingWCF.Configuration;
using System.Diagnostics;

namespace RelayTunnelUsingWCF
{
    class Program
    {
        private static List<ServiceHost> _serviceHosts = new List<ServiceHost>();

        static void Main(string[] args)
        {
            var configService = new ConfigService();

            if (args.Length > 0 && args[0] == "config")
            {
                if (args.Length > 1 && args[1] == "edit")
                {
                    var path = configService.GetConfigPath();
                    Console.WriteLine($"Opening config file: {path}");
                    if (!File.Exists(path))
                    {
                        configService.SaveConfig(new AppConfig());
                    }
                    Process.Start(path);
                    return;
                }
                if (args.Length > 1 && args[1] == "show")
                {
                    var cfg = configService.LoadConfig();
                    Console.WriteLine($"Configuration file: {configService.GetConfigPath()}");
                    Console.WriteLine(JsonConvert.SerializeObject(cfg, Formatting.Indented));
                    return;
                }
            }

            Console.WriteLine("Azure Relay WCF Utility (.NET Framework)");
            Console.WriteLine("===============================================");

            try
            {
                var appConfig = configService.LoadConfig();
                var myTunnels = appConfig.Tunnels.Where(t => t.Type == "dotnet-wcf").ToList();

                if (appConfig.Tunnels.Count == 0)
                {
                    Console.WriteLine("No tunnels configured.");
                    Console.Write("Would you like to set up a tunnel now? [Y/n]: ");
                    var response = Console.ReadLine();
                    if (string.IsNullOrEmpty(response) || response.ToLower().StartsWith("y"))
                    {
                        var newTunnel = InteractiveSetup(configService);
                        if (newTunnel != null)
                        {
                            appConfig.Tunnels.Add(newTunnel);
                            configService.SaveConfig(appConfig);
                            Console.WriteLine("Configuration saved successfully!");
                            myTunnels.Add(newTunnel);
                        }
                        else
                        {
                            return;
                        }
                    }
                    else
                    {
                        Console.WriteLine("Exiting. Use 'RelayTunnelUsingWCF.exe config edit' to configure manually.");
                        return;
                    }
                }

                if (myTunnels.Count == 0)
                {
                    Console.WriteLine("No tunnels of type 'dotnet-wcf' found in configuration.");
                    Console.WriteLine($"Found {appConfig.Tunnels.Count} total tunnels.");
                    return;
                }

                Console.WriteLine($"Found {myTunnels.Count} enabled relay configuration(s):");
                Console.WriteLine();

                foreach (var t in myTunnels)
                {
                    try
                    {
                        var key = configService.DecryptKey(t.EncryptedKey);
                        var config = new RelayConfiguration
                        {
                            RelayNamespace = t.RelayNamespace,
                            RelayName = t.HybridConnectionName, // In WCF, this is the Relay name
                            PolicyName = t.KeyName,
                            PolicyKey = key,
                            TargetServiceAddress = $"http://{t.TargetHost}:{t.TargetPort}/",
                            ServiceDiscoveryMode = "Private" // Default to private
                        };

                        StartRelay(config);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"❌ Failed to start relay '{t.Name}': {ex.Message}");
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

        private static TunnelConfig InteractiveSetup(ConfigService configService)
        {
            try
            {
                Console.Write("Tunnel Name (e.g. Legacy WCF Service): ");
                var name = Console.ReadLine();

                Console.Write("Azure Relay Namespace: ");
                var ns = Console.ReadLine();

                Console.Write("Relay Name (WCF Relay): ");
                var hc = Console.ReadLine();

                Console.Write("SAS Key Name [RootManageSharedAccessKey]: ");
                var keyName = Console.ReadLine();
                if (string.IsNullOrWhiteSpace(keyName)) keyName = "RootManageSharedAccessKey";

                Console.Write("SAS Key: ");
                var key = ReadPassword();
                Console.WriteLine();

                Console.Write("Target Host [localhost]: ");
                var host = Console.ReadLine();
                if (string.IsNullOrWhiteSpace(host)) host = "localhost";

                Console.Write("Target Port [8080]: ");
                var portStr = Console.ReadLine();
                if (string.IsNullOrWhiteSpace(portStr)) portStr = "8080";
                int.TryParse(portStr, out int port);

                return new TunnelConfig
                {
                    Id = Guid.NewGuid().ToString(),
                    Name = name,
                    Type = "dotnet-wcf",
                    RelayNamespace = ns,
                    HybridConnectionName = hc,
                    KeyName = keyName,
                    EncryptedKey = configService.EncryptKey(key),
                    TargetHost = host,
                    TargetPort = port
                };
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error during setup: {ex.Message}");
                return null;
            }
        }

        private static string ReadPassword()
        {
            string pass = "";
            do
            {
                ConsoleKeyInfo key = Console.ReadKey(true);
                // Backspace Should Not Work
                if (key.Key != ConsoleKey.Backspace && key.Key != ConsoleKey.Enter)
                {
                    pass += key.KeyChar;
                    Console.Write("*");
                }
                else
                {
                    if (key.Key == ConsoleKey.Backspace && pass.Length > 0)
                    {
                        pass = pass.Substring(0, (pass.Length - 1));
                        Console.Write("\b \b");
                    }
                    else if(key.Key == ConsoleKey.Enter)
                    {
                        break;
                    }
                }
            } while (true);
            return pass;
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
