using FluentAssertions;
using System;
using Xunit;

namespace RelayTunnelUsingHybridConnection.Tests
{
    public class WebSocketForwarderTests
    {
        [Theory]
        [InlineData("ws://localhost:8080")]
        [InlineData("wss://example.com:443")]
        [InlineData("ws://192.168.1.1:9000")]
        [InlineData("wss://api.example.com/websocket")]
        public void UriCreation_ShouldSucceed_ForValidWebSocketUris(string uriString)
        {
            var action = () => new Uri(uriString);

            action.Should().NotThrow();
            var uri = new Uri(uriString);
            uri.Scheme.Should().Match(s => s == "ws" || s == "wss");
        }

        [Fact]
        public void WebSocketUri_ShouldHaveCorrectScheme_ForSecureConnection()
        {
            var uri = new Uri("wss://example.com:443");

            uri.Scheme.Should().Be("wss");
            uri.Host.Should().Be("example.com");
            uri.Port.Should().Be(443);
        }

        [Fact]
        public void WebSocketUri_ShouldHaveCorrectScheme_ForInsecureConnection()
        {
            var uri = new Uri("ws://localhost:8080");

            uri.Scheme.Should().Be("ws");
            uri.Host.Should().Be("localhost");
            uri.Port.Should().Be(8080);
        }
    }
}
