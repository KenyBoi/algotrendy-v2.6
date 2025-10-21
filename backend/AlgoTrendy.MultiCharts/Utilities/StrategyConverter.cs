using AlgoTrendy.MultiCharts.Models;

namespace AlgoTrendy.MultiCharts.Utilities;

/// <summary>
/// Utility for converting strategies between AlgoTrendy and MultiCharts formats
/// </summary>
public static class StrategyConverter
{
    /// <summary>
    /// Convert AlgoTrendy strategy to MultiCharts PowerLanguage .NET format
    /// </summary>
    public static string ConvertToMultiChartsFormat(string algoTrendyStrategy)
    {
        // This is a simplified conversion - actual implementation would need to parse
        // and transform the strategy code based on the specific format

        // For now, we'll provide a template-based approach
        return @"
using System;
using PowerLanguage.Function;

namespace PowerLanguage.Strategy
{
    public class ConvertedStrategy : SignalObject
    {
        // TODO: Add strategy logic here
        // Original AlgoTrendy code:
        // " + algoTrendyStrategy + @"
    }
}";
    }

    /// <summary>
    /// Extract parameters from MultiCharts strategy code
    /// </summary>
    public static List<StrategyParameter> ExtractParameters(string strategyCode)
    {
        var parameters = new List<StrategyParameter>();

        // Parse [Input] attributes from code
        var lines = strategyCode.Split('\n');
        foreach (var line in lines)
        {
            if (line.Contains("[Input]"))
            {
                var nextLine = lines[Array.IndexOf(lines, line) + 1];
                var param = ParseParameterLine(nextLine);
                if (param != null)
                {
                    parameters.Add(param);
                }
            }
        }

        return parameters;
    }

    private static StrategyParameter? ParseParameterLine(string line)
    {
        try
        {
            // Simple parsing - in production this would be more robust
            var parts = line.Trim().Split(' ');
            if (parts.Length >= 3)
            {
                var type = parts[1];
                var name = parts[2].Replace("{", "").Replace("get;", "").Replace("set;", "").Replace("}", "").Trim();

                return new StrategyParameter
                {
                    Name = name,
                    Type = type,
                    DefaultValue = GetDefaultValue(type)
                };
            }
        }
        catch
        {
            // Ignore parsing errors
        }

        return null;
    }

    private static object GetDefaultValue(string type)
    {
        return type.ToLower() switch
        {
            "int" => 0,
            "double" or "decimal" => 0.0,
            "bool" => false,
            "string" => "",
            _ => new object()
        };
    }

    /// <summary>
    /// Validate MultiCharts strategy code
    /// </summary>
    public static (bool IsValid, List<string> Errors) ValidateStrategy(string strategyCode)
    {
        var errors = new List<string>();

        if (string.IsNullOrWhiteSpace(strategyCode))
        {
            errors.Add("Strategy code cannot be empty");
        }

        if (!strategyCode.Contains("namespace PowerLanguage.Strategy"))
        {
            errors.Add("Strategy must be in PowerLanguage.Strategy namespace");
        }

        if (!strategyCode.Contains(": SignalObject"))
        {
            errors.Add("Strategy must inherit from SignalObject");
        }

        if (!strategyCode.Contains("protected override void CalcBar()"))
        {
            errors.Add("Strategy must implement CalcBar() method");
        }

        return (errors.Count == 0, errors);
    }
}
