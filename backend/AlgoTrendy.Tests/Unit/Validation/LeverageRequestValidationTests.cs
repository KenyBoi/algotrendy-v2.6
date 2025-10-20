using AlgoTrendy.Core.Enums;
using System.ComponentModel.DataAnnotations;
using Xunit;
using FluentAssertions;

namespace AlgoTrendy.Tests.Unit.Validation;

/// <summary>
/// Unit tests for leverage-related request validation
/// CRITICAL SECURITY: Tests 10x leverage limit enforcement
/// </summary>
public class LeverageRequestValidationTests
{
    private static List<ValidationResult> ValidateModel(object model)
    {
        var validationResults = new List<ValidationResult>();
        var validationContext = new ValidationContext(model);
        Validator.TryValidateObject(model, validationContext, validationResults, validateAllProperties: true);
        return validationResults;
    }

    #region SetLeverageRequest Tests

    [Fact]
    public void SetLeverageRequest_ValidInput_PassesValidation()
    {
        // Arrange
        var request = new AlgoTrendy.API.Controllers.SetLeverageRequest
        {
            Symbol = "BTCUSDT",
            Leverage = 5.0m,
            MarginType = MarginType.Cross
        };

        // Act
        var results = ValidateModel(request);

        // Assert
        results.Should().BeEmpty();
    }

    [Theory]
    [InlineData(0)]              // Zero leverage
    [InlineData(0.5)]            // Below minimum
    [InlineData(11)]             // Above 10x limit
    [InlineData(75)]             // Dangerous v2.5 default
    [InlineData(100)]            // Way too high
    [InlineData(-5)]             // Negative
    public void SetLeverageRequest_InvalidLeverage_FailsValidation(double invalidLeverage)
    {
        // Arrange
        var request = new AlgoTrendy.API.Controllers.SetLeverageRequest
        {
            Symbol = "BTCUSDT",
            Leverage = (decimal)invalidLeverage,
            MarginType = MarginType.Cross
        };

        // Act
        var results = ValidateModel(request);

        // Assert
        results.Should().ContainSingle(r =>
            r.MemberNames.Contains("Leverage") &&
            r.ErrorMessage!.Contains("must be between 1x and 10x"));
    }

    [Theory]
    [InlineData(1.0)]     // Minimum (safest)
    [InlineData(2.0)]     // Conservative
    [InlineData(5.0)]     // Moderate
    [InlineData(10.0)]    // Maximum allowed
    public void SetLeverageRequest_ValidLeverage_PassesValidation(double validLeverage)
    {
        // Arrange
        var request = new AlgoTrendy.API.Controllers.SetLeverageRequest
        {
            Symbol = "BTCUSDT",
            Leverage = (decimal)validLeverage,
            MarginType = MarginType.Cross
        };

        // Act
        var results = ValidateModel(request);

        // Assert
        results.Should().NotContain(r => r.MemberNames.Contains("Leverage"));
    }

    [Fact]
    public void SetLeverageRequest_V25DangerousLeverage_IsBlocked()
    {
        // Arrange - v2.5 had dangerous 75x default
        var request = new AlgoTrendy.API.Controllers.SetLeverageRequest
        {
            Symbol = "BTCUSDT",
            Leverage = 75.0m,  // BLOCKED - Would liquidate at 1.33% drop
            MarginType = MarginType.Cross
        };

        // Act
        var results = ValidateModel(request);

        // Assert
        results.Should().NotBeEmpty("75x leverage is extremely dangerous and must be blocked");
        results.Should().ContainSingle(r =>
            r.MemberNames.Contains("Leverage") &&
            r.ErrorMessage!.Contains("maximum safe limit"));
    }

    [Theory]
    [InlineData("BTC'; DROP TABLE--")]  // SQL injection
    [InlineData("BTC<script>")]          // XSS
    [InlineData("btcusdt")]              // Lowercase
    public void SetLeverageRequest_InvalidSymbol_FailsValidation(string invalidSymbol)
    {
        // Arrange
        var request = new AlgoTrendy.API.Controllers.SetLeverageRequest
        {
            Symbol = invalidSymbol,
            Leverage = 5.0m,
            MarginType = MarginType.Cross
        };

        // Act
        var results = ValidateModel(request);

        // Assert
        results.Should().NotBeEmpty();
        results.Should().Contain(r => r.MemberNames.Contains("Symbol"));
    }

    #endregion

    #region ValidateLeverageRequest Tests

    [Fact]
    public void ValidateLeverageRequest_ValidInput_PassesValidation()
    {
        // Arrange
        var request = new AlgoTrendy.API.Controllers.ValidateLeverageRequest
        {
            Symbol = "BTCUSDT",
            ProposedLeverage = 8.0m
        };

        // Act
        var results = ValidateModel(request);

        // Assert
        results.Should().BeEmpty();
    }

