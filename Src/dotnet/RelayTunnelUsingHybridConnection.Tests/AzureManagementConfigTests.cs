using FluentAssertions;
using Xunit;

namespace RelayTunnelUsingHybridConnection.Tests
{
    public class AzureManagementConfigTests
    {
        [Fact]
        public void DefaultValues_ShouldBeSetCorrectly()
        {
            var config = new AzureManagementConfig();

            config.UseDefaultAzureCredential.Should().BeTrue();
        }

        [Fact]
        public void Properties_ShouldBeSettable()
        {
            var config = new AzureManagementConfig
            {
                SubscriptionId = "test-subscription-id",
                TenantId = "test-tenant-id",
                ClientId = "test-client-id",
                ClientSecret = "test-client-secret",
                UseDefaultAzureCredential = false
            };

            config.SubscriptionId.Should().Be("test-subscription-id");
            config.TenantId.Should().Be("test-tenant-id");
            config.ClientId.Should().Be("test-client-id");
            config.ClientSecret.Should().Be("test-client-secret");
            config.UseDefaultAzureCredential.Should().BeFalse();
        }

        [Fact]
        public void Properties_ShouldHandleNullValues()
        {
            var config = new AzureManagementConfig
            {
                SubscriptionId = null,
                TenantId = null,
                ClientId = null,
                ClientSecret = null
            };

            config.SubscriptionId.Should().BeNull();
            config.TenantId.Should().BeNull();
            config.ClientId.Should().BeNull();
            config.ClientSecret.Should().BeNull();
        }
    }
}
