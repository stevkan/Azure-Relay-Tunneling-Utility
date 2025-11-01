using FluentAssertions;
using Xunit;

namespace RelayTunnelUsingHybridConnection.Tests
{
    public class RelayResourceManagerTests
    {
        [Fact]
        public void Constructor_ShouldAcceptAzureManagementConfig_WithDefaultCredential()
        {
            var config = new AzureManagementConfig
            {
                SubscriptionId = "test-subscription-id",
                UseDefaultAzureCredential = true
            };

            var action = () => new RelayResourceManager(config);

            action.Should().NotThrow();
        }

        [Fact]
        public void Constructor_ShouldAcceptAzureManagementConfig_WithClientSecretCredential()
        {
            var config = new AzureManagementConfig
            {
                SubscriptionId = "test-subscription-id",
                TenantId = "test-tenant-id",
                ClientId = "test-client-id",
                ClientSecret = "test-client-secret",
                UseDefaultAzureCredential = false
            };

            var action = () => new RelayResourceManager(config);

            action.Should().NotThrow();
        }

        [Fact]
        public void RelayResourceManager_ShouldBeCreatedSuccessfully()
        {
            var config = new AzureManagementConfig
            {
                SubscriptionId = "test-subscription-id",
                UseDefaultAzureCredential = true
            };

            var manager = new RelayResourceManager(config);
            
            manager.Should().NotBeNull();
        }
    }
}
