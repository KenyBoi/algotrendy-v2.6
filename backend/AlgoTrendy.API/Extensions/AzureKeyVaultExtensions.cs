using AlgoTrendy.Core.Configuration;
using AlgoTrendy.Core.Interfaces;
using AlgoTrendy.Infrastructure.Services;
using Azure.Extensions.AspNetCore.Configuration.Secrets;
using Azure.Identity;
using Azure.Security.KeyVault.Secrets;

namespace AlgoTrendy.API.Extensions;

/// <summary>
/// Extension methods for integrating Azure Key Vault into the application
/// </summary>
public static class AzureKeyVaultExtensions
{
    /// <summary>
    /// Adds Azure Key Vault as a configuration source and registers the secrets service
    /// </summary>
    /// <param name="builder">The web application builder</param>
    /// <returns>The web application builder for chaining</returns>
    public static WebApplicationBuilder AddAzureKeyVault(this WebApplicationBuilder builder)
    {
        // Check if Azure Key Vault is configured
        var keyVaultSettings = builder.Configuration
            .GetSection("AzureKeyVault")
            .Get<AzureKeyVaultSettings>();

        if (keyVaultSettings == null || string.IsNullOrWhiteSpace(keyVaultSettings.KeyVaultUri))
        {
            builder.Services.AddSingleton<ILogger>(sp =>
            {
                var loggerFactory = sp.GetRequiredService<ILoggerFactory>();
                return loggerFactory.CreateLogger("AzureKeyVault");
            });

            var logger = builder.Services.BuildServiceProvider()
                .GetRequiredService<ILogger<WebApplicationBuilder>>();

            logger.LogWarning(
                "Azure Key Vault is not configured. Secrets service will not be available. " +
                "Set AzureKeyVault:KeyVaultUri in appsettings.json or environment variables to enable.");

            return builder;
        }

        // Validate settings
        var (isValid, errorMessage) = keyVaultSettings.Validate();
        if (!isValid)
        {
            throw new InvalidOperationException(
                $"Invalid Azure Key Vault configuration: {errorMessage}");
        }

        // Add Azure Key Vault to configuration
        try
        {
            Azure.Core.TokenCredential credential;
            if (keyVaultSettings.UseManagedIdentity)
            {
                credential = new DefaultAzureCredential(new DefaultAzureCredentialOptions
                {
                    ExcludeInteractiveBrowserCredential = true,
                    ExcludeSharedTokenCacheCredential = true
                });
            }
            else
            {
                credential = new ClientSecretCredential(
                    keyVaultSettings.TenantId,
                    keyVaultSettings.ClientId,
                    keyVaultSettings.ClientSecret);
            }

            var secretClient = new SecretClient(
                new Uri(keyVaultSettings.KeyVaultUri),
                credential);

            // Add Key Vault secrets to configuration
            builder.Configuration.AddAzureKeyVault(
                secretClient,
                new AzureKeyVaultConfigurationOptions
                {
                    ReloadInterval = TimeSpan.FromMinutes(keyVaultSettings.CacheDurationMinutes)
                });

            builder.Services.AddSingleton<ILogger>(sp =>
            {
                var loggerFactory = sp.GetRequiredService<ILoggerFactory>();
                return loggerFactory.CreateLogger("AzureKeyVault");
            });

            var logger = builder.Services.BuildServiceProvider()
                .GetRequiredService<ILogger<WebApplicationBuilder>>();

            logger.LogInformation(
                "Azure Key Vault integrated as configuration source. Vault: {KeyVaultUri}",
                keyVaultSettings.KeyVaultUri);
        }
        catch (Exception ex)
        {
            var logger = builder.Services.BuildServiceProvider()
                .GetRequiredService<ILogger<WebApplicationBuilder>>();

            logger.LogError(ex,
                "Failed to initialize Azure Key Vault. Falling back to local configuration.");

            // Don't throw - allow app to start with local configuration
        }

        // Register the secrets service
        builder.Services.Configure<AzureKeyVaultSettings>(
            builder.Configuration.GetSection("AzureKeyVault"));

        builder.Services.AddSingleton<ISecretsService, AzureKeyVaultSecretsService>();

        return builder;
    }

    /// <summary>
    /// Loads broker API credentials from Azure Key Vault
    /// </summary>
    /// <param name="builder">The web application builder</param>
    /// <param name="brokerName">Name of the broker (e.g., "binance", "okx")</param>
    /// <returns>The web application builder for chaining</returns>
    public static async Task<WebApplicationBuilder> LoadBrokerCredentialsAsync(
        this WebApplicationBuilder builder,
        string brokerName)
    {
        var secretsService = builder.Services.BuildServiceProvider()
            .GetService<ISecretsService>();

        if (secretsService == null)
        {
            var logger = builder.Services.BuildServiceProvider()
                .GetRequiredService<ILogger<WebApplicationBuilder>>();

            logger.LogWarning(
                "Secrets service not available. Cannot load credentials for broker: {BrokerName}",
                brokerName);

            return builder;
        }

        try
        {
            var secretNames = new[]
            {
                $"{brokerName}-api-key",
                $"{brokerName}-api-secret"
            };

            var secrets = await secretsService.GetSecretsAsync(secretNames);

            if (secrets.TryGetValue($"{brokerName}-api-key", out var apiKey))
            {
                builder.Configuration[$"{brokerName}:ApiKey"] = apiKey;
            }

            if (secrets.TryGetValue($"{brokerName}-api-secret", out var apiSecret))
            {
                builder.Configuration[$"{brokerName}:ApiSecret"] = apiSecret;
            }

            var logger = builder.Services.BuildServiceProvider()
                .GetRequiredService<ILogger<WebApplicationBuilder>>();

            logger.LogInformation(
                "Loaded credentials for broker {BrokerName} from Azure Key Vault",
                brokerName);
        }
        catch (Exception ex)
        {
            var logger = builder.Services.BuildServiceProvider()
                .GetRequiredService<ILogger<WebApplicationBuilder>>();

            logger.LogError(ex,
                "Failed to load credentials for broker {BrokerName} from Azure Key Vault",
                brokerName);
        }

        return builder;
    }
}
