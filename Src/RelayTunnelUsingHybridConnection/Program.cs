using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using System.Linq;
using System.Runtime.InteropServices;

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

        // Keep delegate alive to prevent garbage collection while native code holds reference
        private static ConsoleCtrlDelegate _consoleCtrlHandler;

        public static IConfiguration Configuration { get; set; }

        public static async Task Main(string[] args)
        {
            Console.WriteLine("Azure Relay Hybrid Connection Utility (.NET Core)");
            Console.WriteLine("============================================");
            Console.WriteLine();

            var builder = new ConfigurationBuilder()
                .SetBasePath(AppDomain.CurrentDomain.BaseDirectory)
                .AddJsonFile("appsettings.json", false, true)
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
                    Console.WriteLine($"  \u263A {cfg.RelayName} --> {cfg.TargetServiceAddress} (Dynamic: {cfg.DynamicResourceCreation})");
                }
                Console.WriteLine();

                var dispatcherServices = enabledRelayConfigs.Select(cfg =>
                {
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

                // Ensure both tasks are completed before proceeding to shutdown
                if (completedTask == exitTask && !readLineTask.IsCompleted)
                {
                    // Try to signal Console.ReadLine to complete by sending a newline
                    try
                    {
                        if (!Console.IsInputRedirected)
                        {
                            Console.WriteLine();
                        }
                    }
                    catch { /* Ignore any errors */ }
                    await readLineTask;
                }
                else if (completedTask == readLineTask && !exitTask.IsCompleted)
                {
                    exitEvent.Set();
                    await exitTask;
                }
                Console.WriteLine("Shutting down and cleaning up resources...");
                
                // Get configurable shutdown timeout or use default
                var shutdownTimeoutSeconds = Configuration.GetValue<int?>("ShutdownTimeoutSeconds") ?? DEFAULT_SHUTDOWN_TIMEOUT_SECONDS;
                using var shutdownCts = new CancellationTokenSource(TimeSpan.FromSeconds(shutdownTimeoutSeconds));
                
                try
                {
                    var closeTasks = dispatcherServices.Select(ds => ds.CloseAsync(shutdownCts.Token)).ToList();
                    await Task.WhenAll(closeTasks);
                    Console.WriteLine("✓ All resources cleaned up successfully");
                }
                catch (OperationCanceledException)
                {
                    Console.WriteLine($"⚠️ Shutdown timeout ({shutdownTimeoutSeconds}s) exceeded. Some resources may not have cleaned up properly.");
                }
                catch (Exception cleanupEx)
                {
                    Console.WriteLine($"⚠️ Error during cleanup: {cleanupEx.Message}");
                }
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