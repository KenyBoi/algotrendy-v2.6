using AlgoTrendy.DataChannels.Providers;
using Microsoft.Extensions.Logging;
using Xunit;
using Xunit.Sdk;

namespace AlgoTrendy.Tests.Integration.DataProviders;

/// <summary>
/// Integration tests for FinancialModelingPrepProvider
/// These tests require actual FMP API credentials
/// Set environment variable: FinancialModelingPrep__ApiKey
///
/// Get FREE API key at: https://site.financialmodelingprep.com/
/// Free tier: 250 calls/day, 500MB bandwidth/30 days
///
/// WARNING: These tests consume API quota! Run sparingly on free tier.
/// </summary>
[Collection("FMPIntegration")]
public class FinancialModelingPrepProviderIntegrationTests : IAsyncLifetime
{
    private readonly FinancialModelingPrepProvider _provider;
    private readonly ILogger<FinancialModelingPrepProvider> _logger;
    private readonly bool _hasValidKey;

    public FinancialModelingPrepProviderIntegrationTests()
    {
        // Get API key from environment variable
        var apiKey = Environment.GetEnvironmentVariable("FinancialModelingPrep__ApiKey");

        _hasValidKey = !string.IsNullOrEmpty(apiKey);

        if (!_hasValidKey)
        {
            Skip.If(true, "FMP API key not found. Set FinancialModelingPrep__ApiKey environment variable");
        }

        _logger = LoggerFactory.Create(builder => builder.AddConsole().SetMinimumLevel(LogLevel.Information))
            .CreateLogger<FinancialModelingPrepProvider>();

        var httpClient = new HttpClient();
        _provider = new FinancialModelingPrepProvider(httpClient, _logger, apiKey!);
    }

    public Task InitializeAsync()
    {
        return Task.CompletedTask;
    }

    public Task DisposeAsync()
    {
        _provider?.Dispose();
        return Task.CompletedTask;
    }

    [SkippableFact]
    public async Task FetchLatestAsync_ForAAPL_ReturnsValidQuote()
    {
        // Arrange
        Skip.If(!_hasValidKey, "FMP API key not available");
        var symbol = "AAPL";

        // Act
        var quote = await _provider.FetchLatestAsync(symbol);

        // Assert
        Assert.NotNull(quote);
        Assert.Equal(symbol, quote.Symbol);
        Assert.True(quote.Close > 0, "Close price should be greater than 0");
        Assert.True(quote.Open > 0, "Open price should be greater than 0");
        Assert.True(quote.High > 0, "High price should be greater than 0");
        Assert.True(quote.Low > 0, "Low price should be greater than 0");
        Assert.True(quote.High >= quote.Low, "High should be >= Low");
        Assert.True(quote.Volume > 0, "Volume should be > 0");
        Assert.Equal("FinancialModelingPrep", quote.Source);

        _logger.LogInformation(
            "Quote for {Symbol}: Open={Open}, High={High}, Low={Low}, Close={Close}, Volume={Volume:N0}, Timestamp={Timestamp}",
            quote.Symbol, quote.Open, quote.High, quote.Low, quote.Close, quote.Volume, quote.Timestamp);
    }

    [SkippableFact]
    public async Task FetchHistoricalAsync_ForAAPL_Daily_ReturnsCandles()
    {
        // Arrange
        Skip.If(!_hasValidKey, "FMP API key not available");
        var symbol = "AAPL";
        var endDate = DateTime.UtcNow;
        var startDate = endDate.AddDays(-30);

        // Act
        var candles = (await _provider.FetchHistoricalAsync(symbol, startDate, endDate, "1d")).ToList();

        // Assert
        Assert.NotEmpty(candles);
        Assert.True(candles.Count >= 20, $"Should have at least 20 trading days in 30 days, got {candles.Count}");

        // Validate first candle
        var firstCandle = candles.First();
        Assert.Equal(symbol, firstCandle.Symbol);
        Assert.True(firstCandle.Close > 0, "Close price should be > 0");
        Assert.True(firstCandle.Volume > 0, "Volume should be > 0");
        Assert.Equal("FinancialModelingPrep", firstCandle.Source);

        _logger.LogInformation(
            "Fetched {Count} daily candles for {Symbol} from {Start:yyyy-MM-dd} to {End:yyyy-MM-dd}",
            candles.Count, symbol, startDate, endDate);
    }

