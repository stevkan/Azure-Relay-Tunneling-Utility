# RelayTunnelUsingHybridConnection Tests

This project contains unit tests for the RelayTunnelUsingHybridConnection application.

## Test Framework

- **xUnit**: The primary test framework
- **FluentAssertions**: For readable and expressive assertions
- **Moq**: For creating mock objects (available for future use)

## Running Tests

### Important: Avoiding Build Conflicts

If the RelayTunnelUsingHybridConnection application is currently running, the build will fail with file locking errors. You have two options:

**Option 1: Stop the application first, then run tests**
```bash
dotnet test D:\personal\AzureRelayTunnelingUtility\Src\AzureRelayTunnelingUtility.sln
```

**Option 2: Build first, then run tests without rebuilding**
```bash
# Build the solution (stop the application first if running)
dotnet build D:\personal\AzureRelayTunnelingUtility\Src\AzureRelayTunnelingUtility.sln

# Run tests without building (can be done while application is running)
dotnet test D:\personal\AzureRelayTunnelingUtility\Src\AzureRelayTunnelingUtility.sln --no-build
```

### Run tests with verbose output
```bash
dotnet test --verbosity detailed --no-build
```

### Run only the test project
```bash
dotnet test D:\personal\AzureRelayTunnelingUtility\Src\RelayTunnelUsingHybridConnection.Tests\RelayTunnelUsingHybridConnection.Tests.csproj --no-build
```

## Test Coverage

### RelayConfigTests
- Tests for relay name conversion to lowercase
- Tests for handling null and empty values
- Tests for default configuration values
- Tests for property setters

### AzureManagementConfigTests
- Tests for default values
- Tests for property setters and null handling

### RelayResourceManagerTests
- Tests for constructor with different credential types
- Tests for resource manager creation

### ExtensionsTests
- Tests for `EnsureEndsWith` string extension method
- Tests for null input handling

### WebSocketForwarderTests
- Tests for URI validation and creation
- Tests for secure and insecure WebSocket URIs
- Tests for URI scheme validation

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

- Some components like `DispatcherService` and `WebSocketForwarder` are marked as `internal` and may require additional design patterns (e.g., interfaces, dependency injection) for comprehensive unit testing
- Integration tests for Azure Relay operations would require live Azure resources and should be handled separately
