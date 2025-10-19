namespace AlgoTrendy.Core.Configuration;

/// <summary>
/// Configuration settings for Azure Key Vault integration
/// </summary>
public class AzureKeyVaultSettings
{
    /// <summary>
    /// Key Vault URI (e.g., https://algotrendy-vault.vault.azure.net/)
    /// </summary>
    public required string KeyVaultUri { get; set; }

    /// <summary>
    /// Azure AD Tenant ID for authentication
    /// </summary>
    public string? TenantId { get; set; }

    /// <summary>
    /// Client ID for service principal authentication
    /// </summary>
    public string? ClientId { get; set; }

    /// <summary>
    /// Client secret for service principal authentication
    /// Leave empty to use Managed Identity or Azure CLI credentials
    /// </summary>
    public string? ClientSecret { get; set; }

    /// <summary>
    /// Whether to use Managed Identity (default: true in production)
    /// </summary>
    public bool UseManagedIdentity { get; set; } = true;

    /// <summary>
    /// Cache secrets locally for this duration (in minutes)
    /// Set to 0 to disable caching (default: 60 minutes)
    /// </summary>
    public int CacheDurationMinutes { get; set; } = 60;

    /// <summary>
    /// Validates the configuration
    /// </summary>
    public (bool IsValid, string? ErrorMessage) Validate()
    {
        if (string.IsNullOrWhiteSpace(KeyVaultUri))
        {
            return (false, "KeyVaultUri is required");
        }

        if (!Uri.TryCreate(KeyVaultUri, UriKind.Absolute, out var uri))
        {
            return (false, "KeyVaultUri must be a valid URI");
        }

        if (!uri.Host.EndsWith(".vault.azure.net", StringComparison.OrdinalIgnoreCase))
        {
            return (false, "KeyVaultUri must be a valid Azure Key Vault URI (*.vault.azure.net)");
        }

        if (!UseManagedIdentity)
        {
            if (string.IsNullOrWhiteSpace(TenantId))
            {
                return (false, "TenantId is required when not using Managed Identity");
            }

            if (string.IsNullOrWhiteSpace(ClientId))
            {
                return (false, "ClientId is required when not using Managed Identity");
            }

            if (string.IsNullOrWhiteSpace(ClientSecret))
            {
                return (false, "ClientSecret is required when not using Managed Identity");
            }
        }

        return (true, null);
    }
}