    [SkippableFact]
    public async Task GetCompanyProfileAsync_ForAAPL_ReturnsProfile()
    {
        // Arrange
        Skip.If(!_hasValidKey, "FMP API key not available");
        var symbol = "AAPL";

        // Act
        var profile = await _provider.GetCompanyProfileAsync(symbol);

        // Assert
        Assert.NotNull(profile);
        Assert.True(profile.Count > 0, "Profile should have data");

        var company = profile[0];
        var companyName = company["companyName"]?.ToString();
        var ticker = company["symbol"]?.ToString();
        var exchange = company["exchangeShortName"]?.ToString();
        var industry = company["industry"]?.ToString();
        var sector = company["sector"]?.ToString();
        var marketCap = company["mktCap"]?.ToString();
        var ceo = company["ceo"]?.ToString();

        Assert.NotNull(companyName);
        Assert.Equal(symbol, ticker);
        Assert.NotNull(exchange);

        _logger.LogInformation(
            "Company Profile for {Symbol}: Name={Name}, CEO={CEO}, Exchange={Exchange}, Industry={Industry}, Sector={Sector}, MarketCap={MarketCap}",
            symbol, companyName, ceo, exchange, industry, sector, marketCap);
    }

    [SkippableFact]
    public async Task GetIncomeStatementAsync_ForAAPL_Annual_ReturnsStatements()
    {
        // Arrange
        Skip.If(!_hasValidKey, "FMP API key not available");
        var symbol = "AAPL";

        // Act
        var statements = await _provider.GetIncomeStatementAsync(symbol, period: "annual", limit: 3);

        // Assert
        Assert.NotNull(statements);
        Assert.True(statements.Count > 0, "Should have income statements");

        var latest = statements[0];
        var date = latest["date"]?.ToString();
        var revenue = latest["revenue"]?.ToString();
        var netIncome = latest["netIncome"]?.ToString();
        var eps = latest["eps"]?.ToString();
        var epsDiluted = latest["epsdiluted"]?.ToString();

        Assert.NotNull(date);
        Assert.NotNull(revenue);
        Assert.NotNull(netIncome);

        _logger.LogInformation(
            "Income Statement for {Symbol} ({Date}): Revenue={Revenue}, NetIncome={NetIncome}, EPS={EPS}, EPSDiluted={EPSDiluted}",
            symbol, date, revenue, netIncome, eps, epsDiluted);
    }

    [SkippableFact]
    public async Task GetBalanceSheetAsync_ForAAPL_Annual_ReturnsStatements()
    {
        // Arrange
        Skip.If(!_hasValidKey, "FMP API key not available");
        var symbol = "AAPL";

        // Act
        var statements = await _provider.GetBalanceSheetAsync(symbol, period: "annual", limit: 3);

        // Assert
        Assert.NotNull(statements);
        Assert.True(statements.Count > 0, "Should have balance sheets");

        var latest = statements[0];
        var date = latest["date"]?.ToString();
        var totalAssets = latest["totalAssets"]?.ToString();
        var totalLiabilities = latest["totalLiabilities"]?.ToString();
        var totalEquity = latest["totalStockholdersEquity"]?.ToString();
        var cashAndEquivalents = latest["cashAndCashEquivalents"]?.ToString();

        Assert.NotNull(date);
        Assert.NotNull(totalAssets);

        _logger.LogInformation(
            "Balance Sheet for {Symbol} ({Date}): Assets={Assets}, Liabilities={Liabilities}, Equity={Equity}, Cash={Cash}",
            symbol, date, totalAssets, totalLiabilities, totalEquity, cashAndEquivalents);
    }

