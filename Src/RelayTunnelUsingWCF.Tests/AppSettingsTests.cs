using FluentAssertions;
using Xunit;

namespace RelayTunnelUsingWCF.Tests
{
    public class AppSettingsTests
    {
        [Fact]
        public void DefaultConstructor_ShouldInitializeEmptyRelaysList()
        {
            var appSettings = new AppSettings();

            appSettings.Relays.Should().NotBeNull();
            appSettings.Relays.Should().BeEmpty();
        }

        [Fact]
        public void Relays_ShouldAcceptMultipleConfigurations()
        {
            var appSettings = new AppSettings
            {
                Relays = new List<RelayConfiguration>
                {
                    new RelayConfiguration { RelayName = "relay1" },
                    new RelayConfiguration { RelayName = "relay2" },
                    new RelayConfiguration { RelayName = "relay3" }
                }
            };

            appSettings.Relays.Should().HaveCount(3);
            appSettings.Relays[0].RelayName.Should().Be("relay1");
            appSettings.Relays[1].RelayName.Should().Be("relay2");
            appSettings.Relays[2].RelayName.Should().Be("relay3");
        }

        [Fact]
        public void Relays_ShouldBeSettable()
        {
            var appSettings = new AppSettings();
            var newRelays = new List<RelayConfiguration>
            {
                new RelayConfiguration { RelayName = "new-relay" }
            };

            appSettings.Relays = newRelays;

            appSettings.Relays.Should().BeSameAs(newRelays);
            appSettings.Relays.Should().HaveCount(1);
        }
    }
}
