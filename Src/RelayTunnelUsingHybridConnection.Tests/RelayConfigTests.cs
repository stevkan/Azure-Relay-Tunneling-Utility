using FluentAssertions;
using Xunit;

namespace RelayTunnelUsingHybridConnection.Tests
{
    public class RelayConfigTests
    {
        [Fact]
        public void RelayName_ShouldConvertToLowercase_WhenContainsUppercaseLetters()
        {
            var config = new RelayConfig
            {
                RelayName = "MyRelayName"
            };

            config.RelayName.Should().Be("myrelayname");
        }

        [Fact]
        public void RelayName_ShouldHandleNullValue()
        {
            var config = new RelayConfig
            {
                RelayName = null
            };

            config.RelayName.Should().BeNull();
        }

        [Fact]
        public void RelayName_ShouldHandleEmptyString()
        {
            var config = new RelayConfig
            {
                RelayName = ""
            };

            config.RelayName.Should().Be("");
        }

        [Theory]
        [InlineData("test-relay", "test-relay")]
        [InlineData("TEST-RELAY", "test-relay")]
        [InlineData("Test-Relay-123", "test-relay-123")]
        [InlineData("relay_with_underscores", "relay_with_underscores")]
        public void RelayName_ShouldConvertCorrectly(string input, string expected)
        {
            var config = new RelayConfig
            {
                RelayName = input
            };

            config.RelayName.Should().Be(expected);
        }

        [Fact]
        public void DefaultValues_ShouldBeSetCorrectly()
        {
            var config = new RelayConfig();

            config.EnableWebSocketSupport.Should().BeTrue();
            config.DynamicResourceCreation.Should().BeFalse();
            config.Description.Should().Be("Dynamically created hybrid connection");
            config.RequiresClientAuthorization.Should().BeTrue();
        }

        [Fact]
        public void Properties_ShouldBeSettable()
        {
            var config = new RelayConfig
            {
                IsEnabled = true,
                RelayNamespace = "test-namespace",
                RelayName = "test-relay",
                PolicyName = "RootManageSharedAccessKey",
                PolicyKey = "test-key",
                TargetServiceAddress = "http://localhost:8080",
                EnableWebSocketSupport = false,
                TargetWebSocketAddress = "ws://localhost:8080",
                DynamicResourceCreation = true,
                ResourceGroupName = "test-rg",
                Description = "Test relay",
                RequiresClientAuthorization = false
            };

            config.IsEnabled.Should().BeTrue();
            config.RelayNamespace.Should().Be("test-namespace");
            config.RelayName.Should().Be("test-relay");
            config.PolicyName.Should().Be("RootManageSharedAccessKey");
            config.PolicyKey.Should().Be("test-key");
            config.TargetServiceAddress.Should().Be("http://localhost:8080");
            config.EnableWebSocketSupport.Should().BeFalse();
            config.TargetWebSocketAddress.Should().Be("ws://localhost:8080");
            config.DynamicResourceCreation.Should().BeTrue();
            config.ResourceGroupName.Should().Be("test-rg");
            config.Description.Should().Be("Test relay");
            config.RequiresClientAuthorization.Should().BeFalse();
        }
    }
}
