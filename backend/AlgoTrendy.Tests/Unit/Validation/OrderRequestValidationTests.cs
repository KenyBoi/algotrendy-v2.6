using AlgoTrendy.Core.Models;
using AlgoTrendy.Core.Enums;
using System.ComponentModel.DataAnnotations;
using Xunit;
using FluentAssertions;

namespace AlgoTrendy.Tests.Unit.Validation;

/// <summary>
/// Unit tests for OrderRequest validation attributes
/// Tests protection against SQL injection, XSS, overflow, and invalid input
/// </summary>
public class OrderRequestValidationTests
{
    private static List<ValidationResult> ValidateModel(object model)
    {
        var validationResults = new List<ValidationResult>();
        var validationContext = new ValidationContext(model);
        Validator.TryValidateObject(model, validationContext, validationResults, validateAllProperties: true);
        return validationResults;
    }

    [Fact]
    public void OrderRequest_ValidInput_PassesValidation()
    {
        // Arrange
        var request = new OrderRequest
        {
            Symbol = "BTCUSDT",
            Exchange = "binance",
            Side = OrderSide.Buy,
            Type = OrderType.Market,
            Quantity = 1.5m
        };

        // Act
        var results = ValidateModel(request);

        // Assert
        results.Should().BeEmpty();
    }

    #region Symbol Validation Tests

    [Theory]
    [InlineData("BTC'; DROP TABLE orders;--")]  // SQL injection attempt
    [InlineData("BTC<script>alert(1)</script>")] // XSS attempt
    [InlineData("BTC@USDT")]                     // Invalid character
    [InlineData("BTC USDT")]                     // Space character
    [InlineData("btcusdt")]                      // Lowercase (should be uppercase)
    public void OrderRequest_InvalidSymbolFormat_FailsValidation(string invalidSymbol)
    {
        // Arrange
        var request = new OrderRequest
        {
            Symbol = invalidSymbol,
            Exchange = "binance",
            Side = OrderSide.Buy,
            Type = OrderType.Market,
            Quantity = 1.0m
        };

        // Act
        var results = ValidateModel(request);

        // Assert
        results.Should().ContainSingle(r =>
            r.MemberNames.Contains("Symbol") &&
            r.ErrorMessage!.Contains("must contain only uppercase letters"));
    }

    [Theory]
    [InlineData("BT")]      // Too short (min 3)
    [InlineData("ABCDEFGHIJKLMNOPQRSTU")]  // Too long (max 20)
    public void OrderRequest_InvalidSymbolLength_FailsValidation(string invalidSymbol)
    {
        // Arrange
        var request = new OrderRequest
        {
            Symbol = invalidSymbol,
            Exchange = "binance",
            Side = OrderSide.Buy,
            Type = OrderType.Market,
            Quantity = 1.0m
        };

        // Act
        var results = ValidateModel(request);

        // Assert
        results.Should().ContainSingle(r =>
            r.MemberNames.Contains("Symbol") &&
            r.ErrorMessage!.Contains("must be between 3 and 20 characters"));
    }

    [Fact]
    public void OrderRequest_EmptySymbol_FailsValidation()
    {
        // Arrange
        var request = new OrderRequest
        {
            Symbol = "",
            Exchange = "binance",
            Side = OrderSide.Buy,
            Type = OrderType.Market,
            Quantity = 1.0m
        };

        // Act
        var results = ValidateModel(request);

        // Assert
        results.Should().Contain(r =>
            r.MemberNames.Contains("Symbol") &&
            r.ErrorMessage!.Contains("Symbol is required"));
    }