    [Theory]
    [InlineData(0)]              // Zero
    [InlineData(-1)]             // Negative
    [InlineData(101)]            // Above validation max
    public void ValidateLeverageRequest_InvalidProposedLeverage_FailsValidation(double invalidLeverage)
    {
        // Arrange
        var request = new AlgoTrendy.API.Controllers.ValidateLeverageRequest
        {
            Symbol = "BTCUSDT",
            ProposedLeverage = (decimal)invalidLeverage
        };

        // Act
        var results = ValidateModel(request);

        // Assert
        results.Should().ContainSingle(r =>
            r.MemberNames.Contains("ProposedLeverage"));
    }

    [Fact]
    public void ValidateLeverageRequest_AllowsHigherRangeForTesting()
    {
        // Arrange - ValidateLeverageRequest allows up to 100x for validation testing
        // (Actual SetLeverageRequest enforces 10x limit)
        var request = new AlgoTrendy.API.Controllers.ValidateLeverageRequest
        {
            Symbol = "BTCUSDT",
            ProposedLeverage = 50.0m  // Would be rejected by SetLeverageRequest
        };

        // Act
        var results = ValidateModel(request);

        // Assert - Should pass validation endpoint
        results.Should().NotContain(r => r.MemberNames.Contains("ProposedLeverage"));
    }

    #endregion

    #region ClosePositionRequest Tests

    [Fact]
    public void ClosePositionRequest_ValidReason_PassesValidation()
    {
        // Arrange
        var request = new AlgoTrendy.API.Controllers.ClosePositionRequest
        {
            Reason = "Manual close by user"
        };

        // Act
        var results = ValidateModel(request);

        // Assert
        results.Should().BeEmpty();
    }

    [Theory]
    [InlineData("Manual")]
    [InlineData("Margin Call")]
    [InlineData("Liquidation")]
    [InlineData("Stop loss triggered at -5.2%")]
    [InlineData("Take profit reached (target: 10%)")]
    public void ClosePositionRequest_ValidReasons_PassValidation(string validReason)
    {
        // Arrange
        var request = new AlgoTrendy.API.Controllers.ClosePositionRequest
        {
            Reason = validReason
        };

        // Act
        var results = ValidateModel(request);

        // Assert
        results.Should().NotContain(r => r.MemberNames.Contains("Reason"));
    }

    [Theory]
    [InlineData("AB")]  // Too short (min 3)
    [InlineData("")]    // Empty
    public void ClosePositionRequest_TooShortReason_FailsValidation(string shortReason)
    {
        // Arrange
        var request = new AlgoTrendy.API.Controllers.ClosePositionRequest
        {
            Reason = shortReason
        };

        // Act
        var results = ValidateModel(request);

        // Assert
        results.Should().NotBeEmpty();
        results.Should().Contain(r => r.MemberNames.Contains("Reason"));
    }

    [Fact]
    public void ClosePositionRequest_TooLongReason_FailsValidation()
    {
        // Arrange - Max 500 characters
        var request = new AlgoTrendy.API.Controllers.ClosePositionRequest
        {
            Reason = new string('a', 501)
        };

        // Act
        var results = ValidateModel(request);

        // Assert
        results.Should().ContainSingle(r =>
            r.MemberNames.Contains("Reason") &&
            r.ErrorMessage!.Contains("500 characters"));
    }

    [Theory]
    [InlineData("Reason<script>alert(1)</script>")]  // XSS attempt
    [InlineData("Reason'; DROP TABLE--")]             // SQL injection
    [InlineData("Reason@test")]                       // Invalid character
    [InlineData("Reason#test")]                       // Invalid character
    public void ClosePositionRequest_InvalidCharacters_FailsValidation(string invalidReason)
    {
        // Arrange
        var request = new AlgoTrendy.API.Controllers.ClosePositionRequest
        {
            Reason = invalidReason
        };

        // Act
        var results = ValidateModel(request);

        // Assert
        results.Should().NotBeEmpty("Invalid characters should be rejected");
        results.Should().Contain(r =>
            r.MemberNames.Contains("Reason") &&
            r.ErrorMessage!.Contains("invalid characters"));
    }

    #endregion

    #region Liquidation Risk Tests

    [Fact]
    public void SetLeverageRequest_CalculateLiquidationRisk_10xVs75x()
    {
        // This test demonstrates why 10x is safe and 75x is dangerous

        // At 10x leverage: Liquidation at 10% price drop
        decimal leverage10x = 10.0m;
        decimal liquidationDrop10x = (1m / leverage10x) * 100m;  // 10%

        // At 75x leverage (v2.5 default): Liquidation at 1.33% price drop
        decimal leverage75x = 75.0m;
        decimal liquidationDrop75x = (1m / leverage75x) * 100m;  // 1.33%

        // Assert - 10x is 7.5x safer
        liquidationDrop10x.Should().BeGreaterThan(liquidationDrop75x * 7);

        // 10% drop is survivable in normal market volatility
        liquidationDrop10x.Should().BeGreaterThan(5m, "10% drop allows for normal market volatility");

        // 1.33% drop is too tight - can liquidate from normal price movements
        liquidationDrop75x.Should().BeLessThan(2m, "75x leverage is dangerously tight");
    }

    #endregion
}
