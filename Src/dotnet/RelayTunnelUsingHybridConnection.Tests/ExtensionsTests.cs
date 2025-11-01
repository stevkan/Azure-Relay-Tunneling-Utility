using FluentAssertions;
using RelayTunnelUsingHybridConnection.Extensions;
using Xunit;

namespace RelayTunnelUsingHybridConnection.Tests
{
    public class ExtensionsTests
    {
        [Theory]
        [InlineData("path", "path/")]
        [InlineData("path/", "path/")]
        [InlineData("", "/")]
        [InlineData("/", "/")]
        [InlineData("path/to/resource", "path/to/resource/")]
        [InlineData("path/to/resource/", "path/to/resource/")]
        public void EnsureEndsWith_ShouldAppendSlashIfMissing(string input, string expected)
        {
            var result = input.EnsureEndsWith("/");

            result.Should().Be(expected);
        }

        [Fact]
        public void EnsureEndsWith_ShouldHandleNullInput()
        {
            string? input = null;
            var result = input.EnsureEndsWith("/");

            result.Should().Be("/");
        }

        [Theory]
        [InlineData("test", ".", "test.")]
        [InlineData("test.", ".", "test.")]
        [InlineData("file", ".txt", "file.txt")]
        [InlineData("file.txt", ".txt", "file.txt")]
        public void EnsureEndsWith_ShouldWorkWithDifferentSuffixes(string input, string suffix, string expected)
        {
            var result = input.EnsureEndsWith(suffix);

            result.Should().Be(expected);
        }
    }
}