    [Theory]
    [InlineData("BTCUSDT")]      // Standard crypto pair
    [InlineData("BTC-USD")]      // Coinbase format
    [InlineData("BTC_USDT")]     // Alternative format
    [InlineData("BTC/USDT")]     // Slash format
    [InlineData("XXBTZUSD")]     // Kraken format
    public void OrderRequest_ValidSymbolFormats_PassValidation(string validSymbol)
    {
        // Arrange
        var request = new OrderRequest
        {
            Symbol = validSymbol,
            Exchange = "binance",
            Side = OrderSide.Buy,
            Type = OrderType.Market,
            Quantity = 1.0m
        };

        // Act
        var results = ValidateModel(request);

        // Assert
        results.Should().NotContain(r => r.MemberNames.Contains("Symbol"));
    }

    #endregion

    #region Exchange Validation Tests

    [Theory]
    [InlineData("binance123")]   // Contains numbers
    [InlineData("bin-ance")]     // Contains hyphen
    [InlineData("bin ance")]     // Contains space
    [InlineData("binance'; DROP TABLE--")]  // SQL injection
    public void OrderRequest_InvalidExchangeFormat_FailsValidation(string invalidExchange)
    {
        // Arrange
        var request = new OrderRequest
        {
            Symbol = "BTCUSDT",
            Exchange = invalidExchange,
            Side = OrderSide.Buy,
            Type = OrderType.Market,
            Quantity = 1.0m
        };

        // Act
        var results = ValidateModel(request);

        // Assert
        results.Should().ContainSingle(r =>
            r.MemberNames.Contains("Exchange") &&
            r.ErrorMessage!.Contains("must contain only letters"));
    }

    [Theory]
    [InlineData("binance")]
    [InlineData("Binance")]
    [InlineData("BINANCE")]
    [InlineData("okx")]
    [InlineData("coinbase")]
    public void OrderRequest_ValidExchangeNames_PassValidation(string validExchange)
    {
        // Arrange
        var request = new OrderRequest
        {
            Symbol = "BTCUSDT",
            Exchange = validExchange,
            Side = OrderSide.Buy,
            Type = OrderType.Market,
            Quantity = 1.0m
        };

        // Act
        var results = ValidateModel(request);

        // Assert
        results.Should().NotContain(r => r.MemberNames.Contains("Exchange"));
    }

    #endregion

    #region Quantity Validation Tests

    [Theory]
    [InlineData(-1.0)]           // Negative
    [InlineData(0)]              // Zero
    [InlineData(0.000000001)]    // Below minimum
    [InlineData(1000001)]        // Above maximum
    [InlineData(99999999)]       // Way above maximum
    public void OrderRequest_InvalidQuantity_FailsValidation(double invalidQuantity)
    {
        // Arrange
        var request = new OrderRequest
        {
            Symbol = "BTCUSDT",
            Exchange = "binance",
            Side = OrderSide.Buy,
            Type = OrderType.Market,
            Quantity = (decimal)invalidQuantity
        };

        // Act
        var results = ValidateModel(request);

        // Assert
        results.Should().ContainSingle(r =>
            r.MemberNames.Contains("Quantity") &&
            r.ErrorMessage!.Contains("must be between"));
    }

    [Theory]
    [InlineData(0.00000001)]     // Minimum valid (1 satoshi)
    [InlineData(0.0001)]         // Small amount
    [InlineData(1.0)]            // 1 unit
    [InlineData(100.5)]          // Decimal
    [InlineData(1000)]           // Large amount
    [InlineData(1000000)]        // Maximum valid
    public void OrderRequest_ValidQuantity_PassesValidation(double validQuantity)
    {
        // Arrange
        var request = new OrderRequest
        {
            Symbol = "BTCUSDT",
            Exchange = "binance",
            Side = OrderSide.Buy,
            Type = OrderType.Market,
            Quantity = (decimal)validQuantity
        };

        // Act
        var results = ValidateModel(request);

        // Assert
        results.Should().NotContain(r => r.MemberNames.Contains("Quantity"));
    }

    #endregion

    #region Price Validation Tests