    [SkippableFact]
    public async Task GetCashFlowStatementAsync_ForAAPL_Annual_ReturnsStatements()
    {
        // Arrange
        Skip.If(!_hasValidKey, "FMP API key not available");
        var symbol = "AAPL";

        // Act
        var statements = await _provider.GetCashFlowStatementAsync(symbol, period: "annual", limit: 3);

        // Assert
        Assert.NotNull(statements);
        Assert.True(statements.Count > 0, "Should have cash flow statements");

        var latest = statements[0];
        var date = latest["date"]?.ToString();
        var operatingCashFlow = latest["operatingCashFlow"]?.ToString();
        var investingCashFlow = latest["netCashUsedForInvestingActivites"]?.ToString();
        var financingCashFlow = latest["netCashUsedProvidedByFinancingActivities"]?.ToString();
        var freeCashFlow = latest["freeCashFlow"]?.ToString();

        Assert.NotNull(date);
        Assert.NotNull(operatingCashFlow);

        _logger.LogInformation(
            "Cash Flow Statement for {Symbol} ({Date}): Operating={Operating}, Investing={Investing}, Financing={Financing}, FreeCashFlow={FreeCashFlow}",
            symbol, date, operatingCashFlow, investingCashFlow, financingCashFlow, freeCashFlow);
    }

    [SkippableFact]
    public async Task GetFinancialRatiosAsync_ForAAPL_Annual_ReturnsRatios()
    {
        // Arrange
        Skip.If(!_hasValidKey, "FMP API key not available");
        var symbol = "AAPL";

        // Act
        var ratios = await _provider.GetFinancialRatiosAsync(symbol, period: "annual", limit: 3);

        // Assert
        Assert.NotNull(ratios);
        Assert.True(ratios.Count > 0, "Should have financial ratios");

        var latest = ratios[0];
        var date = latest["date"]?.ToString();
        var peRatio = latest["priceEarningsRatio"]?.ToString();
        var pbRatio = latest["priceToBookRatio"]?.ToString();
        var roe = latest["returnOnEquity"]?.ToString();
        var roa = latest["returnOnAssets"]?.ToString();
        var debtToEquity = latest["debtEquityRatio"]?.ToString();
        var currentRatio = latest["currentRatio"]?.ToString();

        Assert.NotNull(date);

        _logger.LogInformation(
            "Financial Ratios for {Symbol} ({Date}): P/E={PE}, P/B={PB}, ROE={ROE}, ROA={ROA}, D/E={DE}, Current={Current}",
            symbol, date, peRatio, pbRatio, roe, roa, debtToEquity, currentRatio);
    }

    [SkippableFact]
    public async Task GetSocialSentimentAsync_ForAAPL_ReturnsSentiment()
    {
        // Arrange
        Skip.If(!_hasValidKey, "FMP API key not available");
        var symbol = "AAPL";

        // Act
        var sentiment = await _provider.GetSocialSentimentAsync(symbol, page: 0);

        // Assert
        Assert.NotNull(sentiment);
        Assert.True(sentiment.Count > 0, "Should have social sentiment data");

        var latest = sentiment[0];
        var date = latest["date"]?.ToString();
        var stocktwitsPosts = latest["stocktwitsPosts"]?.ToString();
        var twitterPosts = latest["twitterPosts"]?.ToString();
        var stocktwitsComments = latest["stocktwitsComments"]?.ToString();
        var twitterComments = latest["twitterComments"]?.ToString();
        var stocktwitsSentiment = latest["stocktwitsSentiment"]?.ToString();
        var twitterSentiment = latest["twitterSentiment"]?.ToString();

        Assert.NotNull(date);

        _logger.LogInformation(
            "Social Sentiment for {Symbol} ({Date}):\n" +
            "  StockTwits: Posts={STPosts}, Comments={STComments}, Sentiment={STSentiment}\n" +
            "  Twitter: Posts={TWPosts}, Comments={TWComments}, Sentiment={TWSentiment}",
            symbol, date, stocktwitsPosts, stocktwitsComments, stocktwitsSentiment,
            twitterPosts, twitterComments, twitterSentiment);
    }

    [SkippableFact]
    public async Task GetESGScoresAsync_ForAAPL_ReturnsESGData()
    {
        // Arrange
        Skip.If(!_hasValidKey, "FMP API key not available");
        var symbol = "AAPL";

        // Act
        var esg = await _provider.GetESGScoresAsync(symbol);

        // Assert
        Assert.NotNull(esg);
        Assert.True(esg.Count > 0, "Should have ESG data");

        var latest = esg[0];
        var environmentalScore = latest["environmentalScore"]?.ToString();
        var socialScore = latest["socialScore"]?.ToString();
        var governanceScore = latest["governanceScore"]?.ToString();
        var esgScore = latest["ESGScore"]?.ToString();

        _logger.LogInformation(
            "ESG Scores for {Symbol}: Environmental={E}, Social={S}, Governance={G}, Total={ESG}",
            symbol, environmentalScore, socialScore, governanceScore, esgScore);
    }

