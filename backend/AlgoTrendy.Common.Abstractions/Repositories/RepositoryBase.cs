using Microsoft.Extensions.Logging;
using Npgsql;
using System.Data;

namespace AlgoTrendy.Common.Abstractions.Repositories;

/// <summary>
/// Base class for all repository implementations.
/// Provides common database connection and command management.
/// Eliminates ~100 lines of duplicate connection/command setup code across repositories.
/// </summary>
public abstract class RepositoryBase
{
    protected readonly string ConnectionString;
    protected readonly ILogger Logger;

    protected RepositoryBase(string connectionString, ILogger logger)
    {
        ConnectionString = connectionString ?? throw new ArgumentNullException(nameof(connectionString));
        Logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    #region Connection Management

    /// <summary>
    /// Creates and opens a new database connection.
    /// </summary>
    protected async Task<NpgsqlConnection> CreateConnectionAsync(CancellationToken cancellationToken = default)
    {
        var connection = new NpgsqlConnection(ConnectionString);
        await connection.OpenAsync(cancellationToken);
        return connection;
    }

    /// <summary>
    /// Creates a command with the specified SQL and connection.
    /// </summary>
    protected NpgsqlCommand CreateCommand(string sql, NpgsqlConnection connection)
    {
        return new NpgsqlCommand(sql, connection);
    }

    #endregion

    #region Execute Helpers

    /// <summary>
    /// Executes a non-query command and returns the number of affected rows.
    /// Handles connection/command lifecycle automatically.
    /// </summary>
    protected async Task<int> ExecuteNonQueryAsync(
        string sql,
        Action<NpgsqlCommand>? addParameters = null,
        CancellationToken cancellationToken = default)
    {
        await using var connection = await CreateConnectionAsync(cancellationToken);
        await using var command = CreateCommand(sql, connection);

        addParameters?.Invoke(command);

        return await command.ExecuteNonQueryAsync(cancellationToken);
    }

    /// <summary>
    /// Executes a scalar query and returns a single value.
    /// Handles connection/command lifecycle automatically.
    /// </summary>
    protected async Task<T?> ExecuteScalarAsync<T>(
        string sql,
        Action<NpgsqlCommand>? addParameters = null,
        CancellationToken cancellationToken = default)
    {
        await using var connection = await CreateConnectionAsync(cancellationToken);
        await using var command = CreateCommand(sql, connection);

        addParameters?.Invoke(command);

        var result = await command.ExecuteScalarAsync(cancellationToken);

        if (result == null || result == DBNull.Value)
            return default;

        return (T)result;
    }

    /// <summary>
    /// Executes a reader query and maps results using the provided function.
    /// Handles connection/command/reader lifecycle automatically.
    /// </summary>
    protected async Task<List<T>> ExecuteReaderAsync<T>(
        string sql,
        Func<NpgsqlDataReader, T> mapFunction,
        Action<NpgsqlCommand>? addParameters = null,
        CancellationToken cancellationToken = default)
    {
        var results = new List<T>();

        await using var connection = await CreateConnectionAsync(cancellationToken);
        await using var command = CreateCommand(sql, connection);

        addParameters?.Invoke(command);

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);

        while (await reader.ReadAsync(cancellationToken))
        {
            results.Add(mapFunction(reader));
        }

        return results;
    }

    /// <summary>
    /// Executes a reader query and maps the first result using the provided function.
    /// Returns null if no results found.
    /// </summary>
    protected async Task<T?> ExecuteReaderSingleAsync<T>(
        string sql,
        Func<NpgsqlDataReader, T> mapFunction,
        Action<NpgsqlCommand>? addParameters = null,
        CancellationToken cancellationToken = default) where T : class
    {
        await using var connection = await CreateConnectionAsync(cancellationToken);
        await using var command = CreateCommand(sql, connection);

        addParameters?.Invoke(command);

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);

        if (await reader.ReadAsync(cancellationToken))
        {
            return mapFunction(reader);
        }

        return null;
    }

    /// <summary>
    /// Executes multiple commands in a transaction.
    /// Automatically commits on success or rolls back on failure.
    /// </summary>
    protected async Task<bool> ExecuteTransactionAsync(
        Func<NpgsqlConnection, NpgsqlTransaction, Task> transactionAction,
        CancellationToken cancellationToken = default)
    {
        await using var connection = await CreateConnectionAsync(cancellationToken);
        await using var transaction = await connection.BeginTransactionAsync(cancellationToken);

        try
        {
            await transactionAction(connection, transaction);
            await transaction.CommitAsync(cancellationToken);
            return true;
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync(cancellationToken);
            Logger.LogError(ex, "Transaction failed and was rolled back");
            throw;
        }
    }

    #endregion

    #region Parameter Helpers

    /// <summary>
    /// Adds a parameter to the command with null handling.
    /// </summary>
    protected void AddParameter<T>(NpgsqlCommand command, string name, T? value)
    {
        command.Parameters.AddWithValue(name, (object?)value ?? DBNull.Value);
    }

    /// <summary>
    /// Adds multiple parameters from a dictionary.
    /// </summary>
    protected void AddParameters(NpgsqlCommand command, Dictionary<string, object?> parameters)
    {
        foreach (var (name, value) in parameters)
        {
            AddParameter(command, name, value);
        }
    }

    #endregion

    #region Reader Helpers

    /// <summary>
    /// Safely reads a nullable value from the data reader.
    /// </summary>
    protected T? GetValueOrNull<T>(NpgsqlDataReader reader, string columnName) where T : struct
    {
        var ordinal = reader.GetOrdinal(columnName);
        return reader.IsDBNull(ordinal) ? null : reader.GetFieldValue<T>(ordinal);
    }

    /// <summary>
    /// Safely reads a nullable reference type from the data reader.
    /// </summary>
    protected string? GetStringOrNull(NpgsqlDataReader reader, string columnName)
    {
        var ordinal = reader.GetOrdinal(columnName);
        return reader.IsDBNull(ordinal) ? null : reader.GetString(ordinal);
    }

    /// <summary>
    /// Safely reads a value with a default if null.
    /// </summary>
    protected T GetValueOrDefault<T>(NpgsqlDataReader reader, string columnName, T defaultValue) where T : struct
    {
        var ordinal = reader.GetOrdinal(columnName);
        return reader.IsDBNull(ordinal) ? defaultValue : reader.GetFieldValue<T>(ordinal);
    }

    #endregion

    #region Logging Helpers

    /// <summary>
    /// Logs database operation with execution time.
    /// </summary>
    protected async Task<T> LogExecutionTimeAsync<T>(string operation, Func<Task<T>> action)
    {
        var startTime = DateTime.UtcNow;
        try
        {
            var result = await action();
            var elapsed = (DateTime.UtcNow - startTime).TotalMilliseconds;
            Logger.LogDebug("{Operation} completed in {ElapsedMs}ms", operation, elapsed);
            return result;
        }
        catch (Exception ex)
        {
            var elapsed = (DateTime.UtcNow - startTime).TotalMilliseconds;
            Logger.LogError(ex, "{Operation} failed after {ElapsedMs}ms", operation, elapsed);
            throw;
        }
    }

    #endregion
}
