# RelayTunnelUsingWCF Tests

This project contains unit tests for the RelayTunnelUsingWCF application.

## Test Framework

- **xUnit**: The primary test framework
- **FluentAssertions**: For readable and expressive assertions
- **Moq**: For creating mock objects

## Running Tests

### Run all tests
```bash
dotnet test D:\personal\AzureRelayTunnelingUtility\Src\AzureRelayTunnelingUtility.sln
```

### Run tests without rebuilding
```bash
dotnet test D:\personal\AzureRelayTunnelingUtility\Src\AzureRelayTunnelingUtility.sln --no-build
```

### Run tests with verbose output
```bash
dotnet test --verbosity detailed --no-build
```

### Run only the WCF test project
```bash
dotnet test D:\personal\AzureRelayTunnelingUtility\Src\RelayTunnelUsingWCF.Tests\RelayTunnelUsingWCF.Tests.csproj --no-build
```

## Test Coverage

### RelayConfigurationTests
- Tests for default configuration values (ServiceDiscoveryMode, EnableDetailedLogging, IsEnabled)
- Tests for property setters
- Tests for handling null values
- Tests for valid ServiceDiscoveryMode values

### AppSettingsTests
- Tests for default empty Relays list initialization
- Tests for accepting multiple relay configurations
- Tests for Relays property setter

### RelayProxyServiceFactoryTests
- Tests for constructor with RelayConfiguration
- Tests for handling null configuration

### RelayServiceInstanceProviderTests
- Tests for constructor with RelayConfiguration
- Tests for GetInstance methods returning ConfigurableRelayProxyService
- Tests for ReleaseInstance with disposable and non-disposable instances

## Test Structure

Each test class follows the Arrange-Act-Assert pattern:
1. **Arrange**: Set up test data and prerequisites
2. **Act**: Execute the code being tested
3. **Assert**: Verify the expected outcome

## Adding New Tests

When adding new tests:
1. Create a new test class in this project
2. Name the test class with the suffix `Tests` (e.g., `MyComponentTests`)
3. Use descriptive test method names that explain what is being tested
4. Follow the existing patterns and use FluentAssertions for assertions
5. Run tests locally before committing

## Notes

- Some components like WCF service behaviors require integration testing with actual WCF infrastructure
- The current tests focus on unit testing public APIs and configuration classes
- Integration tests for Azure Relay operations would require live Azure resources and should be handled separately
