# Contributing to AlgoTrendy v2.6

Thank you for your interest in contributing to AlgoTrendy! This document provides guidelines and instructions for contributing to the project.

## Table of Contents

- [Code of Conduct](#code-of-conduct)
- [Getting Started](#getting-started)
- [Development Workflow](#development-workflow)
- [Coding Standards](#coding-standards)
- [Testing Requirements](#testing-requirements)
- [Pull Request Process](#pull-request-process)
- [Commit Message Guidelines](#commit-message-guidelines)
- [Project Structure](#project-structure)

## Code of Conduct

- Be respectful and inclusive
- Focus on constructive feedback
- Help others learn and grow
- Maintain professional communication
- Report any unacceptable behavior to the maintainers

## Getting Started

### Prerequisites

- .NET 8.0 SDK or later
- Docker and Docker Compose
- Git
- Your favorite IDE (Visual Studio 2022, VS Code, or Rider)

### Initial Setup

1. **Fork the repository**
   ```bash
   # Fork on GitHub, then clone your fork
   git clone https://github.com/YOUR_USERNAME/algotrendy-v2.6.git
   cd algotrendy-v2.6
   ```

2. **Add upstream remote**
   ```bash
   git remote add upstream https://github.com/KenyBoi/algotrendy-v2.6.git
   ```

3. **Install dependencies**
   ```bash
   dotnet restore
   ```

4. **Setup credentials**
   ```bash
   # Run the interactive setup script
   ./quick_setup_credentials.sh

   # Or manually configure using user secrets
   dotnet user-secrets init --project backend/AlgoTrendy.API
   dotnet user-secrets set "QuantConnect:UserId" "YOUR_USER_ID" --project backend/AlgoTrendy.API
   dotnet user-secrets set "QuantConnect:ApiToken" "YOUR_API_TOKEN" --project backend/AlgoTrendy.API
   ```

5. **Start development environment**
   ```bash
   docker-compose up -d
   ```

6. **Run the application**
   ```bash
   cd backend/AlgoTrendy.API
   dotnet run
   ```

## Development Workflow

### Creating a Feature Branch

```bash
# Update your main branch
git checkout main
git pull upstream main

# Create a feature branch
git checkout -b feature/your-feature-name
```

### Branch Naming Convention

- `feature/` - New features (e.g., `feature/add-binance-websocket`)
- `fix/` - Bug fixes (e.g., `fix/order-execution-bug`)
- `docs/` - Documentation updates (e.g., `docs/update-readme`)
- `refactor/` - Code refactoring (e.g., `refactor/broker-base-class`)
- `test/` - Test additions/updates (e.g., `test/add-bybit-integration-tests`)
- `chore/` - Maintenance tasks (e.g., `chore/update-dependencies`)

### Making Changes

1. **Write code following our standards** (see [Coding Standards](#coding-standards))
2. **Add or update tests** for your changes
3. **Update documentation** if needed
4. **Run tests locally** before committing
5. **Commit your changes** with clear messages

### Running Tests

```bash
# Run all tests
dotnet test

# Run specific test project
dotnet test backend/AlgoTrendy.Tests/AlgoTrendy.Tests.csproj

# Run tests with coverage
dotnet test /p:CollectCoverage=true /p:CoverletOutputFormat=opencover

# Run specific test
dotnet test --filter "FullyQualifiedName~AlgoTrendy.Tests.BrokerTests.BybitBrokerTests"
```

### Building the Project

```bash
# Build entire solution
dotnet build

# Build specific project
dotnet build backend/AlgoTrendy.API

# Build in Release mode
dotnet build -c Release
```

## Coding Standards

### C# Style Guidelines

We follow the [Microsoft C# Coding Conventions](https://docs.microsoft.com/en-us/dotnet/csharp/fundamentals/coding-style/coding-conventions) with some additions:

#### Naming Conventions

```csharp
// Classes, interfaces, methods: PascalCase
public class BrokerService { }
public interface IBroker { }
public async Task<Order> PlaceOrderAsync() { }

// Private fields: _camelCase with underscore
private readonly ILogger _logger;

// Local variables, parameters: camelCase
var orderResult = await broker.PlaceOrderAsync(symbol, quantity);

// Constants: PascalCase
public const int MaxRetryAttempts = 3;

// Async methods: Always suffix with "Async"
public async Task<MarketData> FetchMarketDataAsync() { }
```

#### Code Organization

```csharp
public class OrderService : IOrderService
{
    // 1. Constants
    private const int DefaultTimeout = 30000;

    // 2. Fields
    private readonly ILogger<OrderService> _logger;
    private readonly IBroker _broker;

    // 3. Constructor
    public OrderService(ILogger<OrderService> logger, IBroker broker)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _broker = broker ?? throw new ArgumentNullException(nameof(broker));
    }

    // 4. Public methods
    public async Task<OrderResult> PlaceOrderAsync(OrderRequest request)
    {
        ValidateRequest(request);
        return await _broker.SubmitOrderAsync(request);
    }

    // 5. Private methods
    private void ValidateRequest(OrderRequest request)
    {
        if (request == null)
            throw new ArgumentNullException(nameof(request));
    }
}
```

#### Error Handling

```csharp
// DO: Use specific exceptions
if (quantity <= 0)
    throw new ArgumentException("Quantity must be positive", nameof(quantity));

// DO: Log errors with context
_logger.LogError(ex, "Failed to place order for {Symbol} at {Price}", symbol, price);

// DO: Use try-catch for expected errors
try
{
    await broker.PlaceOrderAsync(order);
}
catch (BrokerException ex)
{
    _logger.LogWarning(ex, "Broker rejected order: {Reason}", ex.Message);
    return OrderResult.Failed(ex.Message);
}

// DON'T: Catch generic exceptions without re-throwing
// DON'T: Swallow exceptions silently
```

#### Async/Await Best Practices

```csharp
// DO: Use ConfigureAwait(false) in library code
var data = await httpClient.GetAsync(url).ConfigureAwait(false);

// DO: Use async all the way
public async Task<Result> ProcessAsync()
{
    var data = await FetchDataAsync();
    return await SaveDataAsync(data);
}

// DON'T: Use async void (except for event handlers)
// DON'T: Block on async code with .Result or .Wait()
```

### Documentation

```csharp
/// <summary>
/// Places a market order for the specified symbol and quantity.
/// </summary>
/// <param name="symbol">The trading symbol (e.g., "BTCUSDT")</param>
/// <param name="quantity">The order quantity</param>
/// <param name="side">The order side (Buy or Sell)</param>
/// <returns>The order result including order ID and status</returns>
/// <exception cref="ArgumentException">Thrown when quantity is invalid</exception>
/// <exception cref="BrokerException">Thrown when the broker rejects the order</exception>
public async Task<OrderResult> PlaceMarketOrderAsync(
    string symbol,
    decimal quantity,
    OrderSide side)
{
    // Implementation
}
```

## Testing Requirements

### Test Coverage

- **Minimum coverage:** 75% (target: 85%+)
- All new features must include tests
- Bug fixes must include regression tests

### Test Structure

```csharp
[Fact]
public async Task PlaceOrderAsync_WithValidRequest_ReturnsSuccess()
{
    // Arrange
    var broker = new MockBroker();
    var service = new OrderService(broker);
    var request = new OrderRequest
    {
        Symbol = "BTCUSDT",
        Quantity = 1.0m
    };

    // Act
    var result = await service.PlaceOrderAsync(request);

    // Assert
    Assert.True(result.IsSuccess);
    Assert.NotNull(result.OrderId);
}

[Theory]
[InlineData(0)]
[InlineData(-1)]
public async Task PlaceOrderAsync_WithInvalidQuantity_ThrowsException(decimal quantity)
{
    // Arrange
    var service = new OrderService(new MockBroker());

    // Act & Assert
    await Assert.ThrowsAsync<ArgumentException>(
        async () => await service.PlaceOrderAsync("BTCUSDT", quantity)
    );
}
```

### Integration Tests

```csharp
[Collection("Integration")]
public class BybitIntegrationTests : IAsyncLifetime
{
    private readonly ITestOutputHelper _output;
    private BybitBroker _broker;

    public BybitIntegrationTests(ITestOutputHelper output)
    {
        _output = output;
    }

    public async Task InitializeAsync()
    {
        _broker = new BybitBroker(/* testnet credentials */);
        await _broker.ConnectAsync();
    }

    [Fact]
    public async Task GetBalance_ReturnsValidData()
    {
        var balance = await _broker.GetBalanceAsync();
        Assert.NotNull(balance);
        _output.WriteLine($"Balance: {balance.Total}");
    }

    public async Task DisposeAsync()
    {
        await _broker.DisconnectAsync();
    }
}
```

## Pull Request Process

### Before Submitting

- [ ] Code follows style guidelines
- [ ] All tests pass locally
- [ ] Test coverage is maintained or improved
- [ ] Documentation is updated
- [ ] Commit messages follow conventions
- [ ] Branch is up to date with main

### Submitting a PR

1. **Push your changes**
   ```bash
   git push origin feature/your-feature-name
   ```

2. **Create Pull Request on GitHub**
   - Use a clear, descriptive title
   - Reference related issues (e.g., "Fixes #123")
   - Fill out the PR template completely
   - Add screenshots/GIFs for UI changes
   - Request review from maintainers

3. **PR Title Format**
   ```
   feat: add WebSocket support for Binance
   fix: resolve order execution timeout issue
   docs: update deployment guide
   refactor: simplify broker base class
   test: add integration tests for Bybit
   chore: update dependencies
   ```

### PR Template

```markdown
## Description
Brief description of what this PR does

## Type of Change
- [ ] Bug fix
- [ ] New feature
- [ ] Breaking change
- [ ] Documentation update

## Related Issues
Fixes #(issue number)

## Testing
Describe how you tested your changes

## Checklist
- [ ] Tests pass locally
- [ ] Code follows style guidelines
- [ ] Documentation updated
- [ ] No breaking changes (or documented)
```

### Review Process

1. **Automated checks** must pass (builds, tests, linting)
2. **Code review** by at least one maintainer
3. **Address feedback** - make requested changes
4. **Approval** - PR will be merged when approved

## Commit Message Guidelines

We follow [Conventional Commits](https://www.conventionalcommits.org/):

### Format

```
<type>(<scope>): <subject>

<body>

<footer>
```

### Types

- `feat`: New feature
- `fix`: Bug fix
- `docs`: Documentation changes
- `style`: Code style changes (formatting, no logic change)
- `refactor`: Code refactoring
- `perf`: Performance improvements
- `test`: Adding or updating tests
- `chore`: Maintenance tasks
- `ci`: CI/CD changes

### Examples

```bash
# Simple feature
git commit -m "feat: add Binance WebSocket support"

# Bug fix with issue reference
git commit -m "fix: resolve timeout in order execution

The order execution was timing out due to incorrect
timeout configuration. Updated default timeout to 30s.

Fixes #123"

# Breaking change
git commit -m "feat!: migrate to .NET 9.0

BREAKING CHANGE: Minimum required version is now .NET 9.0"
```

## Project Structure

```
AlgoTrendy_v2.6/
├── backend/
│   ├── AlgoTrendy.API/          # Web API project
│   ├── AlgoTrendy.Core/         # Core business logic
│   ├── AlgoTrendy.Brokers/      # Broker integrations
│   ├── AlgoTrendy.Backtesting/  # Backtesting engine
│   ├── AlgoTrendy.DataChannels/ # Data providers
│   └── AlgoTrendy.Tests/        # Unit & integration tests
├── frontend/                     # React frontend
├── docs/                        # Documentation
├── scripts/                     # Utility scripts
├── docker-compose.yml           # Development environment
└── .github/                     # GitHub workflows
```

### Adding New Files

- **Controllers:** `backend/AlgoTrendy.API/Controllers/`
- **Services:** `backend/AlgoTrendy.Core/Services/`
- **Brokers:** `backend/AlgoTrendy.Brokers/Brokers/`
- **Tests:** `backend/AlgoTrendy.Tests/`
- **Documentation:** `docs/`

## Need Help?

- Check existing [Issues](https://github.com/KenyBoi/algotrendy-v2.6/issues)
- Read the [Documentation](./docs/)
- Ask questions in [Discussions](https://github.com/KenyBoi/algotrendy-v2.6/discussions)

## License

By contributing, you agree that your contributions will be licensed under the same license as the project.

---

**Thank you for contributing to AlgoTrendy!** 🚀
