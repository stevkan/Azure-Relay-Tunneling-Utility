using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using System.Linq;
using System.Runtime.InteropServices;
using RelayTunnelUsingHybridConnection.Configuration;
using System.Diagnostics;
using System.Text.Json;

namespace RelayTunnelUsingHybridConnection
{
    public class Program
    {
        private delegate bool ConsoleCtrlDelegate(int dwCtrlType);

        [DllImport("kernel32.dll")]
        private static extern bool SetConsoleCtrlHandler(ConsoleCtrlDelegate handler, bool add);

        private const int CTRL_CLOSE_EVENT = 2;
        private const int CTRL_LOGOFF_EVENT = 5;
        private const int CTRL_SHUTDOWN_EVENT = 6;
        private const int DEFAULT_SHUTDOWN_TIMEOUT_SECONDS = 30;

        private static ConsoleCtrlDelegate _consoleCtrlHandler;

        // NOTE: We are moving away from IConfiguration for relays, but might still use it for AzureManagement if loaded from legacy env
        // For now, we focus on ConfigService.

        public static async Task Main(string[] args)
        {
            var configService = new ConfigService();
            string targetTunnelId = null;

            // Handle CLI args
            for (int i = 0; i < args.Length; i++)
            {
                if (args[i] == "--tunnel-id" && i + 1 < args.Length)
                {
                    targetTunnelId = args[i + 1];
                }
                else if (args[i] == "config")
                {
                    // Handle config commands...
                    if (i + 1 < args.Length && args[i + 1] == "edit")
                    {
                        var path = configService.GetConfigPath();
                        Console.WriteLine($"Opening config file: {path}");
                        // Create file if not exists to avoid editor error
                        if (!System.IO.File.Exists(path)) {
                            configService.SaveConfig(new AppConfig());
                        }
                        new Process { StartInfo = new ProcessStartInfo(path) { UseShellExecute = true } }.Start();
                        return;
                    }
                    if (i + 1 < args.Length && args[i + 1] == "show")
                    {
                        var cfg = configService.LoadConfig();
                        Console.WriteLine($"Configuration file: {configService.GetConfigPath()}");
                        Console.WriteLine(JsonSerializer.Serialize(cfg, new JsonSerializerOptions { WriteIndented = true }));
                        return;
                    }
                }
            }

            Console.WriteLine("Azure Relay Hybrid Connection Utility (.NET Core)");
            Console.WriteLine("============================================");
            Console.WriteLine();

            var appConfig = configService.LoadConfig();
            var myTunnels = appConfig.Tunnels.Where(t => t.Type == "dotnet-core").ToList();

            // Filter by ID if provided
            if (!string.IsNullOrEmpty(targetTunnelId))
            {
                myTunnels = myTunnels.Where(t => t.Id == targetTunnelId).ToList();
                if (myTunnels.Count == 0)
                {
                    Console.WriteLine($"❌ Tunnel with ID '{targetTunnelId}' not found or is not of type 'dotnet-core'.");
                    return;
                }
            }

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
                    Console.WriteLine("Exiting. Use 'dotnet run -- config edit' to configure manually.");
                    return;
                }
            }
            
            if (myTunnels.Count == 0)
            {
                Console.WriteLine("No tunnels of type 'dotnet-core' found in configuration.");
                Console.WriteLine($"Found {appConfig.Tunnels.Count} total tunnels.");
                return;
            }

            Console.WriteLine($"Found {myTunnels.Count} enabled relay configuration(s):");
            Console.WriteLine();

