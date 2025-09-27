using System;
using System.Threading.Tasks;
using Azure;
using Azure.Identity;
using Azure.ResourceManager;
using Azure.ResourceManager.Relay;
using Azure.ResourceManager.Relay.Models;

namespace RelayTunnelUsingHybridConnection
{
    public class RelayResourceManager
    {
        private readonly ArmClient _armClient;
        private readonly string _subscriptionId;

        public RelayResourceManager(AzureManagementConfig config)
        {
            _subscriptionId = config.SubscriptionId;

            // Choose authentication method
            if (config.UseDefaultAzureCredential)
            {
                _armClient = new ArmClient(new DefaultAzureCredential());
            }
            else
            {
                var clientSecretCredential = new ClientSecretCredential(
                    config.TenantId,
                    config.ClientId,
                    config.ClientSecret);
                _armClient = new ArmClient(clientSecretCredential);
            }
        }

        public async Task<bool> CreateHybridConnectionAsync(RelayConfig config)
        {
            try
            {
                var namespaceShortName = GetNamespaceFromFqdn(config.RelayNamespace);
                Console.WriteLine($"Creating Hybrid Connection '{config.RelayName}' in namespace '{namespaceShortName}'...");

                var subscription = _armClient.GetSubscriptionResource(Azure.Core.ResourceIdentifier.Root.AppendChildResource("subscriptions", _subscriptionId));
                var subscriptionData = await subscription.GetAsync();
                var resourceGroup = await subscriptionData.Value.GetResourceGroupAsync(config.ResourceGroupName);
                var relayNamespace = await resourceGroup.Value.GetRelayNamespaceAsync(namespaceShortName);
                
                var hybridConnectionData = new RelayHybridConnectionData()
                {
                    IsClientAuthorizationRequired = config.RequiresClientAuthorization,
                    UserMetadata = config.Description
                };

                var hybridConnectionCollection = relayNamespace.Value.GetRelayHybridConnections();
                var operation = await hybridConnectionCollection.CreateOrUpdateAsync(
                    WaitUntil.Completed,
                    config.RelayName,
                    hybridConnectionData);

                Console.WriteLine($"✓ Hybrid Connection '{config.RelayName}' created successfully");
                return operation.HasCompleted;
            }
            catch (RequestFailedException ex) when (ex.Status == 409)
            {
                Console.WriteLine($"⚠ Hybrid Connection '{config.RelayName}' already exists");
                return true; // Already exists, consider it successful
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error creating Hybrid Connection '{config.RelayName}': {ex.Message}");
                return false;
            }
        }

        public async Task<bool> DeleteHybridConnectionAsync(RelayConfig config)
        {
            try
            {
                Console.WriteLine($"Deleting Hybrid Connection '{config.RelayName}' from namespace '{GetNamespaceFromFqdn(config.RelayNamespace)}'...");

                var subscription = _armClient.GetSubscriptionResource(Azure.Core.ResourceIdentifier.Root.AppendChildResource("subscriptions", _subscriptionId));
                var subscriptionData = await subscription.GetAsync();
                var resourceGroup = await subscriptionData.Value.GetResourceGroupAsync(config.ResourceGroupName);
                var relayNamespace = await resourceGroup.Value.GetRelayNamespaceAsync(GetNamespaceFromFqdn(config.RelayNamespace));
                
                var hybridConnection = await relayNamespace.Value.GetRelayHybridConnectionAsync(config.RelayName);
                
                if (hybridConnection.Value != null)
                {
                    var operation = await hybridConnection.Value.DeleteAsync(WaitUntil.Completed);
                    Console.WriteLine($"✓ Hybrid Connection '{config.RelayName}' deleted successfully");
                    return operation.HasCompleted;
                }
                else
                {
                    Console.WriteLine($"⚠ Hybrid Connection '{config.RelayName}' not found (may already be deleted)");
                    return true;
                }
            }
            catch (RequestFailedException ex) when (ex.Status == 404)
            {
                Console.WriteLine($"⚠ Hybrid Connection '{config.RelayName}' not found (may already be deleted)");
                return true; // Not found, consider deletion successful
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error deleting Hybrid Connection '{config.RelayName}': {ex.Message}");
                return false;
            }
        }

        public async Task<bool> HybridConnectionExistsAsync(RelayConfig config)
        {
            try
            {
                var subscription = _armClient.GetSubscriptionResource(Azure.Core.ResourceIdentifier.Root.AppendChildResource("subscriptions", _subscriptionId));
                var subscriptionData = await subscription.GetAsync();
                var resourceGroup = await subscriptionData.Value.GetResourceGroupAsync(config.ResourceGroupName);
                var relayNamespace = await resourceGroup.Value.GetRelayNamespaceAsync(GetNamespaceFromFqdn(config.RelayNamespace));
                
                var hybridConnection = await relayNamespace.Value.GetRelayHybridConnectionAsync(config.RelayName);
                return hybridConnection.Value != null;
            }
            catch (RequestFailedException ex) when (ex.Status == 404)
            {
                return false;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"⚠ Error checking Hybrid Connection existence: {ex.Message}");
                return false;
            }
        }

        private static string GetNamespaceFromFqdn(string fqdn)
        {
            // Convert "common-relay.servicebus.windows.net" to "common-relay"
            if (fqdn.Contains(".servicebus.windows.net"))
            {
                return fqdn.Replace(".servicebus.windows.net", "");
            }
            return fqdn;
        }
    }
}
