namespace AlgoTrendy.Core.Interfaces;

/// <summary>
/// Service for retrieving secrets from Azure Key Vault or other secret stores
/// </summary>
public interface ISecretsService
{
    /// <summary>
    /// Gets a secret value by name
    /// </summary>
    /// <param name="secretName">Name of the secret</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Secret value, or null if not found</returns>
    Task<string?> GetSecretAsync(string secretName, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets multiple secrets by name in a single batch operation
    /// </summary>
    /// <param name="secretNames">Names of the secrets to retrieve</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Dictionary of secret names to values</returns>
    Task<Dictionary<string, string>> GetSecretsAsync(IEnumerable<string> secretNames, CancellationToken cancellationToken = default);

    /// <summary>
    /// Sets a secret value (if supported by the backing store)
    /// </summary>
    /// <param name="secretName">Name of the secret</param>
    /// <param name="secretValue">Value to store</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task SetSecretAsync(string secretName, string secretValue, CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if a secret exists
    /// </summary>
    /// <param name="secretName">Name of the secret</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if secret exists</returns>
    Task<bool> SecretExistsAsync(string secretName, CancellationToken cancellationToken = default);

    /// <summary>
    /// Clears the local cache (if caching is enabled)
    /// </summary>
    void ClearCache();
}
