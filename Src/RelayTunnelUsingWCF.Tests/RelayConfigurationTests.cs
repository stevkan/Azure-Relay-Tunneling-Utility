using FluentAssertions;
using Xunit;

namespace RelayTunnelUsingWCF.Tests
{
    public class RelayConfigurationTests
    {
        [Fact]
        public void DefaultValues_ShouldBeSetCorrectly()
        {
            var config = new RelayConfiguration();

            config.ServiceDiscoveryMode.Should().Be("Private");
            config.EnableDetailedLogging.Should().BeTrue();
            config.IsEnabled.Should().BeTrue();
        }

        [Fact]
        public void Properties_ShouldBeSettable()
        {
            var config = new RelayConfiguration
            {
                RelayNamespace = "test-namespace",
                RelayName = "test-relay",
                PolicyName = "test-policy",
                PolicyKey = "test-key",
                TargetServiceAddress = "http://localhost:8080",
                ServiceDiscoveryMode = "Public",
                EnableDetailedLogging = false,
                IsEnabled = false
            };

            config.RelayNamespace.Should().Be("test-namespace");
            config.RelayName.Should().Be("test-relay");
            config.PolicyName.Should().Be("test-policy");
            config.PolicyKey.Should().Be("test-key");
            config.TargetServiceAddress.Should().Be("http://localhost:8080");
            config.ServiceDiscoveryMode.Should().Be("Public");
            config.EnableDetailedLogging.Should().BeFalse();
            config.IsEnabled.Should().BeFalse();
        }

        [Fact]
        public void Properties_ShouldHandleNullValues()
        {
            var config = new RelayConfiguration
            {
                RelayNamespace = null,
                RelayName = null,
                PolicyName = null,
                PolicyKey = null,
                TargetServiceAddress = null,
                ServiceDiscoveryMode = null
            };

            config.RelayNamespace.Should().BeNull();
            config.RelayName.Should().BeNull();
            config.PolicyName.Should().BeNull();
            config.PolicyKey.Should().BeNull();
            config.TargetServiceAddress.Should().BeNull();
            config.ServiceDiscoveryMode.Should().BeNull();
        }

        [Theory]
        [InlineData("Private")]
        [InlineData("Public")]
        [InlineData("AutoDetect")]
        public void ServiceDiscoveryMode_ShouldAcceptValidValues(string mode)
        {
            var config = new RelayConfiguration
            {
                ServiceDiscoveryMode = mode
            };

            config.ServiceDiscoveryMode.Should().Be(mode);
        }
    }
}
