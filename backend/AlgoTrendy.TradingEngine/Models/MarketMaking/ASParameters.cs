namespace AlgoTrendy.TradingEngine.Models.MarketMaking;

/// <summary>
/// Avellaneda-Stoikov strategy parameters
/// Controls market making behavior and risk management
/// </summary>
public class ASParameters
{
    /// <summary>
    /// Risk aversion coefficient (gamma)
    /// Higher values = more conservative (wider spreads, smaller positions)
    /// Typical range: 0.01 to 1.0
    /// </summary>
    public required decimal Gamma { get; init; }

    /// <summary>
    /// Market liquidity parameter (kappa)
    /// Higher values = faster order fill rate assumption
    /// Typical range: 0.1 to 10.0
    /// </summary>
    public required decimal Kappa { get; init; }

    /// <summary>
    /// Volatility of the asset (sigma)
    /// Estimated from historical data (annualized)
    /// Typical range: 0.1 to 2.0 (10% to 200%)
    /// </summary>
    public required decimal Sigma { get; init; }

    /// <summary>
    /// Trading horizon - time remaining in session (T)
    /// Normalized: 1.0 at session start, 0.0 at session end
    /// Range: 0.0 to 1.0
    /// </summary>
    public required decimal T { get; init; }

    /// <summary>
    /// Maximum inventory (position size limit)
    /// Absolute value in base currency
    /// Example: 1.0 BTC, 100 ETH
    /// </summary>
    public required decimal MaxInventory { get; init; }

    /// <summary>
    /// Target inventory (usually 0 for market neutral)
    /// </summary>
    public decimal TargetInventory { get; init; } = 0.0m;

    /// <summary>
    /// Minimum spread (bps) to maintain profitability
    /// Prevents placing orders too close together
    /// </summary>
    public decimal MinSpreadBps { get; init; } = 5.0m;

    /// <summary>
    /// Maximum spread (bps) to stay competitive
    /// Prevents placing orders too far from market
    /// </summary>
    public decimal MaxSpreadBps { get; init; } = 100.0m;

    /// <summary>
    /// Validates parameters are within acceptable ranges
    /// </summary>
    /// <returns>True if all parameters valid</returns>
    public bool IsValid()
    {
        return Gamma > 0 && Gamma <= 10.0m &&
               Kappa > 0 && Kappa <= 100.0m &&
               Sigma > 0 && Sigma <= 5.0m &&
               T >= 0 && T <= 1.0m &&
               MaxInventory > 0 &&
               MinSpreadBps >= 0 &&
               MaxSpreadBps > MinSpreadBps;
    }

    /// <summary>
    /// Gets validation errors (if any)
    /// </summary>
    /// <returns>List of validation error messages</returns>
    public List<string> GetValidationErrors()
    {
        var errors = new List<string>();

        if (Gamma <= 0 || Gamma > 10.0m)
            errors.Add($"Gamma must be between 0 and 10.0 (got {Gamma})");

        if (Kappa <= 0 || Kappa > 100.0m)
            errors.Add($"Kappa must be between 0 and 100.0 (got {Kappa})");

        if (Sigma <= 0 || Sigma > 5.0m)
            errors.Add($"Sigma must be between 0 and 5.0 (got {Sigma})");

        if (T < 0 || T > 1.0m)
            errors.Add($"T must be between 0.0 and 1.0 (got {T})");

        if (MaxInventory <= 0)
            errors.Add($"MaxInventory must be positive (got {MaxInventory})");

        if (MinSpreadBps < 0)
            errors.Add($"MinSpreadBps must be non-negative (got {MinSpreadBps})");

        if (MaxSpreadBps <= MinSpreadBps)
            errors.Add($"MaxSpreadBps ({MaxSpreadBps}) must be greater than MinSpreadBps ({MinSpreadBps})");

        return errors;
    }

    /// <summary>
    /// Creates default conservative parameters for testing
    /// </summary>
    /// <param name="maxInventory">Maximum position size</param>
    /// <returns>Conservative ASParameters instance</returns>
    public static ASParameters CreateConservative(decimal maxInventory = 1.0m)
    {
        return new ASParameters
        {
            Gamma = 0.1m,          // Low risk aversion (moderate spreads)
            Kappa = 1.5m,          // Moderate liquidity assumption
            Sigma = 0.5m,          // 50% annualized volatility
            T = 1.0m,              // Full session remaining
            MaxInventory = maxInventory,
            TargetInventory = 0.0m,
            MinSpreadBps = 10.0m,  // 0.1% minimum spread
            MaxSpreadBps = 50.0m   // 0.5% maximum spread
        };
    }

    /// <summary>
    /// Creates aggressive parameters for high-frequency trading
    /// </summary>
    /// <param name="maxInventory">Maximum position size</param>
    /// <returns>Aggressive ASParameters instance</returns>
    public static ASParameters CreateAggressive(decimal maxInventory = 1.0m)
    {
        return new ASParameters
        {
            Gamma = 0.01m,         // Very low risk aversion (tight spreads)
            Kappa = 5.0m,          // High liquidity assumption
            Sigma = 0.3m,          // 30% annualized volatility
            T = 1.0m,              // Full session remaining
            MaxInventory = maxInventory,
            TargetInventory = 0.0m,
            MinSpreadBps = 2.0m,   // 0.02% minimum spread
            MaxSpreadBps = 20.0m   // 0.2% maximum spread
        };
    }

    /// <summary>
    /// Deep clone
    /// </summary>
    public ASParameters Clone()
    {
        return new ASParameters
        {
            Gamma = Gamma,
            Kappa = Kappa,
            Sigma = Sigma,
            T = T,
            MaxInventory = MaxInventory,
            TargetInventory = TargetInventory,
            MinSpreadBps = MinSpreadBps,
            MaxSpreadBps = MaxSpreadBps
        };
    }

    public override string ToString()
    {
        return $"ASParameters: γ={Gamma:F3}, κ={Kappa:F2}, σ={Sigma:F2}, T={T:F2}, " +
               $"MaxInv={MaxInventory:F4}, Spread=[{MinSpreadBps:F1}-{MaxSpreadBps:F1}] bps";
    }
}
