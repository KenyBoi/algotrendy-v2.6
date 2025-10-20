using System.Text.Json;
using AlgoTrendy.Core.Configuration;
using AlgoTrendy.Core.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Npgsql;

namespace AlgoTrendy.Infrastructure.Services;

/// <summary>
/// OFAC (Office of Foreign Assets Control) sanctions screening service
/// Screens users and transactions against SDN (Specially Designated Nationals) list
/// </summary>
public class OFACScreeningService
{
    private readonly ILogger<OFACScreeningService> _logger;
    private readonly ComplianceSettings _complianceSettings;
    private readonly string _connectionString;
    private readonly HttpClient _httpClient;
    private DateTime _lastOFACUpdate = DateTime.MinValue;

    public OFACScreeningService(
        ILogger<OFACScreeningService> logger,
        IOptions<ComplianceSettings> complianceSettings,
        IConfiguration configuration,
        IHttpClientFactory httpClientFactory)
    {
        _logger = logger;
        _complianceSettings = complianceSettings.Value;
        _connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("Database connection string not configured");
        _httpClient = httpClientFactory.CreateClient();
    }

    /// <summary>
    /// Screen a user against OFAC sanctions lists
    /// </summary>
    public async Task<OFACScreeningResult> ScreenUserAsync(User user, CancellationToken cancellationToken = default)
    {
        if (!_complianceSettings.OFAC.Enabled)
        {
            return new OFACScreeningResult { IsMatch = false, ScreeningPerformed = false };
        }

        try
        {
            _logger.LogInformation("Screening user {UserId} ({FullName}) against OFAC sanctions list",
                user.UserId, user.FullName);

            // Ensure OFAC list is current
            await RefreshOFACListIfNeededAsync(cancellationToken);

            // Check name matches
            var matches = await FindOFACMatchesAsync(user.FullName, user.DateOfBirth, user.Country, cancellationToken);

            var result = new OFACScreeningResult
            {
                ScreeningPerformed = true,
                ScreeningDate = DateTime.UtcNow,
                UserId = user.UserId,
                ScreenedName = user.FullName,
                IsMatch = matches.Any(),
                Matches = matches,
                MatchScore = matches.Any() ? matches.Max(m => m.MatchScore) : 0
            };

            // Log compliance event
            if (result.IsMatch)
            {
                await LogComplianceEventAsync(
                    ComplianceEventType.OFACSanctionsMatch,
                    ComplianceSeverity.Critical,
                    user.UserId,
                    $"OFAC sanctions match detected for user {user.FullName}",
                    JsonSerializer.Serialize(result),
                    cancellationToken);

                _logger.LogWarning("OFAC SANCTIONS MATCH: User {UserId} ({FullName}) matched {MatchCount} entries with score {Score}",
                    user.UserId, user.FullName, matches.Count, result.MatchScore);
            }

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error screening user {UserId} against OFAC list", user.UserId);

            await LogComplianceEventAsync(
                ComplianceEventType.OFACScreeningFailed,
                ComplianceSeverity.High,
                user.UserId,
                $"OFAC screening failed for user {user.FullName}",
                ex.Message,
                cancellationToken);

            throw;
        }
    }

    /// <summary>
    /// Screen a trade/transaction for sanctions compliance
    /// </summary>
    public async Task<bool> ScreenTradeAsync(Guid userId, string symbol, decimal amount, CancellationToken cancellationToken = default)
    {
        if (!_complianceSettings.OFAC.ScreenAllTrades)
        {
            return true; // Pass through
        }

        try
        {
            // Get user information
            await using var connection = new NpgsqlConnection(_connectionString);
            await connection.OpenAsync(cancellationToken);

            var user = await GetUserByIdAsync(connection, userId, cancellationToken);
            if (user == null)
            {
                _logger.LogWarning("Cannot screen trade: User {UserId} not found", userId);
                return false;
            }

            // Check if user is sanctioned
            if (user.IsSanctioned)
            {
                _logger.LogWarning("Trade blocked: User {UserId} is sanctioned", userId);
                return false;
            }

            // Check if screening is recent (within 24 hours)
            if (user.LastSanctionsCheck.HasValue &&
                (DateTime.UtcNow - user.LastSanctionsCheck.Value).TotalHours < 24)
            {
                return true;
            }

            // Perform fresh screening
            var result = await ScreenUserAsync(user, cancellationToken);

            if (result.IsMatch && _complianceSettings.OFAC.BlockSanctionedOrders)
            {
                _logger.LogWarning("Trade blocked: User {UserId} matched OFAC sanctions list", userId);
                return false;
            }

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error screening trade for user {UserId}", userId);

            // Fail-safe: block trade if screening fails
            return !_complianceSettings.OFAC.BlockSanctionedOrders;
        }
    }

