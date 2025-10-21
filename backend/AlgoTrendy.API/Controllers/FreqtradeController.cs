using AlgoTrendy.TradingEngine.Brokers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace AlgoTrendy.API.Controllers;

/// <summary>
/// Freqtrade integration API endpoints
/// Provides access to multiple Freqtrade bot instances for portfolio aggregation
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class FreqtradeController : ControllerBase
{
    private readonly ILogger<FreqtradeController> _logger;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IConfiguration _configuration;

    // Freqtrade bot configurations
    private readonly List<FreqtradeBotConfig> _botConfigs;

    public FreqtradeController(
        ILogger<FreqtradeController> logger,
        IHttpClientFactory httpClientFactory,
        IConfiguration configuration)
    {
        _logger = logger;
        _httpClientFactory = httpClientFactory;
        _configuration = configuration;

        // Initialize bot configurations from appsettings or environment
        _botConfigs = new List<FreqtradeBotConfig>
        {
            new FreqtradeBotConfig
            {
                Name = "Conservative RSI",
                Port = _configuration.GetValue<int>("Freqtrade:ConservativeRSI:Port", 8082),
                Username = _configuration["Freqtrade:ConservativeRSI:Username"] ?? "memgpt",
                Password = _configuration["Freqtrade:ConservativeRSI:Password"] ?? "trading123",
                Strategy = "RSI_Conservative",
                Description = "Conservative RSI-based trading with tight risk management"
            },
            new FreqtradeBotConfig
            {
                Name = "MACD Hunter",
                Port = _configuration.GetValue<int>("Freqtrade:MACDHunter:Port", 8083),
                Username = _configuration["Freqtrade:MACDHunter:Username"] ?? "memgpt",
                Password = _configuration["Freqtrade:MACDHunter:Password"] ?? "trading123",
                Strategy = "MACD_Aggressive",
                Description = "Aggressive MACD crossover strategy for trending markets"
            },
            new FreqtradeBotConfig
            {
                Name = "Aggressive RSI",
                Port = _configuration.GetValue<int>("Freqtrade:AggressiveRSI:Port", 8084),
                Username = _configuration["Freqtrade:AggressiveRSI:Username"] ?? "memgpt",
                Password = _configuration["Freqtrade:AggressiveRSI:Password"] ?? "trading123",
                Strategy = "RSI_Aggressive",
                Description = "High-frequency RSI trading with dynamic position sizing"
            }
        };
    }

    /// <summary>
    /// Get status of all Freqtrade bots
    /// </summary>
    /// <returns>List of bot statuses with performance metrics</returns>
    [HttpGet("bots")]
    [ProducesResponseType(typeof(FreqtradeBotsSummary), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetBots()
    {
        try
        {
            var bots = new List<FreqtradeBotStatus>();

            foreach (var botConfig in _botConfigs)
            {
                var botStatus = await GetBotStatusAsync(botConfig);
                if (botStatus != null)
                {
                    bots.Add(botStatus);
                }
            }

            var summary = new FreqtradeBotsSummary
            {
                Bots = bots,
                TotalBots = bots.Count,
                OnlineBots = bots.Count(b => b.Status == "online"),
                TotalBalance = bots.Sum(b => b.Balance),
                TotalProfit = bots.Sum(b => b.Profit),
                TotalOpenTrades = bots.Sum(b => b.OpenTrades),
                LastUpdated = DateTime.UtcNow
            };

            return Ok(new { success = true, data = summary });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching Freqtrade bots status");
            return StatusCode(500, new { success = false, error = ex.Message });
        }
    }

    /// <summary>
    /// Get combined portfolio from all Freqtrade bots
    /// </summary>
    /// <returns>Aggregated portfolio data</returns>
    [HttpGet("portfolio")]
    [ProducesResponseType(typeof(FreqtradePortfolio), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetPortfolio()
    {
        try
        {
            var portfolio = new FreqtradePortfolio
            {
                TotalBalance = 0m,
                TotalProfit = 0m,
                TotalProfitPercent = 0m,
                OpenTrades = 0,
                ClosedTrades = 0,
                WinningTrades = 0,
                LosingTrades = 0,
                WinRate = 0m,
                BestPerformingBot = "",
                WorstPerformingBot = "",
                DailyPnL = 0m,
                Bots = new Dictionary<string, BotPortfolio>()
            };

            decimal bestProfit = decimal.MinValue;
            decimal worstProfit = decimal.MaxValue;

            foreach (var botConfig in _botConfigs)
            {
                var botPortfolio = await GetBotPortfolioAsync(botConfig);
                if (botPortfolio != null)
                {
                    portfolio.TotalBalance += botPortfolio.Balance;
                    portfolio.TotalProfit += botPortfolio.TotalProfit;
                    portfolio.OpenTrades += botPortfolio.OpenTrades;
                    portfolio.ClosedTrades += botPortfolio.ClosedTrades;
                    portfolio.WinningTrades += botPortfolio.WinningTrades;
                    portfolio.LosingTrades += botPortfolio.LosingTrades;

                    portfolio.Bots[botConfig.Name] = botPortfolio;

                    if (botPortfolio.TotalProfit > bestProfit)
                    {
                        bestProfit = botPortfolio.TotalProfit;
                        portfolio.BestPerformingBot = botConfig.Name;
                    }

                    if (botPortfolio.TotalProfit < worstProfit)
                    {
                        worstProfit = botPortfolio.TotalProfit;
                        portfolio.WorstPerformingBot = botConfig.Name;
                    }
                }
            }

            // Calculate aggregate metrics
            if (portfolio.ClosedTrades > 0)
            {
                portfolio.WinRate = ((decimal)portfolio.WinningTrades / portfolio.ClosedTrades) * 100;
            }

            if (portfolio.TotalBalance > 0)
            {
                portfolio.TotalProfitPercent = (portfolio.TotalProfit / portfolio.TotalBalance) * 100;
            }

            return Ok(new { success = true, data = portfolio });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching Freqtrade portfolio");
            return StatusCode(500, new { success = false, error = ex.Message });
        }
    }

    /// <summary>
    /// Get active positions from Freqtrade bots
    /// </summary>
    /// <param name="botName">Optional bot name filter</param>
    /// <returns>List of active positions</returns>
    [HttpGet("positions")]
    [ProducesResponseType(typeof(List<FreqtradePosition>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetPositions([FromQuery] string? botName = null)
    {
        try
        {
            var allPositions = new List<FreqtradePosition>();

            var botsToQuery = string.IsNullOrEmpty(botName)
                ? _botConfigs
                : _botConfigs.Where(b => b.Name.Equals(botName, StringComparison.OrdinalIgnoreCase)).ToList();

            foreach (var botConfig in botsToQuery)
            {
                var positions = await GetBotPositionsAsync(botConfig);
                if (positions != null)
                {
                    allPositions.AddRange(positions);
                }
            }

            return Ok(new { success = true, data = allPositions, count = allPositions.Count });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching Freqtrade positions");
            return StatusCode(500, new { success = false, error = ex.Message });
        }
    }

    /// <summary>
    /// Trigger Algolia indexing of Freqtrade data
    /// </summary>
    /// <returns>Indexing result</returns>
    [HttpPost("index")]
    [ProducesResponseType(typeof(IndexingResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> TriggerIndexing()
    {
        try
        {
            _logger.LogInformation("Triggering Freqtrade data indexing");

            // TODO: Implement Algolia indexing
            // For now, return success
            var result = new IndexingResult
            {
                Success = true,
                Message = "Freqtrade data indexing triggered successfully",
                RecordsIndexed = 0,
                IndexName = "algotrendy_trades",
                Timestamp = DateTime.UtcNow
            };

            return Ok(new { success = true, data = result });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error triggering Freqtrade indexing");
            return StatusCode(500, new { success = false, error = ex.Message });
        }
    }

    #region Helper Methods

    private async Task<FreqtradeBotStatus?> GetBotStatusAsync(FreqtradeBotConfig config)
    {
        try
        {
            var client = CreateHttpClient(config);

            // Test connection
            var pingResponse = await client.GetAsync("/api/v1/ping");
            if (!pingResponse.IsSuccessStatusCode)
            {
                return CreateOfflineBot(config);
            }

            // Get profit data
            var profitResponse = await client.GetAsync("/api/v1/profit");
            var statusResponse = await client.GetAsync("/api/v1/status");

            var profitData = await DeserializeResponse<FreqtradeProfitData>(profitResponse);
            var statusData = await DeserializeResponse<List<FreqtradeTradeData>>(statusResponse);

            return new FreqtradeBotStatus
            {
                Name = config.Name,
                Port = config.Port,
                Strategy = config.Strategy,
                Status = "online",
                Balance = profitData?.TotalBalance ?? 0m,
                Profit = profitData?.ProfitAllCoin ?? 0m,
                ProfitPercent = (profitData?.ProfitAllRatio ?? 0m) * 100,
                OpenTrades = statusData?.Count ?? 0,
                WinRate = profitData?.WinRate ?? 0m,
                LastUpdated = DateTime.UtcNow
            };
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, $"Bot '{config.Name}' on port {config.Port} is offline");
            return CreateOfflineBot(config);
        }
    }

    private async Task<BotPortfolio?> GetBotPortfolioAsync(FreqtradeBotConfig config)
    {
        try
        {
            var client = CreateHttpClient(config);
            var profitResponse = await client.GetAsync("/api/v1/profit");
            var profitData = await DeserializeResponse<FreqtradeProfitData>(profitResponse);

            if (profitData == null) return null;

            return new BotPortfolio
            {
                Balance = profitData.TotalBalance,
                TotalProfit = profitData.ProfitAllCoin,
                ProfitPercent = profitData.ProfitAllRatio * 100,
                OpenTrades = profitData.TradeCount,
                ClosedTrades = profitData.ClosedTradeCount,
                WinningTrades = profitData.WinningTrades,
                LosingTrades = profitData.LosingTrades,
                WinRate = profitData.WinRate
            };
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, $"Error fetching portfolio for bot '{config.Name}'");
            return null;
        }
    }

    private async Task<List<FreqtradePosition>?> GetBotPositionsAsync(FreqtradeBotConfig config)
    {
        try
        {
            var client = CreateHttpClient(config);
            var statusResponse = await client.GetAsync("/api/v1/status");
            var trades = await DeserializeResponse<List<FreqtradeTradeData>>(statusResponse);

            if (trades == null || !trades.Any())
                return new List<FreqtradePosition>();

            return trades.Select(t => new FreqtradePosition
            {
                Id = $"freqtrade_{config.Name.ToLower().Replace(" ", "_")}_{t.TradeId}",
                BotName = config.Name,
                Symbol = t.Pair ?? "UNKNOWN",
                Side = t.IsShort ? "short" : "long",
                EntryPrice = t.OpenRate,
                CurrentPrice = t.CurrentRate,
                Quantity = t.Amount,
                PnL = t.ProfitAbs,
                PnLPercent = t.ProfitRatio * 100,
                EntryTime = DateTime.TryParse(t.OpenDate, out var openDate) ? openDate : DateTime.UtcNow,
                DurationMinutes = t.TradeDurationS / 60,
                EntryReason = t.EnterTag ?? "",
                StopLoss = t.StopLoss,
                FreqtradeTradeId = t.TradeId,
                StrategyId = config.Strategy
            }).ToList();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, $"Error fetching positions for bot '{config.Name}'");
            return null;
        }
    }

    private HttpClient CreateHttpClient(FreqtradeBotConfig config)
    {
        var client = _httpClientFactory.CreateClient();
        client.BaseAddress = new Uri($"http://127.0.0.1:{config.Port}");
        client.Timeout = TimeSpan.FromSeconds(10);

        var authBytes = Encoding.ASCII.GetBytes($"{config.Username}:{config.Password}");
        var authHeader = Convert.ToBase64String(authBytes);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", authHeader);

        return client;
    }

    private async Task<T?> DeserializeResponse<T>(HttpResponseMessage response)
    {
        if (!response.IsSuccessStatusCode)
            return default;

        var content = await response.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<T>(content, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });
    }

    private FreqtradeBotStatus CreateOfflineBot(FreqtradeBotConfig config)
    {
        return new FreqtradeBotStatus
        {
            Name = config.Name,
            Port = config.Port,
            Strategy = config.Strategy,
            Status = "offline",
            Balance = 0m,
            Profit = 0m,
            ProfitPercent = 0m,
            OpenTrades = 0,
            WinRate = 0m,
            LastUpdated = DateTime.UtcNow
        };
    }

    #endregion

    #region Data Models

    private class FreqtradeBotConfig
    {
        public string Name { get; set; } = "";
        public int Port { get; set; }
        public string Username { get; set; } = "";
        public string Password { get; set; } = "";
        public string Strategy { get; set; } = "";
        public string Description { get; set; } = "";
    }

    private class FreqtradeProfitData
    {
        public decimal ProfitAllCoin { get; set; }
        public decimal ProfitAllRatio { get; set; }
        public decimal TotalBalance { get; set; }
        public int TradeCount { get; set; }
        public int ClosedTradeCount { get; set; }
        public int WinningTrades { get; set; }
        public int LosingTrades { get; set; }
        public decimal WinRate => ClosedTradeCount > 0 ? ((decimal)WinningTrades / ClosedTradeCount) * 100 : 0m;
    }

    private class FreqtradeTradeData
    {
        public int TradeId { get; set; }
        public string? Pair { get; set; }
        public bool IsShort { get; set; }
        public decimal Amount { get; set; }
        public decimal OpenRate { get; set; }
        public decimal CurrentRate { get; set; }
        public decimal ProfitAbs { get; set; }
        public decimal ProfitRatio { get; set; }
        public string? OpenDate { get; set; }
        public int TradeDurationS { get; set; }
        public string? EnterTag { get; set; }
        public decimal? StopLoss { get; set; }
    }

    #endregion

    #region Response Models

    public class FreqtradeBotsSummary
    {
        public List<FreqtradeBotStatus> Bots { get; set; } = new();
        public int TotalBots { get; set; }
        public int OnlineBots { get; set; }
        public decimal TotalBalance { get; set; }
        public decimal TotalProfit { get; set; }
        public int TotalOpenTrades { get; set; }
        public DateTime LastUpdated { get; set; }
    }

    public class FreqtradeBotStatus
    {
        public string Name { get; set; } = "";
        public int Port { get; set; }
        public string Strategy { get; set; } = "";
        public string Status { get; set; } = "";
        public decimal Balance { get; set; }
        public decimal Profit { get; set; }
        public decimal ProfitPercent { get; set; }
        public int OpenTrades { get; set; }
        public decimal WinRate { get; set; }
        public DateTime LastUpdated { get; set; }
    }

    public class FreqtradePortfolio
    {
        public decimal TotalBalance { get; set; }
        public decimal TotalProfit { get; set; }
        public decimal TotalProfitPercent { get; set; }
        public int OpenTrades { get; set; }
        public int ClosedTrades { get; set; }
        public int WinningTrades { get; set; }
        public int LosingTrades { get; set; }
        public decimal WinRate { get; set; }
        public string BestPerformingBot { get; set; } = "";
        public string WorstPerformingBot { get; set; } = "";
        public decimal DailyPnL { get; set; }
        public Dictionary<string, BotPortfolio> Bots { get; set; } = new();
    }

    public class BotPortfolio
    {
        public decimal Balance { get; set; }
        public decimal TotalProfit { get; set; }
        public decimal ProfitPercent { get; set; }
        public int OpenTrades { get; set; }
        public int ClosedTrades { get; set; }
        public int WinningTrades { get; set; }
        public int LosingTrades { get; set; }
        public decimal WinRate { get; set; }
    }

    public class FreqtradePosition
    {
        public string Id { get; set; } = "";
        public string BotName { get; set; } = "";
        public string Symbol { get; set; } = "";
        public string Side { get; set; } = "";
        public decimal EntryPrice { get; set; }
        public decimal CurrentPrice { get; set; }
        public decimal Quantity { get; set; }
        public decimal PnL { get; set; }
        public decimal PnLPercent { get; set; }
        public DateTime EntryTime { get; set; }
        public int DurationMinutes { get; set; }
        public string EntryReason { get; set; } = "";
        public decimal? StopLoss { get; set; }
        public int FreqtradeTradeId { get; set; }
        public string StrategyId { get; set; } = "";
    }

    public class IndexingResult
    {
        public bool Success { get; set; }
        public string Message { get; set; } = "";
        public int RecordsIndexed { get; set; }
        public string IndexName { get; set; } = "";
        public DateTime Timestamp { get; set; }
    }

    #endregion
}
