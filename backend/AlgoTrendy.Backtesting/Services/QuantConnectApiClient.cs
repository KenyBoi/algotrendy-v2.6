using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using AlgoTrendy.Backtesting.Models.QuantConnect;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace AlgoTrendy.Backtesting.Services;

/// <summary>
/// Configuration for QuantConnect API
/// </summary>
public class QuantConnectConfig
{
    /// <summary>
    /// QuantConnect User ID
    /// </summary>
    public string UserId { get; set; } = string.Empty;

    /// <summary>
    /// QuantConnect API Token
    /// </summary>
    public string ApiToken { get; set; } = string.Empty;

    /// <summary>
    /// Base URL for QuantConnect API (default: https://www.quantconnect.com/api/v2)
    /// </summary>
    public string BaseUrl { get; set; } = "https://www.quantconnect.com/api/v2";

    /// <summary>
    /// Default project ID for backtests (optional)
    /// </summary>
    public int? DefaultProjectId { get; set; }

    /// <summary>
    /// Request timeout in seconds
    /// </summary>
    public int TimeoutSeconds { get; set; } = 300;
}

/// <summary>
/// Implementation of QuantConnect REST API client
/// </summary>
public class QuantConnectApiClient : IQuantConnectApiClient
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<QuantConnectApiClient> _logger;
    private readonly QuantConnectConfig _config;
    private readonly JsonSerializerOptions _jsonOptions;

    public QuantConnectApiClient(
        HttpClient httpClient,
        ILogger<QuantConnectApiClient> logger,
        IOptions<QuantConnectConfig> config)
    {
        _httpClient = httpClient;
        _logger = logger;
        _config = config.Value;

        _httpClient.BaseAddress = new Uri(_config.BaseUrl);
        _httpClient.Timeout = TimeSpan.FromSeconds(_config.TimeoutSeconds);

        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            WriteIndented = false
        };
    }

    /// <inheritdoc/>
    public async Task<bool> AuthenticateAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Authenticating with QuantConnect API");

            // Test authentication by reading account
            var request = new HttpRequestMessage(HttpMethod.Post, "/account/read");
            AddAuthenticationHeaders(request);

            var response = await _httpClient.SendAsync(request, cancellationToken);
            var content = await response.Content.ReadAsStringAsync(cancellationToken);

            if (response.IsSuccessStatusCode)
            {
                _logger.LogInformation("QuantConnect authentication successful");
                return true;
            }

            _logger.LogError("QuantConnect authentication failed: {StatusCode} - {Content}",
                response.StatusCode, content);
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error authenticating with QuantConnect");
            return false;
        }
    }

    /// <inheritdoc/>
    public async Task<QCProjectResponse> CreateProjectAsync(
        string name,
        string language = "CSharp",
        CancellationToken cancellationToken = default)
    {
        var requestBody = new
        {
            name,
            language
        };

        return await PostAsync<QCProjectResponse>("/projects/create", requestBody, cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<QCProjectResponse> ReadProjectAsync(
        int projectId,
        CancellationToken cancellationToken = default)
    {
        var requestBody = new
        {
            projectId
        };

        return await PostAsync<QCProjectResponse>("/projects/read", requestBody, cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<bool> CreateOrUpdateFileAsync(
        int projectId,
        string fileName,
        string fileContent,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var requestBody = new
            {
                projectId,
                name = fileName,
                content = fileContent
            };

            var response = await PostAsync<QCBaseResponse>("/files/create", requestBody, cancellationToken);
            return response.Success;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating/updating file {FileName} in project {ProjectId}",
                fileName, projectId);
            return false;
        }
    }

    /// <inheritdoc/>
    public async Task<QCCompileResponse> CompileProjectAsync(
        int projectId,
        CancellationToken cancellationToken = default)
    {
        var requestBody = new
        {
            projectId
        };

        return await PostAsync<QCCompileResponse>("/compile/create", requestBody, cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<QCCompileResponse> ReadCompileAsync(
        int projectId,
        string compileId,
        CancellationToken cancellationToken = default)
    {
        var requestBody = new
        {
            projectId,
            compileId
        };

        return await PostAsync<QCCompileResponse>("/compile/read", requestBody, cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<QCBacktestResponse> CreateBacktestAsync(
        int projectId,
        string compileId,
        string backtestName,
        Dictionary<string, object>? parameters = null,
        CancellationToken cancellationToken = default)
    {
        var requestBody = new
        {
            projectId,
            compileId,
            backtestName,
            parameters = parameters ?? new Dictionary<string, object>()
        };

        return await PostAsync<QCBacktestResponse>("/backtests/create", requestBody, cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<QCBacktestResponse> ReadBacktestAsync(
        int projectId,
        string backtestId,
        CancellationToken cancellationToken = default)
    {
        var requestBody = new
        {
            projectId,
            backtestId
        };

        return await PostAsync<QCBacktestResponse>("/backtests/read", requestBody, cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<QCBacktestListResponse> ListBacktestsAsync(
        int projectId,
        CancellationToken cancellationToken = default)
    {
        var requestBody = new
        {
            projectId
        };

        return await PostAsync<QCBacktestListResponse>("/backtests/list", requestBody, cancellationToken);
    }

    /// <summary>
    /// Make authenticated POST request to QuantConnect API
    /// </summary>
    private async Task<T> PostAsync<T>(
        string endpoint,
        object requestBody,
        CancellationToken cancellationToken) where T : QCBaseResponse, new()
    {
        try
        {
            var request = new HttpRequestMessage(HttpMethod.Post, endpoint);
            AddAuthenticationHeaders(request);

            var jsonContent = JsonSerializer.Serialize(requestBody, _jsonOptions);
            request.Content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

            _logger.LogDebug("Sending request to {Endpoint}: {Body}", endpoint, jsonContent);

            var response = await _httpClient.SendAsync(request, cancellationToken);
            var content = await response.Content.ReadAsStringAsync(cancellationToken);

            _logger.LogDebug("Response from {Endpoint}: {StatusCode} - {Content}",
                endpoint, response.StatusCode, content);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError("QuantConnect API error: {StatusCode} - {Content}",
                    response.StatusCode, content);
                return new T
                {
                    Success = false,
                    Errors = new List<string> { $"HTTP {response.StatusCode}: {content}" }
                };
            }

            var result = JsonSerializer.Deserialize<T>(content, _jsonOptions);
            if (result == null)
            {
                return new T
                {
                    Success = false,
                    Errors = new List<string> { "Failed to deserialize response" }
                };
            }

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calling QuantConnect API endpoint {Endpoint}", endpoint);
            return new T
            {
                Success = false,
                Errors = new List<string> { ex.Message }
            };
        }
    }

    /// <summary>
    /// Add authentication headers to request using QuantConnect's nonce token auth
    /// </summary>
    private void AddAuthenticationHeaders(HttpRequestMessage request)
    {
        // QuantConnect authentication: SHA256(apiToken + timestamp) as password, userId as username
        var timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString();
        var hashedToken = CreateHash(_config.ApiToken + timestamp);

        var credentials = Convert.ToBase64String(
            Encoding.ASCII.GetBytes($"{_config.UserId}:{hashedToken}"));

        request.Headers.Authorization = new AuthenticationHeaderValue("Basic", credentials);
        request.Headers.Add("Timestamp", timestamp);
    }

    /// <summary>
    /// Create SHA256 hash of input string
    /// </summary>
    private static string CreateHash(string input)
    {
        using var sha256 = SHA256.Create();
        var bytes = Encoding.UTF8.GetBytes(input);
        var hash = sha256.ComputeHash(bytes);
        return Convert.ToBase64String(hash);
    }
}