    [Theory]
    [InlineData(-1000)]          // Negative price
    [InlineData(0)]              // Zero price
    [InlineData(10000001)]       // Above maximum
    public void OrderRequest_InvalidPrice_FailsValidation(double invalidPrice)
    {
        // Arrange
        var request = new OrderRequest
        {
            Symbol = "BTCUSDT",
            Exchange = "binance",
            Side = OrderSide.Buy,
            Type = OrderType.Limit,
            Quantity = 1.0m,
            Price = (decimal)invalidPrice
        };

        // Act
        var results = ValidateModel(request);

        // Assert
        results.Should().ContainSingle(r =>
            r.MemberNames.Contains("Price") &&
            r.ErrorMessage!.Contains("must be between"));
    }

    [Theory]
    [InlineData(0.00000001)]     // Minimum
    [InlineData(50000)]          // BTC price range
    [InlineData(100000)]         // High BTC price
    [InlineData(10000000)]       // Maximum
    public void OrderRequest_ValidPrice_PassesValidation(double validPrice)
    {
        // Arrange
        var request = new OrderRequest
        {
            Symbol = "BTCUSDT",
            Exchange = "binance",
            Side = OrderSide.Buy,
            Type = OrderType.Limit,
            Quantity = 1.0m,
            Price = (decimal)validPrice
        };

        // Act
        var results = ValidateModel(request);

        // Assert
        results.Should().NotContain(r => r.MemberNames.Contains("Price"));
    }

    [Fact]
    public void OrderRequest_NullPrice_ForMarketOrder_PassesValidation()
    {
        // Arrange - Market orders don't need price
        var request = new OrderRequest
        {
            Symbol = "BTCUSDT",
            Exchange = "binance",
            Side = OrderSide.Buy,
            Type = OrderType.Market,
            Quantity = 1.0m,
            Price = null
        };

        // Act
        var results = ValidateModel(request);

        // Assert
        results.Should().NotContain(r => r.MemberNames.Contains("Price"));
    }

    #endregion

    #region ClientOrderId Validation Tests

    [Theory]
    [InlineData("<script>alert(1)</script>")]     // XSS attempt
    [InlineData("order'; DROP TABLE--")]           // SQL injection
    [InlineData("order@123")]                      // Invalid character
    [InlineData("order 123")]                      // Space
    [InlineData("order.123")]                      // Period
    public void OrderRequest_InvalidClientOrderId_FailsValidation(string invalidId)
    {
        // Arrange
        var request = new OrderRequest
        {
            Symbol = "BTCUSDT",
            Exchange = "binance",
            Side = OrderSide.Buy,
            Type = OrderType.Market,
            Quantity = 1.0m,
            ClientOrderId = invalidId
        };

        // Act
        var results = ValidateModel(request);

        // Assert
        results.Should().ContainSingle(r =>
            r.MemberNames.Contains("ClientOrderId") &&
            r.ErrorMessage!.Contains("can only contain alphanumeric"));
    }

    [Theory]
    [InlineData("order_123")]
    [InlineData("ORDER-456")]
    [InlineData("abc123DEF")]
    [InlineData("user_order_2024_01_01")]
    public void OrderRequest_ValidClientOrderId_PassesValidation(string validId)
    {
        // Arrange
        var request = new OrderRequest
        {
            Symbol = "BTCUSDT",
            Exchange = "binance",
            Side = OrderSide.Buy,
            Type = OrderType.Market,
            Quantity = 1.0m,
            ClientOrderId = validId
        };

        // Act
        var results = ValidateModel(request);

        // Assert
        results.Should().NotContain(r => r.MemberNames.Contains("ClientOrderId"));
    }

    [Fact]
    public void OrderRequest_NullClientOrderId_PassesValidation()
    {
        // Arrange - ClientOrderId is optional
        var request = new OrderRequest
        {
            Symbol = "BTCUSDT",
            Exchange = "binance",
            Side = OrderSide.Buy,
            Type = OrderType.Market,
            Quantity = 1.0m,
            ClientOrderId = null
        };

        // Act
        var results = ValidateModel(request);

        // Assert
        results.Should().NotContain(r => r.MemberNames.Contains("ClientOrderId"));
    }

