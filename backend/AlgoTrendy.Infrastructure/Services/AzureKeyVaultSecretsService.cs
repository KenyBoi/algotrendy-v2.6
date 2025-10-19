using AlgoTrendy.Core.Configuration;
using AlgoTrendy.Core.Interfaces;
using Azure.Identity;
using Azure.Security.KeyVault.Secrets;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Collections.Concurrent;

namespace AlgoTrendy.Infrastructure.Services;

/// <summary>
/// Azure Key Vault implementation of the secrets service
/// </summary>
public class AzureKeyVaultSecretsService : ISecretsService
{
    private readonly SecretClient _secretClient;
    private readonly AzureKeyVaultSettings _settings;
    private readonly ILogger<AzureKeyVaultSecretsService> _logger;

    // Local cache for secrets (TTL-based)
    private readonly ConcurrentDictionary<string, CachedSecret> _cache = new();

    public AzureKeyVaultSecretsService(
        IOptions<AzureKeyVaultSettings> settings,
        ILogger<AzureKeyVaultSecretsService> logger)
    {
        _settings = settings?.Value ?? throw new ArgumentNullException(nameof(settings));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        // Validate settings
        var (isValid, errorMessage) = _settings.Validate();
        if (!isValid)
        {
            throw new InvalidOperationException($"Invalid Azure Key Vault configuration: {errorMessage}");
        }

        // Create appropriate credential based on configuration
        var credential = CreateCredential();

        // Initialize Secret Client
        _secretClient = new SecretClient(new Uri(_settings.KeyVaultUri), credential);

        _logger.LogInformation(
            "Azure Key Vault Secrets Service initialized. Vault: {KeyVaultUri}, UseManagedIdentity: {UseManagedIdentity}",
            _settings.KeyVaultUri, _settings.UseManagedIdentity);
    }

    /// <summary>
    /// Gets a secret value by name
    /// </summary>
    public async Task<string?> GetSecretAsync(string secretName, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(secretName))
        {
            throw new ArgumentException("Secret name cannot be empty", nameof(secretName));
        }

        // Check cache first
        if (_settings.CacheDurationMinutes > 0 && TryGetFromCache(secretName, out var cachedValue))
        {
            _logger.LogDebug("Retrieved secret {SecretName} from cache", secretName);
            return cachedValue;
        }

        try
        {
            _logger.LogDebug("Fetching secret {SecretName} from Azure Key Vault", secretName);

            var secret = await _secretClient.GetSecretAsync(secretName, cancellationToken: cancellationToken);
            var value = secret.Value.Value;

            // Cache the secret
            if (_settings.CacheDurationMinutes > 0)
            {
                CacheSecret(secretName, value);
            }

            _logger.LogInformation("Successfully retrieved secret {SecretName} from Azure Key Vault", secretName);
            return value;
        }
        catch (Azure.RequestFailedException ex) when (ex.Status == 404)
        {
            _logger.LogWarning("Secret {SecretName} not found in Azure Key Vault", secretName);
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to retrieve secret {SecretName} from Azure Key Vault", secretName);
            throw;
        }
    }

    /// <summary>
    /// Gets multiple secrets by name in a single batch operation
    /// </summary>
    public async Task<Dictionary<string, string>> GetSecretsAsync(
        IEnumerable<string> secretNames,
        CancellationToken cancellationToken = default)
    {
        var names = secretNames.ToList();
        var results = new Dictionary<string, string>();

        foreach (var secretName in names)
        {
            var value = await GetSecretAsync(secretName, cancellationToken);
            if (value != null)
            {
                results[secretName] = value;
            }
        }

        return results;
    }

    /// <summary>
    /// Sets a secret value in Azure Key Vault
    /// </summary>
    public async Task SetSecretAsync(string secretName, string secretValue, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(secretName))
        {
            throw new ArgumentException("Secret name cannot be empty", nameof(secretName));
        }

        if (secretValue == null)
        {
            throw new ArgumentNullException(nameof(secretValue));
        }

        try
        {
            _logger.LogInformation("Setting secret {SecretName} in Azure Key Vault", secretName);

            await _secretClient.SetSecretAsync(secretName, secretValue, cancellationToken);

            // Update cache
            if (_settings.CacheDurationMinutes > 0)
            {
                CacheSecret(secretName, secretValue);
            }

            _logger.LogInformation("Successfully set secret {SecretName} in Azure Key Vault", secretName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to set secret {SecretName} in Azure Key Vault", secretName);
            throw;
        }
    }

    /// <summary>
    /// Checks if a secret exists
    /// </summary>
    public async Task<bool> SecretExistsAsync(string secretName, CancellationToken cancellationToken = default)
    {
        var value = await GetSecretAsync(secretName, cancellationToken);
        return value != null;
    }

    /// <summary>
    /// Clears the local cache
    /// </summary>
    public void ClearCache()
    {
        _cache.Clear();
        _logger.LogInformation("Secrets cache cleared");
    }

    #region Private Helper Methods

    /// <summary>
    /// Creates the appropriate Azure credential based on configuration
    /// </summary>
    private Azure.Core.TokenCredential CreateCredential()
    {
        if (_settings.UseManagedIdentity)
        {
            _logger.LogInformation("Using Managed Identity for Azure Key Vault authentication");

            // Try Managed Identity first, fall back to Azure CLI, then Visual Studio, then Environment
            return new DefaultAzureCredential(new DefaultAzureCredentialOptions
            {
                ExcludeInteractiveBrowserCredential = true,
                ExcludeSharedTokenCacheCredential = true
            });
        }
        else
        {
            _logger.LogInformation(
                "Using Service Principal for Azure Key Vault authentication (TenantId: {TenantId}, ClientId: {ClientId})",
                _settings.TenantId, _settings.ClientId);

            return new ClientSecretCredential(
                _settings.TenantId,
                _settings.ClientId,
                _settings.ClientSecret);
        }
    }

    /// <summary>
    /// Attempts to retrieve a secret from the cache
    /// </summary>
    private bool TryGetFromCache(string secretName, out string? value)
    {
        if (_cache.TryGetValue(secretName, out var cached))
        {
            if (cached.ExpiresAt > DateTime.UtcNow)
            {
                value = cached.Value;
                return true;
            }

            // Expired - remove from cache
            _cache.TryRemove(secretName, out _);
        }

        value = null;
        return false;
    }

    /// <summary>
    /// Caches a secret value with TTL
    /// </summary>
    private void CacheSecret(string secretName, string value)
    {
        var expiresAt = DateTime.UtcNow.AddMinutes(_settings.CacheDurationMinutes);
        _cache[secretName] = new CachedSecret(value, expiresAt);

        _logger.LogDebug(
            "Cached secret {SecretName} until {ExpiresAt}",
            secretName, expiresAt);
    }

    /// <summary>
    /// Represents a cached secret with expiration
    /// </summary>
    private record CachedSecret(string Value, DateTime ExpiresAt);

    #endregion
}
