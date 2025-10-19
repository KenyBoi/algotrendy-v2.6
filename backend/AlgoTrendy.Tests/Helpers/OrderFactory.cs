namespace AlgoTrendy.Tests.Unit.TradingEngine;

/// <summary>
/// Helper factory for creating test orders with proper client order IDs
/// </summary>
public static class OrderFactory
{
    /// <summary>
    /// Generates a unique client order ID in the format: "AT_{timestamp}_{guid}"
    /// </summary>
    public static string GenerateClientOrderId()
    {
        var timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        var guid = Guid.NewGuid().ToString("N")[..8]; // First 8 chars
        return $"AT_{timestamp}_{guid}";
    }
}