    [Fact]
    public void OrderRequest_TooLongClientOrderId_FailsValidation()
    {
        // Arrange - Max 100 characters
        var request = new OrderRequest
        {
            Symbol = "BTCUSDT",
            Exchange = "binance",
            Side = OrderSide.Buy,
            Type = OrderType.Market,
            Quantity = 1.0m,
            ClientOrderId = new string('a', 101)  // 101 characters
        };

        // Act
        var results = ValidateModel(request);

        // Assert
        results.Should().ContainSingle(r =>
            r.MemberNames.Contains("ClientOrderId") &&
            r.ErrorMessage!.Contains("100 characters or less"));
    }

    #endregion

    #region Metadata Validation Tests

    [Fact]
    public void OrderRequest_TooLongMetadata_FailsValidation()
    {
        // Arrange - Max 5000 characters
        var request = new OrderRequest
        {
            Symbol = "BTCUSDT",
            Exchange = "binance",
            Side = OrderSide.Buy,
            Type = OrderType.Market,
            Quantity = 1.0m,
            Metadata = new string('a', 5001)  // 5001 characters
        };

        // Act
        var results = ValidateModel(request);

        // Assert
        results.Should().ContainSingle(r =>
            r.MemberNames.Contains("Metadata") &&
            r.ErrorMessage!.Contains("5000 characters or less"));
    }

    [Fact]
    public void OrderRequest_ValidJsonMetadata_PassesValidation()
    {
        // Arrange
        var request = new OrderRequest
        {
            Symbol = "BTCUSDT",
            Exchange = "binance",
            Side = OrderSide.Buy,
            Type = OrderType.Market,
            Quantity = 1.0m,
            Metadata = "{\"strategy\":\"momentum\",\"confidence\":0.85}"
        };

        // Act
        var results = ValidateModel(request);

        // Assert
        results.Should().NotContain(r => r.MemberNames.Contains("Metadata"));
    }

    #endregion

    #region Security Attack Tests

    [Fact]
    public void OrderRequest_SQLInjectionAttempt_IsBlocked()
    {
        // Arrange - Attempt SQL injection via Symbol
        var request = new OrderRequest
        {
            Symbol = "BTC'; DELETE FROM orders WHERE '1'='1",
            Exchange = "binance",
            Side = OrderSide.Buy,
            Type = OrderType.Market,
            Quantity = 1.0m
        };

        // Act
        var results = ValidateModel(request);

        // Assert
        results.Should().NotBeEmpty("SQL injection attempt should be blocked");
        results.Should().Contain(r => r.MemberNames.Contains("Symbol"));
    }

    [Fact]
    public void OrderRequest_XSSAttempt_IsBlocked()
    {
        // Arrange - Attempt XSS via ClientOrderId
        var request = new OrderRequest
        {
            Symbol = "BTCUSDT",
            Exchange = "binance",
            Side = OrderSide.Buy,
            Type = OrderType.Market,
            Quantity = 1.0m,
            ClientOrderId = "<img src=x onerror=alert(1)>"
        };

        // Act
        var results = ValidateModel(request);

        // Assert
        results.Should().NotBeEmpty("XSS attempt should be blocked");
        results.Should().Contain(r => r.MemberNames.Contains("ClientOrderId"));
    }

    [Fact]
    public void OrderRequest_IntegerOverflowAttempt_IsBlocked()
    {
        // Arrange - Attempt integer overflow via Quantity
        var request = new OrderRequest
        {
            Symbol = "BTCUSDT",
            Exchange = "binance",
            Side = OrderSide.Buy,
            Type = OrderType.Market,
            Quantity = decimal.MaxValue
        };

        // Act
        var results = ValidateModel(request);

        // Assert
        results.Should().NotBeEmpty("Integer overflow attempt should be blocked");
        results.Should().Contain(r => r.MemberNames.Contains("Quantity"));
    }

    #endregion
}
