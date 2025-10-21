namespace AlgoTrendy.MultiCharts.Strategies;

/// <summary>
/// Sample trading strategies for MultiCharts .NET
/// These can be deployed to MultiCharts and backtested
/// </summary>
public static class SampleStrategies
{
    /// <summary>
    /// Simple Moving Average Crossover Strategy (C# format for MultiCharts .NET)
    /// </summary>
    public const string SMACrossover = @"
using System;
using System.Drawing;
using System.Linq;
using PowerLanguage.Function;

namespace PowerLanguage.Strategy
{
    public class SMA_Crossover : SignalObject
    {
        [Input]
        public int FastPeriod { get; set; }

        [Input]
        public int SlowPeriod { get; set; }

        private IOrderMarket buyMarket;
        private IOrderMarket sellMarket;

        private VariableSeries<Double> fastMA;
        private VariableSeries<Double> slowMA;

        protected override void Create()
        {
            FastPeriod = 10;
            SlowPeriod = 30;

            fastMA = new VariableSeries<Double>(this);
            slowMA = new VariableSeries<Double>(this);
        }

        protected override void StartCalc()
        {
            buyMarket = OrderCreator.MarketNextBar(new SOrderParameters(Contracts.Default, EOrderAction.Buy));
            sellMarket = OrderCreator.MarketNextBar(new SOrderParameters(Contracts.Default, EOrderAction.Sell));
        }

        protected override void CalcBar()
        {
            // Calculate moving averages
            fastMA.Value = Bars.Close.Average(FastPeriod);
            slowMA.Value = Bars.Close.Average(SlowPeriod);

            // Generate signals
            if (fastMA.Value > slowMA.Value && fastMA[1] <= slowMA[1])
            {
                // Bullish crossover - Buy
                buyMarket.Send();
            }
            else if (fastMA.Value < slowMA.Value && fastMA[1] >= slowMA[1])
            {
                // Bearish crossover - Sell
                sellMarket.Send();
            }
        }
    }
}";

    /// <summary>
    /// RSI Mean Reversion Strategy
    /// </summary>
    public const string RSIMeanReversion = @"
using System;
using System.Drawing;
using PowerLanguage.Function;

namespace PowerLanguage.Strategy
{
    public class RSI_MeanReversion : SignalObject
    {
        [Input]
        public int RSIPeriod { get; set; }

        [Input]
        public double OversoldLevel { get; set; }

        [Input]
        public double OverboughtLevel { get; set; }

        private IOrderMarket buyMarket;
        private IOrderMarket sellMarket;

        private VariableObject<RSI> rsi;

        protected override void Create()
        {
            RSIPeriod = 14;
            OversoldLevel = 30;
            OverboughtLevel = 70;

            rsi = new VariableObject<RSI>(this);
        }

        protected override void StartCalc()
        {
            rsi.Value = new RSI(this);
            rsi.Value.period = RSIPeriod;
            rsi.Value.pricevalue = Bars.Close;

            buyMarket = OrderCreator.MarketNextBar(new SOrderParameters(Contracts.Default, EOrderAction.Buy));
            sellMarket = OrderCreator.MarketNextBar(new SOrderParameters(Contracts.Default, EOrderAction.Sell));
        }

        protected override void CalcBar()
        {
            double rsiValue = rsi.Value.Value;

            // Buy when oversold
            if (rsiValue < OversoldLevel && rsi.Value[1] >= OversoldLevel)
            {
                buyMarket.Send();
            }
            // Sell when overbought
            else if (rsiValue > OverboughtLevel && rsi.Value[1] <= OverboughtLevel)
            {
                sellMarket.Send();
            }
        }
    }
}";

    /// <summary>
    /// Bollinger Bands Breakout Strategy
    /// </summary>
    public const string BollingerBreakout = @"
using System;
using System.Drawing;
using PowerLanguage.Function;

namespace PowerLanguage.Strategy
{
    public class Bollinger_Breakout : SignalObject
    {
        [Input]
        public int Period { get; set; }

        [Input]
        public double NumStdDev { get; set; }

        private IOrderMarket buyMarket;
        private IOrderMarket sellMarket;

        private VariableObject<BBandUpper> upperBand;
        private VariableObject<BBandLower> lowerBand;

        protected override void Create()
        {
            Period = 20;
            NumStdDev = 2.0;

            upperBand = new VariableObject<BBandUpper>(this);
            lowerBand = new VariableObject<BBandLower>(this);
        }

        protected override void StartCalc()
        {
            upperBand.Value = new BBandUpper(this);
            upperBand.Value.price = Bars.Close;
            upperBand.Value.length = Period;
            upperBand.Value.numdevsup = NumStdDev;

            lowerBand.Value = new BBandLower(this);
            lowerBand.Value.price = Bars.Close;
            lowerBand.Value.length = Period;
            lowerBand.Value.numdevsdn = NumStdDev;

            buyMarket = OrderCreator.MarketNextBar(new SOrderParameters(Contracts.Default, EOrderAction.Buy));
            sellMarket = OrderCreator.MarketNextBar(new SOrderParameters(Contracts.Default, EOrderAction.Sell));
        }

        protected override void CalcBar()
        {
            double upper = upperBand.Value.Value;
            double lower = lowerBand.Value.Value;
            double close = Bars.Close[0];

            // Buy on lower band breakout
            if (close < lower && Bars.Close[1] >= lower)
            {
                buyMarket.Send();
            }
            // Sell on upper band breakout
            else if (close > upper && Bars.Close[1] <= upper)
            {
                sellMarket.Send();
            }
        }
    }
}";

    /// <summary>
    /// Get strategy code by name
    /// </summary>
    public static string GetStrategy(string strategyName)
    {
        return strategyName.ToLower() switch
        {
            "sma_crossover" or "sma crossover" => SMACrossover,
            "rsi_meanreversion" or "rsi mean reversion" => RSIMeanReversion,
            "bollinger_breakout" or "bollinger breakout" => BollingerBreakout,
            _ => throw new ArgumentException($"Unknown strategy: {strategyName}")
        };
    }

    /// <summary>
    /// Get list of available strategy names
    /// </summary>
    public static List<string> GetAvailableStrategies()
    {
        return new List<string>
        {
            "SMA_Crossover",
            "RSI_MeanReversion",
            "Bollinger_Breakout"
        };
    }
}