    [SkippableFact]
    public async Task GetInstitutionalHoldingsAsync_ForAAPL_ReturnsHoldings()
    {
        // Arrange
        Skip.If(!_hasValidKey, "FMP API key not available");
        var symbol = "AAPL";

        // Act
        var holdings = await _provider.GetInstitutionalHoldingsAsync(symbol);

        // Assert
        Assert.NotNull(holdings);
        Assert.True(holdings.Count > 0, "Should have institutional holdings");

        var topHolder = holdings[0];
        var holder = topHolder["holder"]?.ToString();
        var shares = topHolder["shares"]?.ToString();
        var dateReported = topHolder["dateReported"]?.ToString();
        var change = topHolder["change"]?.ToString();

        Assert.NotNull(holder);

        _logger.LogInformation(
            "Top Institutional Holder for {Symbol}: {Holder} holds {Shares} shares (reported: {Date}, change: {Change})",
            symbol, holder, shares, dateReported, change);

        // Log top 5 holders
        _logger.LogInformation("Top 5 institutional holders for {Symbol}:", symbol);
        for (int i = 0; i < Math.Min(5, holdings.Count); i++)
        {
            var h = holdings[i];
            _logger.LogInformation(
                "  {Index}. {Holder}: {Shares} shares",
                i + 1, h["holder"], h["shares"]);
        }
    }

    [SkippableFact]
    public async Task SupportsSymbolAsync_ForValidSymbol_ReturnsTrue()
    {
        // Arrange
        Skip.If(!_hasValidKey, "FMP API key not available");
        var symbol = "AAPL";

        // Act
        var supports = await _provider.SupportsSymbolAsync(symbol);

        // Assert
        Assert.True(supports);
    }

    [SkippableFact]
    public async Task GetCurrentUsageAsync_ReturnsUsageCount()
    {
        // Arrange
        Skip.If(!_hasValidKey, "FMP API key not available");

        // Act - Make a call first
        await _provider.FetchLatestAsync("AAPL");

        // Get usage
        var usage = await _provider.GetCurrentUsageAsync();

        // Assert
        Assert.True(usage > 0, "Usage should be > 0 after making API call");

        _logger.LogInformation("Current API usage: {Usage}/250 calls today", usage);
    }

    [SkippableFact]
    public async Task GetRemainingCallsAsync_ReturnsRemainingCount()
    {
        // Arrange
        Skip.If(!_hasValidKey, "FMP API key not available");

        // Act
        var remaining = await _provider.GetRemainingCallsAsync();

        // Assert
        Assert.NotNull(remaining);
        Assert.True(remaining.Value >= 0 && remaining.Value <= 250,
            $"Remaining calls should be 0-250 (daily limit), got {remaining.Value}");

        _logger.LogInformation("Remaining API calls today: {Remaining}/250", remaining);
    }

    [SkippableFact]
    public async Task DataQuality_FinancialStatements_ConsistentData()
    {
        // Arrange
        Skip.If(!_hasValidKey, "FMP API key not available");
        var symbol = "AAPL";

        // Act
        var income = await _provider.GetIncomeStatementAsync(symbol, period: "annual", limit: 1);
        var balance = await _provider.GetBalanceSheetAsync(symbol, period: "annual", limit: 1);
        var cashFlow = await _provider.GetCashFlowStatementAsync(symbol, period: "annual", limit: 1);

        // Assert - All should be for the same fiscal year
        Assert.NotNull(income);
        Assert.NotNull(balance);
        Assert.NotNull(cashFlow);

        var incomeDate = income[0]["date"]?.ToString();
        var balanceDate = balance[0]["date"]?.ToString();
        var cashFlowDate = cashFlow[0]["date"]?.ToString();

        _logger.LogInformation(
            "Financial Statement Dates for {Symbol}: Income={IncomeDate}, Balance={BalanceDate}, CashFlow={CashFlowDate}",
            symbol, incomeDate, balanceDate, cashFlowDate);

        // All three statements should be from the same fiscal year
        Assert.Equal(incomeDate, balanceDate);
        Assert.Equal(balanceDate, cashFlowDate);
    }
}