            // Map to legacy RelayConfig for internal use
            var relayConfigs = new List<RelayConfig>();
            foreach (var t in myTunnels)
            {
                try
                {
                    var key = configService.DecryptKey(t.EncryptedKey);
                    relayConfigs.Add(new RelayConfig
                    {
                        RelayNamespace = t.RelayNamespace,
                        RelayName = t.HybridConnectionName,
                        PolicyName = t.KeyName,
                        PolicyKey = key,
                        TargetServiceAddress = $"http://{t.TargetHost}:{t.TargetPort}/",
                        IsEnabled = true,
                        DynamicResourceCreation = t.DynamicResourceCreation.GetValueOrDefault(false),
                        ResourceGroupName = t.ResourceGroupName,
                        Description = t.Description,
                        RequiresClientAuthorization = t.RequiresClientAuthorization.GetValueOrDefault(true),
                        EnableWebSocketSupport = t.EnableWebSocketSupport.GetValueOrDefault(true),
                        TargetWebSocketAddress = t.TargetWebSocketAddress
                    });
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"❌ Failed to decrypt key for tunnel '{t.Name}': {ex.Message}");
                }
            }

            // Start services
            // Need to zip myTunnels with relayConfigs to keep access to original tunnel config for AzureManagement
            var tunnelsAndConfigs = myTunnels.Zip(relayConfigs, (tunnel, config) => new { Tunnel = tunnel, Config = config }).ToList();

            var dispatcherServices = tunnelsAndConfigs.Select(pair =>
            {
                var cfg = pair.Config;
                var tunnel = pair.Tunnel;
                RelayResourceManager resourceManager = null;

                // Initialize ResourceManager if dynamic resource creation is enabled
                if (cfg.DynamicResourceCreation)
                {
                    // Prioritize the tunnel-specific Azure Management configuration if available
                    var azureConfig = tunnel.AzureManagement ?? appConfig.AzureManagement;
                    
                    if (azureConfig == null)
                    {
                        Console.WriteLine($"⚠️ DynamicResourceCreation is enabled for '{cfg.RelayName}', but AzureManagement configuration is missing.");
                        // We can't create resources without config, but we can try to connect anyway if it exists
                    }
                    else
                    {
                        try
                        {
                            resourceManager = new RelayResourceManager(azureConfig);
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"❌ Failed to initialize Azure Relay Resource Manager: {ex.Message}");
                        }
                    }
                }

                return new DispatcherService(cfg, resourceManager);
            }).ToList();

            var openTasks = dispatcherServices.Select(ds => ds.OpenAsync(CancellationToken.None)).ToList();
            await Task.WhenAll(openTasks);

            using var exitEvent = new ManualResetEvent(false);
            
            Console.CancelKeyPress += (sender, e) =>
            {
                e.Cancel = true;
                exitEvent.Set();
            };

            _consoleCtrlHandler = ctrlType =>
            {
                if (ctrlType == CTRL_CLOSE_EVENT || ctrlType == CTRL_LOGOFF_EVENT || ctrlType == CTRL_SHUTDOWN_EVENT)
                {
                    exitEvent.Set();
                    return true;
                }
                return false;
            };
            SetConsoleCtrlHandler(_consoleCtrlHandler, true);

            Console.WriteLine("Press Enter or Ctrl+C to stop...");
            var readLineTask = Task.Run(() => Console.ReadLine());
            var exitTask = Task.Run(() => exitEvent.WaitOne());

            var completedTask = await Task.WhenAny(readLineTask, exitTask);

            if (completedTask == exitTask && !readLineTask.IsCompleted)
            {
                try { if (!Console.IsInputRedirected) Console.WriteLine(); } catch { }
                await readLineTask;
            }
            else if (completedTask == readLineTask && !exitTask.IsCompleted)
            {
                exitEvent.Set();
                await exitTask;
            }

            Console.WriteLine("Shutting down...");
            
            using var shutdownCts = new CancellationTokenSource(TimeSpan.FromSeconds(DEFAULT_SHUTDOWN_TIMEOUT_SECONDS));
            
            try
            {
                var closeTasks = dispatcherServices.Select(ds => ds.CloseAsync(shutdownCts.Token)).ToList();
                await Task.WhenAll(closeTasks);
                Console.WriteLine("✓ All resources cleaned up successfully");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"⚠️ Error during cleanup: {ex.Message}");
            }
        }

        private static TunnelConfig InteractiveSetup(ConfigService configService)
        {
            try
            {
                Console.Write("Tunnel Name (e.g. Production DB): ");
                var name = Console.ReadLine();

                Console.Write("Azure Relay Namespace: ");
                var ns = Console.ReadLine();

                Console.Write("Hybrid Connection Name: ");
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
                    Type = "dotnet-core",
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
    }
}