    /// <summary>
    /// Refresh OFAC SDN list from Treasury.gov
    /// </summary>
    public async Task RefreshOFACListIfNeededAsync(CancellationToken cancellationToken = default)
    {
        if (!_complianceSettings.OFAC.Enabled)
        {
            return;
        }

        var hoursSinceUpdate = (DateTime.UtcNow - _lastOFACUpdate).TotalHours;
        if (hoursSinceUpdate < _complianceSettings.OFAC.RefreshIntervalHours)
        {
            return; // List is current
        }

        try
        {
            _logger.LogInformation("Refreshing OFAC SDN list from {Url}", _complianceSettings.OFAC.SDNListUrl);

            var response = await _httpClient.GetAsync(_complianceSettings.OFAC.SDNListUrl, cancellationToken);
            response.EnsureSuccessStatusCode();

            var csvContent = await response.Content.ReadAsStringAsync(cancellationToken);
            var entries = ParseOFACCSV(csvContent);

            await using var connection = new NpgsqlConnection(_connectionString);
            await connection.OpenAsync(cancellationToken);

            // Clear old data
            await using (var cmd = new NpgsqlCommand("DELETE FROM ofac_sanctions_list", connection))
            {
                await cmd.ExecuteNonQueryAsync(cancellationToken);
            }

            // Insert new data
            var insertedCount = 0;
            foreach (var entry in entries)
            {
                await InsertOFACEntryAsync(connection, entry, cancellationToken);
                insertedCount++;
            }

            _lastOFACUpdate = DateTime.UtcNow;

            _logger.LogInformation("OFAC SDN list refreshed: {Count} entries loaded", insertedCount);

            await LogComplianceEventAsync(
                ComplianceEventType.OFACListUpdated,
                ComplianceSeverity.Info,
                null,
                $"OFAC SDN list updated with {insertedCount} entries",
                null,
                cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to refresh OFAC SDN list");
            throw;
        }
    }

    /// <summary>
    /// Find OFAC matches for a given name
    /// </summary>
    private async Task<List<OFACMatch>> FindOFACMatchesAsync(
        string fullName,
        DateTime? dateOfBirth,
        string? country,
        CancellationToken cancellationToken)
    {
        var matches = new List<OFACMatch>();

        await using var connection = new NpgsqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);

        // Fuzzy name matching using PostgreSQL similarity
        var sql = @"
            SELECT entry_id, entity_number, full_name, aliases, program,
                   date_of_birth, nationality, citizenship,
                   similarity(full_name, @name) * 100 as match_score
            FROM ofac_sanctions_list
            WHERE similarity(full_name, @name) > @minScore / 100.0
               OR aliases ILIKE '%' || @name || '%'
            ORDER BY match_score DESC
            LIMIT 10";

        await using var cmd = new NpgsqlCommand(sql, connection);
        cmd.Parameters.AddWithValue("name", fullName);
        cmd.Parameters.AddWithValue("minScore", _complianceSettings.OFAC.MinimumMatchScore);

        await using var reader = await cmd.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            var match = new OFACMatch
            {
                EntryId = reader.GetInt64(0),
                EntityNumber = reader.IsDBNull(1) ? null : reader.GetString(1),
                FullName = reader.GetString(2),
                Aliases = reader.IsDBNull(3) ? null : reader.GetString(3),
                Program = reader.IsDBNull(4) ? null : reader.GetString(4),
                DateOfBirth = reader.IsDBNull(5) ? null : reader.GetDateTime(5),
                Nationality = reader.IsDBNull(6) ? null : reader.GetString(6),
                Citizenship = reader.IsDBNull(7) ? null : reader.GetString(7),
                MatchScore = (int)reader.GetDouble(8)
            };

            // Boost score if date of birth matches
            if (dateOfBirth.HasValue && match.DateOfBirth.HasValue &&
                dateOfBirth.Value.Date == match.DateOfBirth.Value.Date)
            {
                match.MatchScore += 20;
            }

            // Filter by minimum score
            if (match.MatchScore >= _complianceSettings.OFAC.MinimumMatchScore)
            {
                matches.Add(match);
            }
        }

        return matches;
    }

    /// <summary>
    /// Parse OFAC CSV file
    /// </summary>
    private List<OFACEntry> ParseOFACCSV(string csvContent)
    {
        var entries = new List<OFACEntry>();
        var lines = csvContent.Split('\n', StringSplitOptions.RemoveEmptyEntries);

        foreach (var line in lines.Skip(1)) // Skip header
        {
            var fields = line.Split(',');
            if (fields.Length < 3) continue;

            entries.Add(new OFACEntry
            {
                EntityNumber = fields[0].Trim('"'),
                SDNType = fields.Length > 1 ? fields[1].Trim('"') : null,
                Program = fields.Length > 2 ? fields[2].Trim('"') : null,
                FullName = fields.Length > 3 ? fields[3].Trim('"') : "",
                Aliases = fields.Length > 4 ? fields[4].Trim('"') : null,
                Address = fields.Length > 5 ? fields[5].Trim('"') : null,
                Citizenship = fields.Length > 6 ? fields[6].Trim('"') : null,
                Nationality = fields.Length > 7 ? fields[7].Trim('"') : null,
            });
        }

        return entries;
    }

    /// <summary>
    /// Insert OFAC entry into database
    /// </summary>
    private async Task InsertOFACEntryAsync(NpgsqlConnection connection, OFACEntry entry, CancellationToken cancellationToken)
    {
        var sql = @"
            INSERT INTO ofac_sanctions_list
            (entity_number, sdn_type, program, full_name, aliases, address, citizenship, nationality, list_source)
            VALUES (@entityNumber, @sdnType, @program, @fullName, @aliases, @address, @citizenship, @nationality, @listSource)";

        await using var cmd = new NpgsqlCommand(sql, connection);
        cmd.Parameters.AddWithValue("entityNumber", (object?)entry.EntityNumber ?? DBNull.Value);
        cmd.Parameters.AddWithValue("sdnType", (object?)entry.SDNType ?? DBNull.Value);
        cmd.Parameters.AddWithValue("program", (object?)entry.Program ?? DBNull.Value);
        cmd.Parameters.AddWithValue("fullName", entry.FullName);
        cmd.Parameters.AddWithValue("aliases", (object?)entry.Aliases ?? DBNull.Value);
        cmd.Parameters.AddWithValue("address", (object?)entry.Address ?? DBNull.Value);
        cmd.Parameters.AddWithValue("citizenship", (object?)entry.Citizenship ?? DBNull.Value);
        cmd.Parameters.AddWithValue("nationality", (object?)entry.Nationality ?? DBNull.Value);
        cmd.Parameters.AddWithValue("listSource", "OFAC SDN");

        await cmd.ExecuteNonQueryAsync(cancellationToken);
    }

    /// <summary>
    /// Get user by ID
    /// </summary>
    private async Task<User?> GetUserByIdAsync(NpgsqlConnection connection, Guid userId, CancellationToken cancellationToken)
    {
        var sql = @"
            SELECT user_id, username, email, full_name, date_of_birth, country,
                   is_sanctioned, last_sanctions_check
            FROM users
            WHERE user_id = @userId";

        await using var cmd = new NpgsqlCommand(sql, connection);
        cmd.Parameters.AddWithValue("userId", userId);

        await using var reader = await cmd.ExecuteReaderAsync(cancellationToken);
        if (await reader.ReadAsync(cancellationToken))
        {
            return new User
            {
                UserId = reader.GetGuid(0),
                Username = reader.GetString(1),
                Email = reader.GetString(2),
                FullName = reader.GetString(3),
                DateOfBirth = reader.IsDBNull(4) ? null : reader.GetDateTime(4),
                Country = reader.IsDBNull(5) ? null : reader.GetString(5),
                IsSanctioned = reader.GetBoolean(6),
                LastSanctionsCheck = reader.IsDBNull(7) ? null : reader.GetDateTime(7)
            };
        }

        return null;
    }

    /// <summary>
    /// Log compliance event
    /// </summary>
    private async Task LogComplianceEventAsync(
        ComplianceEventType eventType,
        ComplianceSeverity severity,
        Guid? userId,
        string title,
        string? eventData,
        CancellationToken cancellationToken)
    {
        await using var connection = new NpgsqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);

        var sql = @"
            INSERT INTO compliance_events
            (event_id, event_type, severity, user_id, title, event_data, source, created_at)
            VALUES (@eventId, @eventType, @severity, @userId, @title, @eventData::jsonb, @source, @createdAt)";

        await using var cmd = new NpgsqlCommand(sql, connection);
        cmd.Parameters.AddWithValue("eventId", Guid.NewGuid());
        cmd.Parameters.AddWithValue("eventType", eventType.ToString());
        cmd.Parameters.AddWithValue("severity", severity.ToString());
        cmd.Parameters.AddWithValue("userId", (object?)userId ?? DBNull.Value);
        cmd.Parameters.AddWithValue("title", title);
        cmd.Parameters.AddWithValue("eventData", (object?)eventData ?? DBNull.Value);
        cmd.Parameters.AddWithValue("source", "OFACScreeningService");
        cmd.Parameters.AddWithValue("createdAt", DateTime.UtcNow);

        await cmd.ExecuteNonQueryAsync(cancellationToken);
    }
}

/// <summary>
/// OFAC screening result
/// </summary>
public class OFACScreeningResult
{
    public bool ScreeningPerformed { get; set; }
    public DateTime? ScreeningDate { get; set; }
    public Guid? UserId { get; set; }
    public string? ScreenedName { get; set; }
    public bool IsMatch { get; set; }
    public int MatchScore { get; set; }
    public List<OFACMatch> Matches { get; set; } = new();
}

/// <summary>
/// OFAC match details
/// </summary>
public class OFACMatch
{
    public long EntryId { get; set; }
    public string? EntityNumber { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string? Aliases { get; set; }
    public string? Program { get; set; }
    public DateTime? DateOfBirth { get; set; }
    public string? Nationality { get; set; }
    public string? Citizenship { get; set; }
    public int MatchScore { get; set; }
}

/// <summary>
/// OFAC entry from SDN list
/// </summary>
public class OFACEntry
{
    public string? EntityNumber { get; set; }
    public string? SDNType { get; set; }
    public string? Program { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string? Aliases { get; set; }
    public string? Address { get; set; }
    public string? Citizenship { get; set; }
    public string? Nationality { get; set; }
}
